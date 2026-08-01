using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    internal sealed partial class TileBrushController
    {
        private const int HistoryLimit = 20;
        private const float RelocationSearchRadius = 12f;
        private const int MaxRuggedSectors = 72;
        private const int MaxBrushFootprintCapacity = 51 * 51;

        private readonly TileBrushSettings settings;
        private readonly uint[] processedCellStamps;
        private readonly HashSet<IntVec3> relocationDestinations = new HashSet<IntVec3>();
        private readonly HashSet<Thing> destroyedThisStroke = new HashSet<Thing>();
        private readonly HashSet<IntVec3> mountainFogCells = new HashSet<IntVec3>();
        private readonly HashSet<IntVec3> mountainRevealCells =
            new HashSet<IntVec3>();
        private readonly List<IntVec3> footprintCells =
            new List<IntVec3>(MaxBrushFootprintCapacity);
        private readonly List<Thing> eraseTargets = new List<Thing>(8);
        private readonly List<Thing> replacementDestroyTargets =
            new List<Thing>(8);
        private readonly List<Thing> replacementRelocationTargets =
            new List<Thing>(4);
        private readonly Dictionary<Thing, IntVec3> relocationPlanScratch =
            new Dictionary<Thing, IntVec3>(4);
        private readonly HashSet<IntVec3> relocationReservedScratch =
            new HashSet<IntVec3>();
        private readonly List<PreservedPlantState> terrainPlantsToPreserve =
            new List<PreservedPlantState>(2);
        private readonly float[] ruggedEdgeNoise =
            new float[MaxRuggedSectors];
        private readonly List<StrokeRecord> undoHistory =
            new List<StrokeRecord>(HistoryLimit);
        private readonly List<StrokeRecord> redoHistory =
            new List<StrokeRecord>(HistoryLimit);
        private readonly Dictionary<int, int> managedThingIdAliases =
            new Dictionary<int, int>();

        private StrokeRecord activeRecord;
        private CellSnapshot activeBeforeSnapshot;
        private IntVec3 activeApplyCell = IntVec3.Invalid;
        private IntVec3 lastPathCell;
        private int strokeSerial;
        private uint processedCellGeneration;
        private bool pathSuspended;

        internal TileBrushController(Map map, TileBrushSettings settings)
        {
            Map = map;
            this.settings = settings;
            processedCellStamps = new uint[map.cellIndices.NumGridCells];
        }

        internal Map Map { get; }
        internal bool StrokeActive => activeRecord != null;
        internal bool CanUndo => !StrokeActive && undoHistory.Count > 0;
        internal bool CanRedo => !StrokeActive && redoHistory.Count > 0;
        internal bool HasStrokeStatus { get; private set; }
        internal int LastApplied { get; private set; }
        internal int LastSkipped { get; private set; }
        internal bool LastRelocationFailed { get; private set; }

        internal void BeginStroke(IntVec3 cell)
        {
            if (StrokeActive || !cell.InBounds(Map))
            {
                return;
            }

            strokeSerial++;
            activeRecord = new StrokeRecord();
            AdvanceProcessedCellGeneration();
            relocationDestinations.Clear();
            destroyedThisStroke.Clear();
            mountainFogCells.Clear();
            mountainRevealCells.Clear();
            lastPathCell = cell;
            pathSuspended = false;
            ApplyPathCenter(cell);
        }

        internal void ContinueStroke(IntVec3 cell)
        {
            if (!StrokeActive || !cell.InBounds(Map))
            {
                return;
            }

            if (pathSuspended)
            {
                pathSuspended = false;
                lastPathCell = cell;
                ApplyPathCenter(cell);
                return;
            }

            if (cell == lastPathCell)
            {
                return;
            }

            var firstCell = true;
            var previousPathCell = lastPathCell;
            foreach (var pathCell in GenSight.BresenhamCellsBetween(
                         lastPathCell,
                         cell))
            {
                if (firstCell)
                {
                    firstCell = false;
                    continue;
                }

                ApplyPathCenter(pathCell, previousPathCell);
                previousPathCell = pathCell;
            }

            lastPathCell = cell;
        }

        internal void SuspendPath()
        {
            if (StrokeActive)
            {
                pathSuspended = true;
            }
        }

        internal void EndStroke()
        {
            if (!StrokeActive)
            {
                return;
            }

            RevealErasedMountainEdges();
            RebuildMountainFog();
            RefreshMountainAfterSnapshots();
            LastApplied = activeRecord.Changes.Count;
            LastSkipped = activeRecord.Skipped;
            LastRelocationFailed = activeRecord.RelocationFailed;
            HasStrokeStatus = true;

            if (activeRecord.Changes.Count > 0)
            {
                undoHistory.Add(activeRecord);
                if (undoHistory.Count > HistoryLimit)
                {
                    undoHistory.RemoveAt(0);
                }

                redoHistory.Clear();
            }

            activeRecord = null;
            relocationDestinations.Clear();
            destroyedThisStroke.Clear();
            mountainFogCells.Clear();
            mountainRevealCells.Clear();
            pathSuspended = false;
        }

        internal void CancelStroke()
        {
            if (!StrokeActive)
            {
                return;
            }

            EndStroke();
        }

        internal bool PassesDensity(IntVec3 cell, float density)
        {
            if (density >= 1f)
            {
                return true;
            }

            if (density <= 0f)
            {
                return false;
            }

            unchecked
            {
                uint hash = 2166136261u;
                hash = (hash ^ (uint)Map.uniqueID) * 16777619u;
                hash = (hash ^ (uint)strokeSerial) * 16777619u;
                hash = (hash ^ (uint)settings.ToolKind) * 16777619u;
                hash = (hash ^ (uint)cell.x) * 16777619u;
                hash = (hash ^ (uint)cell.z) * 16777619u;
                hash ^= hash >> 13;
                hash *= 1274126177u;
                var sample = (hash & 0x00FFFFFFu) / 16777216f;
                return sample < density;
            }
        }

        private void ApplyCell(IntVec3 cell)
        {
            if (settings.Operation == TileBrushOperation.Clear)
            {
                eraseTargets.Clear();
                eraseTargets.AddRange(
                    Map.thingGrid.ThingsListAt(cell));
                if (!HasErasableWork(cell, eraseTargets))
                {
                    activeRecord.Skipped++;
                    return;
                }

                var clearBefore = CellSnapshot.Capture(
                    Map,
                    cell,
                    eraseTargets);
                var clearResult = ClearEverything(cell);
                RecordCellChange(cell, clearBefore, clearResult);
                return;
            }

            if (settings.Tool.SupportsDensity &&
                !PassesDensity(
                    cell,
                    settings.DensityForCurrentTool()))
            {
                activeRecord.Skipped++;
                return;
            }

            activeApplyCell = cell;
            activeBeforeSnapshot = null;
            var result = settings.Tool.Apply(this, settings, cell);
            var before = activeBeforeSnapshot;
            activeBeforeSnapshot = null;
            activeApplyCell = IntVec3.Invalid;
            if (!result.Changed)
            {
                activeRecord.Skipped++;
                if (result.RelocationFailed)
                {
                    activeRecord.RelocationFailed = true;
                }

                return;
            }

            var after = CellSnapshot.Capture(Map, cell);
            var irreversible = result.Irreversible;
            if (before == null)
            {
                Log.Error(
                    "Worldbuilder: Tile brush changed a cell without " +
                    "capturing its previous state.");
                before = after;
                irreversible = true;
            }

            var change = new CellChange(
                cell,
                before,
                after,
                irreversible);
            activeRecord.Changes.Add(change);
            activeRecord.Irreversible |= change.Irreversible;
            activeRecord.RelocationFailed |= result.RelocationFailed;
        }

    }
}
