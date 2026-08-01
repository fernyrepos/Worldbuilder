using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    internal sealed class TileBrushController
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

        internal void ClearHistory()
        {
            CancelStroke();
            undoHistory.Clear();
            redoHistory.Clear();
            managedThingIdAliases.Clear();
            HasStrokeStatus = false;
        }

        internal bool Undo()
        {
            if (!CanUndo)
            {
                return false;
            }

            var record = undoHistory[undoHistory.Count - 1];
            undoHistory.RemoveAt(undoHistory.Count - 1);
            var partial = record.Irreversible;
            var redoRecord = new StrokeRecord();

            for (var i = record.Changes.Count - 1; i >= 0; i--)
            {
                var change = record.Changes[i];
                if (!change.After.Matches(
                        this,
                        change.Cell,
                        change.VerificationDomains))
                {
                    partial = true;
                    continue;
                }

                change.Before.Restore(
                    this,
                    change.Cell,
                    change.Domains,
                    allowCreateManagedThings: !change.Irreversible);
                change.RedoExpected = CellSnapshot.Capture(
                    Map,
                    change.Cell,
                    change.VerificationDomains);
                redoRecord.Changes.Add(change);
                redoRecord.Irreversible |= change.Irreversible;
            }

            if (redoRecord.Changes.Count > 0)
            {
                redoRecord.Changes.Reverse();
                redoHistory.Add(redoRecord);
            }

            NotifyPartialHistory(partial);
            return true;
        }

        internal bool Redo()
        {
            if (!CanRedo)
            {
                return false;
            }

            var record = redoHistory[redoHistory.Count - 1];
            redoHistory.RemoveAt(redoHistory.Count - 1);
            var partial = false;
            var undoRecord = new StrokeRecord();

            foreach (var change in record.Changes)
            {
                if (change.RedoExpected == null ||
                    !change.RedoExpected.Matches(
                        this,
                        change.Cell,
                        change.VerificationDomains))
                {
                    partial = true;
                    continue;
                }

                change.After.Restore(
                    this,
                    change.Cell,
                    change.Domains,
                    allowCreateManagedThings: true);
                change.After = CellSnapshot.Capture(
                    Map,
                    change.Cell,
                    change.VerificationDomains);
                change.RedoExpected = null;
                undoRecord.Changes.Add(change);
                undoRecord.Irreversible |= change.Irreversible;
            }

            if (undoRecord.Changes.Count > 0)
            {
                undoHistory.Add(undoRecord);
            }

            NotifyPartialHistory(partial);
            return true;
        }

        internal void DrawPreview(
            IntVec3 mouseCell,
            bool singleCell)
        {
            if (!mouseCell.InBounds(Map))
            {
                return;
            }

            var center = mouseCell.ToVector3ShiftedWithAltitude(
                AltitudeLayer.MetaOverlays);
            if (singleCell)
            {
                GenDraw.DrawCircleOutline(center, 0.5f);
                return;
            }

            var radius = Mathf.Max(0.5f, settings.Radius + 0.5f);
            switch (settings.Shape)
            {
                case TileBrushShape.Rectangle:
                    DrawRectanglePreview(center, radius);
                    break;
                case TileBrushShape.Rugged:
                    DrawRuggedPreview(
                        mouseCell,
                        center,
                        settings.Radius);
                    break;
                default:
                    GenDraw.DrawCircleOutline(center, radius);
                    break;
            }
        }

        private void DrawRectanglePreview(
            Vector3 center,
            float halfExtent)
        {
            var left = Mathf.Max(0f, center.x - halfExtent);
            var right = Mathf.Min(Map.Size.x, center.x + halfExtent);
            var bottom = Mathf.Max(0f, center.z - halfExtent);
            var top = Mathf.Min(Map.Size.z, center.z + halfExtent);
            var bottomLeft = new Vector3(left, center.y, bottom);
            var topLeft = new Vector3(left, center.y, top);
            var topRight = new Vector3(right, center.y, top);
            var bottomRight = new Vector3(right, center.y, bottom);
            GenDraw.DrawLineBetween(bottomLeft, topLeft);
            GenDraw.DrawLineBetween(topLeft, topRight);
            GenDraw.DrawLineBetween(topRight, bottomRight);
            GenDraw.DrawLineBetween(bottomRight, bottomLeft);
        }

        private void DrawRuggedPreview(
            IntVec3 centerCell,
            Vector3 center,
            int radius)
        {
            if (radius <= 0)
            {
                GenDraw.DrawCircleOutline(
                    center,
                    Mathf.Max(0.5f, radius + 0.5f));
                return;
            }

            var sectorCount = PrepareRuggedEdgeNoise(
                centerCell,
                radius);
            var previous = Vector3.zero;
            for (var sector = 0; sector <= sectorCount; sector++)
            {
                var normalizedAngle =
                    sector / (float)sectorCount;
                var angle =
                    normalizedAngle * 2f * Mathf.PI -
                    Mathf.PI;
                var edgeRadius = RuggedEdgeRadius(
                    radius,
                    sectorCount,
                    normalizedAngle);
                var point = new Vector3(
                    Mathf.Clamp(
                        center.x + Mathf.Cos(angle) * edgeRadius,
                        0f,
                        Map.Size.x),
                    center.y,
                    Mathf.Clamp(
                        center.z + Mathf.Sin(angle) * edgeRadius,
                        0f,
                        Map.Size.z));
                if (sector > 0)
                {
                    GenDraw.DrawLineBetween(previous, point);
                }

                previous = point;
            }
        }

        internal void MarkMountainCellForFog(IntVec3 cell)
        {
            if (StrokeActive && cell.InBounds(Map))
            {
                mountainFogCells.Add(cell);
            }
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

        private void RegisterManagedThingIdAlias(
            int sourceId,
            int replacementId)
        {
            if (sourceId < 0 ||
                replacementId < 0 ||
                sourceId == replacementId)
            {
                return;
            }

            var terminalId = ResolveManagedThingId(sourceId);
            if (terminalId != replacementId)
            {
                managedThingIdAliases[terminalId] = replacementId;
                managedThingIdAliases[sourceId] = replacementId;
            }
        }

        private bool ManagedThingIdsMatch(
            List<int> expectedIds,
            List<int> currentIds)
        {
            if (expectedIds.Count != currentIds.Count)
            {
                return false;
            }

            for (var i = 0; i < expectedIds.Count; i++)
            {
                var expectedId = ResolveManagedThingId(expectedIds[i]);
                var found = false;
                for (var j = 0; j < currentIds.Count; j++)
                {
                    if (currentIds[j] == expectedId)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private int ResolveManagedThingId(int thingId)
        {
            var resolvedId = thingId;
            var remaining = managedThingIdAliases.Count + 1;
            while (remaining-- > 0 &&
                   managedThingIdAliases.TryGetValue(
                       resolvedId,
                       out var replacementId) &&
                   replacementId != resolvedId)
            {
                resolvedId = replacementId;
            }

            if (resolvedId != thingId)
            {
                managedThingIdAliases[thingId] = resolvedId;
            }

            return resolvedId;
        }

        internal bool WouldWipePreservedThing(
            ThingDef thingDef,
            IntVec3 cell)
        {
            return GenSpawn.WouldWipeAnythingWith(
                cell,
                Rot4.North,
                thingDef,
                Map,
                thing => true);
        }

        internal bool PreparePlacement(
            TileBrushSettings brushSettings,
            IntVec3 cell,
            out bool preparationChanged,
            out bool irreversible,
            out bool relocationFailed)
        {
            preparationChanged = false;
            irreversible = false;
            relocationFailed = false;

            if (relocationDestinations.Contains(cell))
            {
                return false;
            }

            var things = Map.thingGrid.ThingsListAt(cell);
            if (!brushSettings.FullReplacement)
            {
                return !ContainsPlacementBlocker(
                    things,
                    terrainOnly: false);
            }

            return PrepareFullReplacement(
                cell,
                things,
                out preparationChanged,
                out irreversible,
                out relocationFailed);
        }

        internal bool PreparePlantPlacement(
            TileBrushSettings brushSettings,
            ThingDef plantDef,
            IntVec3 cell,
            out bool preparationChanged,
            out bool irreversible,
            out bool relocationFailed)
        {
            preparationChanged = false;
            irreversible = false;
            relocationFailed = false;

            if (relocationDestinations.Contains(cell))
            {
                return false;
            }

            var things = Map.thingGrid.ThingsListAt(cell);
            if (!brushSettings.FullReplacement)
            {
                return !ContainsPlacementBlocker(
                           things,
                           terrainOnly: false) &&
                       plantDef.CanEverPlantAt(
                           cell,
                           Map,
                           canWipePlantsExceptTree: false,
                           checkMapTemperature: true);
            }

            if (!TryPlanFullReplacement(
                    cell,
                    things,
                    out relocationFailed))
            {
                ClearPlannedFullReplacement();
                return false;
            }

            var valid = replacementDestroyTargets.Count == 0 &&
                        relocationPlanScratch.Count == 0
                ? plantDef.CanEverPlantAt(
                    cell,
                    Map,
                    canWipePlantsExceptTree: false,
                    checkMapTemperature: true)
                : CanPlantAfterPlannedReplacement(plantDef, cell);
            if (!valid)
            {
                ClearPlannedFullReplacement();
                return false;
            }

            CommitPlannedFullReplacement(
                cell,
                out preparationChanged,
                out irreversible);
            return true;
        }

        internal bool PrepareTerrainPlacement(
            TileBrushSettings brushSettings,
            IntVec3 cell,
            out bool preparationChanged,
            out bool irreversible,
            out bool relocationFailed)
        {
            preparationChanged = false;
            irreversible = false;
            relocationFailed = false;

            if (relocationDestinations.Contains(cell))
            {
                return false;
            }

            var things = Map.thingGrid.ThingsListAt(cell);
            if (!brushSettings.FullReplacement)
            {
                return !ContainsPlacementBlocker(
                    things,
                    terrainOnly: true);
            }

            return PrepareFullReplacement(
                cell,
                things,
                out preparationChanged,
                out irreversible,
                out relocationFailed);
        }

        internal void SetTerrainPreservingPlants(
            IntVec3 cell,
            TerrainDef terrain)
        {
            SetTerrainPreservingPlants(
                cell,
                terrain,
                captureBefore: true);
        }

        private void SetTerrainPreservingPlants(
            IntVec3 cell,
            TerrainDef terrain,
            bool captureBefore)
        {
            terrainPlantsToPreserve.Clear();
            var things = Map.thingGrid.ThingsListAt(cell);
            for (var i = 0; i < things.Count; i++)
            {
                if (things[i] is Plant plant &&
                    plant.Spawned)
                {
                    terrainPlantsToPreserve.Add(
                        new PreservedPlantState(plant));
                }
            }

            if (captureBefore)
            {
                CaptureBeforeCellChange(cell);
            }

            foreach (var plantState in terrainPlantsToPreserve)
            {
                plantState.Plant.DeSpawn(
                    DestroyMode.WillReplace);
            }

            Map.terrainGrid.SetTerrain(cell, terrain);

            foreach (var plantState in terrainPlantsToPreserve)
            {
                GenSpawn.Spawn(
                    plantState.Plant,
                    plantState.Position,
                    Map,
                    plantState.Rotation,
                    WipeMode.Vanish);
            }
        }

        internal bool PrepareNonDestructive(
            TileBrushSettings brushSettings,
            IntVec3 cell,
            out bool preparationChanged,
            out bool irreversible,
            out bool relocationFailed)
        {
            return PreparePlacement(
                brushSettings,
                cell,
                out preparationChanged,
                out irreversible,
                out relocationFailed);
        }

        private BrushApplyResult ClearEverything(IntVec3 cell)
        {
            var changed = false;
            var irreversible = false;
            var previousAllowDestroyNonDestroyable =
                Thing.allowDestroyNonDestroyable;
            Thing.allowDestroyNonDestroyable = true;
            try
            {
                foreach (var target in eraseTargets)
                {
                    if (!target.Spawned ||
                        destroyedThisStroke.Contains(target))
                    {
                        continue;
                    }

                    irreversible |= !IsManagedNaturalThing(target);
                    if (IsNaturalRockStructure(target))
                    {
                        TrackErasedMountainEdges(target);
                        ((Building)target).canChangeTerrainOnDestroyed = false;
                        // The brush removes its roofs and exposes its fog itself.
                        // WillReplace skips a full roof-support and fog flood scan
                        // for every rock in a large stroke.
                        target.Destroy(DestroyMode.WillReplace);
                    }
                    else
                    {
                        target.Destroy(DestroyMode.Vanish);
                    }

                    destroyedThisStroke.Add(target);
                    changed = true;
                }
            }
            finally
            {
                Thing.allowDestroyNonDestroyable =
                    previousAllowDestroyNonDestroyable;
            }

            var roof = cell.GetRoof(Map);
            if (roof != null)
            {
                Map.roofGrid.SetRoof(cell, null);
                changed = true;
            }

            if (Map.snowGrid.GetDepth(cell) > 0f)
            {
                Map.snowGrid.SetDepth(cell, 0f);
                changed = true;
            }

            if (ModsConfig.BiotechActive &&
                Map.pollutionGrid.IsPolluted(cell))
            {
                Map.pollutionGrid.SetPolluted(cell, false);
                changed = true;
            }

            if (changed && Map.fogGrid.IsFogged(cell))
            {
                Map.fogGrid.Unfog(cell);
            }

            return new BrushApplyResult(changed, irreversible);
        }

        private void ApplyPathCenter(IntVec3 center)
        {
            CollectFootprint(center, footprintCells);
            ApplyFootprintCells();
        }

        private void ApplyPathCenter(
            IntVec3 center,
            IntVec3 previousCenter)
        {
            CollectIncrementalFootprint(
                center,
                previousCenter,
                footprintCells);
            ApplyFootprintCells();
        }

        private void ApplyFootprintCells()
        {
            foreach (var cell in footprintCells)
            {
                if (!TryMarkProcessed(cell))
                {
                    continue;
                }

                if (relocationDestinations.Contains(cell))
                {
                    activeRecord.Skipped++;
                    continue;
                }

                ApplyCell(cell);
            }
        }

        private void AdvanceProcessedCellGeneration()
        {
            processedCellGeneration++;
            if (processedCellGeneration != 0)
            {
                return;
            }

            Array.Clear(
                processedCellStamps,
                0,
                processedCellStamps.Length);
            processedCellGeneration = 1;
        }

        private bool TryMarkProcessed(IntVec3 cell)
        {
            var index = Map.cellIndices.CellToIndex(cell);
            if (processedCellStamps[index] == processedCellGeneration)
            {
                return false;
            }

            processedCellStamps[index] = processedCellGeneration;
            return true;
        }

        private bool WasProcessed(IntVec3 cell)
        {
            return processedCellStamps[
                       Map.cellIndices.CellToIndex(cell)] ==
                   processedCellGeneration;
        }

        private void CollectIncrementalFootprint(
            IntVec3 center,
            IntVec3 previousCenter,
            List<IntVec3> destination)
        {
            destination.Clear();
            var radius = Mathf.Max(0, settings.Radius);
            switch (settings.Shape)
            {
                case TileBrushShape.Rectangle:
                    AddRectangleFootprintDelta(
                        center,
                        previousCenter,
                        radius,
                        destination);
                    break;
                case TileBrushShape.Rugged:
                    AddRuggedFootprintDelta(
                        center,
                        previousCenter,
                        radius,
                        destination);
                    break;
                default:
                    AddCircleFootprintDelta(
                        center,
                        previousCenter,
                        radius,
                        destination);
                    break;
            }
        }

        private void CollectFootprint(
            IntVec3 center,
            List<IntVec3> destination)
        {
            destination.Clear();
            var radius = Mathf.Max(0, settings.Radius);
            switch (settings.Shape)
            {
                case TileBrushShape.Rectangle:
                    AddRectangleFootprint(
                        center,
                        radius,
                        destination);
                    break;
                case TileBrushShape.Rugged:
                    AddRuggedFootprint(
                        center,
                        radius,
                        destination);
                    break;
                default:
                    AddCircleFootprint(
                        center,
                        radius,
                        destination);
                    break;
            }
        }

        private void AddCircleFootprint(
            IntVec3 center,
            int radius,
            List<IntVec3> destination)
        {
            foreach (var cell in GenRadial.RadialCellsAround(
                         center,
                         radius,
                         useCenter: true))
            {
                if (cell.InBounds(Map))
                {
                    destination.Add(cell);
                }
            }
        }

        private void AddCircleFootprintDelta(
            IntVec3 center,
            IntVec3 previousCenter,
            int radius,
            List<IntVec3> destination)
        {
            var deltaX = center.x - previousCenter.x;
            var deltaZ = center.z - previousCenter.z;
            var stepDistance = Mathf.Sqrt(
                deltaX * deltaX +
                deltaZ * deltaZ);
            var minimumRadius = Mathf.Max(
                0f,
                radius - stepDistance);
            foreach (var cell in GenRadial.RadialCellsAround(
                         center,
                         minimumRadius,
                         radius))
            {
                if (cell.InBounds(Map))
                {
                    destination.Add(cell);
                }
            }
        }

        private void AddRectangleFootprint(
            IntVec3 center,
            int radius,
            List<IntVec3> destination)
        {
            for (var x = center.x - radius; x <= center.x + radius; x++)
            {
                for (var z = center.z - radius; z <= center.z + radius; z++)
                {
                    var cell = new IntVec3(x, 0, z);
                    if (cell.InBounds(Map))
                    {
                        destination.Add(cell);
                    }
                }
            }
        }

        private void AddRectangleFootprintDelta(
            IntVec3 center,
            IntVec3 previousCenter,
            int radius,
            List<IntVec3> destination)
        {
            var minimumX = Mathf.Max(0, center.x - radius);
            var maximumX = Mathf.Min(
                Map.Size.x - 1,
                center.x + radius);
            var minimumZ = Mathf.Max(0, center.z - radius);
            var maximumZ = Mathf.Min(
                Map.Size.z - 1,
                center.z + radius);
            var previousMinimumX = previousCenter.x - radius;
            var previousMaximumX = previousCenter.x + radius;
            var previousMinimumZ = previousCenter.z - radius;
            var previousMaximumZ = previousCenter.z + radius;

            for (var x = minimumX; x <= maximumX; x++)
            {
                if (x < previousMinimumX ||
                    x > previousMaximumX)
                {
                    AddRectangleColumn(
                        x,
                        minimumZ,
                        maximumZ,
                        destination);
                    continue;
                }

                AddRectangleColumn(
                    x,
                    minimumZ,
                    Mathf.Min(
                        maximumZ,
                        previousMinimumZ - 1),
                    destination);
                AddRectangleColumn(
                    x,
                    Mathf.Max(
                        minimumZ,
                        previousMaximumZ + 1),
                    maximumZ,
                    destination);
            }
        }

        private static void AddRectangleColumn(
            int x,
            int minimumZ,
            int maximumZ,
            List<IntVec3> destination)
        {
            for (var z = minimumZ; z <= maximumZ; z++)
            {
                destination.Add(new IntVec3(x, 0, z));
            }
        }

        private void AddRuggedFootprint(
            IntVec3 center,
            int radius,
            List<IntVec3> destination)
        {
            if (radius <= 0)
            {
                AddCircleFootprint(
                    center,
                    radius,
                    destination);
                return;
            }

            var sectorCount = PrepareRuggedEdgeNoise(
                center,
                radius);
            var edgeVariation = Mathf.Clamp(
                radius * 0.42f,
                1.25f,
                8f);
            for (var x = center.x - radius; x <= center.x + radius; x++)
            {
                for (var z = center.z - radius; z <= center.z + radius; z++)
                {
                    var cell = new IntVec3(x, 0, z);
                    if (!cell.InBounds(Map))
                    {
                        continue;
                    }

                    var offsetX = x - center.x;
                    var offsetZ = z - center.z;
                    var distance = Mathf.Sqrt(
                        offsetX * offsetX +
                        offsetZ * offsetZ);
                    if (distance <= radius - edgeVariation)
                    {
                        destination.Add(cell);
                        continue;
                    }

                    var normalizedAngle =
                        (Mathf.Atan2(offsetZ, offsetX) + Mathf.PI) /
                        (2f * Mathf.PI);
                    var edgeRadius = RuggedEdgeRadius(
                        radius,
                        sectorCount,
                        normalizedAngle);
                    if (distance <= edgeRadius)
                    {
                        destination.Add(cell);
                    }
                }
            }
        }

        private void AddRuggedFootprintDelta(
            IntVec3 center,
            IntVec3 previousCenter,
            int radius,
            List<IntVec3> destination)
        {
            if (radius <= 0)
            {
                AddCircleFootprintDelta(
                    center,
                    previousCenter,
                    radius,
                    destination);
                return;
            }

            var sectorCount = PrepareRuggedEdgeNoise(
                center,
                radius);
            var edgeVariation = Mathf.Clamp(
                radius * 0.42f,
                1.25f,
                8f);
            var deltaX = center.x - previousCenter.x;
            var deltaZ = center.z - previousCenter.z;
            var stepDistance = Mathf.Sqrt(
                deltaX * deltaX +
                deltaZ * deltaZ);
            var minimumRadius = Mathf.Max(
                0f,
                radius -
                edgeVariation -
                stepDistance);

            foreach (var cell in GenRadial.RadialCellsAround(
                         center,
                         minimumRadius,
                         radius + 0.35f))
            {
                if (!cell.InBounds(Map))
                {
                    continue;
                }

                var offsetX = cell.x - center.x;
                var offsetZ = cell.z - center.z;
                if (Mathf.Abs(offsetX) > radius ||
                    Mathf.Abs(offsetZ) > radius)
                {
                    continue;
                }

                var distance = Mathf.Sqrt(
                    offsetX * offsetX +
                    offsetZ * offsetZ);
                if (distance <= radius - edgeVariation)
                {
                    destination.Add(cell);
                    continue;
                }

                var normalizedAngle =
                    (Mathf.Atan2(offsetZ, offsetX) + Mathf.PI) /
                    (2f * Mathf.PI);
                if (distance <= RuggedEdgeRadius(
                        radius,
                        sectorCount,
                        normalizedAngle))
                {
                    destination.Add(cell);
                }
            }
        }

        private int PrepareRuggedEdgeNoise(
            IntVec3 center,
            int radius)
        {
            var sectorCount = Mathf.Clamp(
                radius + 6,
                10,
                Mathf.Min(28, MaxRuggedSectors));
            for (var sector = 0; sector < sectorCount; sector++)
            {
                ruggedEdgeNoise[sector] = RuggedEdgeSample(
                    center,
                    radius,
                    sector);
            }

            return sectorCount;
        }

        private float RuggedEdgeRadius(
            int radius,
            int sectorCount,
            float normalizedAngle)
        {
            var sectorPosition = normalizedAngle * sectorCount;
            var firstSector =
                Mathf.FloorToInt(sectorPosition) % sectorCount;
            var secondSector =
                (firstSector + 1) % sectorCount;
            var blend = sectorPosition -
                        Mathf.Floor(sectorPosition);
            blend = blend * blend * (3f - 2f * blend);
            var edgeNoise = Mathf.Lerp(
                ruggedEdgeNoise[firstSector],
                ruggedEdgeNoise[secondSector],
                blend);
            var edgeVariation = Mathf.Clamp(
                radius * 0.42f,
                1.25f,
                8f);
            return radius -
                   edgeVariation +
                   edgeNoise * edgeVariation +
                   0.35f;
        }

        private float RuggedEdgeSample(
            IntVec3 center,
            int radius,
            int sector)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = (hash ^ (uint)Map.uniqueID) * 16777619u;
                hash = (hash ^ (uint)center.x) * 16777619u;
                hash = (hash ^ (uint)center.z) * 16777619u;
                hash = (hash ^ (uint)radius) * 16777619u;
                hash = (hash ^ (uint)sector) * 16777619u;
                hash ^= hash >> 13;
                hash *= 1274126177u;
                return (hash & 0x00FFFFFFu) / 16777216f;
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

        internal void CaptureBeforeCellChange(IntVec3 cell)
        {
            if (activeApplyCell != cell || activeRecord == null)
            {
                Log.Error(
                    "Worldbuilder: Tile brush attempted to capture a " +
                    "cell outside its active paint operation.");
                return;
            }

            activeBeforeSnapshot ??= CellSnapshot.Capture(Map, cell);
        }

        private void RecordCellChange(
            IntVec3 cell,
            CellSnapshot before,
            BrushApplyResult result)
        {
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
            var change = new CellChange(
                cell,
                before,
                after,
                result.Irreversible);
            activeRecord.Changes.Add(change);
            activeRecord.Irreversible |= change.Irreversible;
            activeRecord.RelocationFailed |= result.RelocationFailed;
        }

        private bool HasErasableWork(
            IntVec3 cell,
            List<Thing> preparedTargets)
        {
            if (preparedTargets.Count > 0)
            {
                return true;
            }

            var roof = cell.GetRoof(Map);
            if (roof != null)
            {
                return true;
            }

            if (Map.snowGrid.GetDepth(cell) > 0f)
            {
                return true;
            }

            return ModsConfig.BiotechActive &&
                   Map.pollutionGrid.IsPolluted(cell);
        }

        private bool PrepareFullReplacement(
            IntVec3 cell,
            List<Thing> things,
            out bool changed,
            out bool irreversible,
            out bool relocationFailed)
        {
            changed = false;
            irreversible = false;
            relocationFailed = false;
            if (!TryPlanFullReplacement(
                    cell,
                    things,
                    out relocationFailed))
            {
                ClearPlannedFullReplacement();
                return false;
            }

            CommitPlannedFullReplacement(
                cell,
                out changed,
                out irreversible);
            return true;
        }

        private bool TryPlanFullReplacement(
            IntVec3 cell,
            List<Thing> things,
            out bool relocationFailed)
        {
            relocationFailed = false;
            replacementDestroyTargets.Clear();
            replacementRelocationTargets.Clear();
            relocationPlanScratch.Clear();
            for (var i = 0; i < things.Count; i++)
            {
                var thing = things[i];
                if (thing is Pawn)
                {
                    if (!replacementRelocationTargets.Contains(thing))
                    {
                        replacementRelocationTargets.Add(thing);
                    }

                    continue;
                }

                if (!ShouldDestroyInFullReplacement(thing) ||
                    destroyedThisStroke.Contains(thing) ||
                    replacementDestroyTargets.Contains(thing))
                {
                    continue;
                }

                replacementDestroyTargets.Add(thing);
            }

            for (var i = 0; i < replacementDestroyTargets.Count; i++)
            {
                if (!replacementDestroyTargets[i].def.destroyable)
                {
                    return false;
                }
            }

            for (var i = 0; i < replacementDestroyTargets.Count; i++)
            {
                if (HasHeldLivingPawn(replacementDestroyTargets[i]))
                {
                    relocationFailed = true;
                    return false;
                }
            }

            if (!TryPlanRelocations(
                    cell,
                    replacementRelocationTargets))
            {
                relocationFailed = true;
                return false;
            }

            return true;
        }

        private void CommitPlannedFullReplacement(
            IntVec3 cell,
            out bool changed,
            out bool irreversible)
        {
            changed = false;
            irreversible = false;

            if (relocationPlanScratch.Count > 0 ||
                replacementDestroyTargets.Count > 0)
            {
                CaptureBeforeCellChange(cell);
            }

            foreach (var move in relocationPlanScratch)
            {
                MoveThing(move.Key, move.Value);
                relocationDestinations.Add(move.Value);
                changed = true;
                irreversible = true;
            }

            foreach (var thing in replacementDestroyTargets)
            {
                if (!thing.Spawned || destroyedThisStroke.Contains(thing))
                {
                    continue;
                }

                thing.Destroy(DestroyMode.Vanish);
                destroyedThisStroke.Add(thing);
                changed = true;
                irreversible = true;
            }

            ClearPlannedFullReplacement();
        }

        private void ClearPlannedFullReplacement()
        {
            replacementDestroyTargets.Clear();
            replacementRelocationTargets.Clear();
            relocationPlanScratch.Clear();
            relocationReservedScratch.Clear();
        }

        private bool CanPlantAfterPlannedReplacement(
            ThingDef plantDef,
            IntVec3 cell)
        {
            if (plantDef?.category != ThingCategory.Plant ||
                plantDef.plant == null ||
                !cell.InBounds(Map))
            {
                return false;
            }

            var terrain = Map.terrainGrid.TerrainAt(cell);
            if (!plantDef.plant.completelyIgnoreFertility &&
                Map.fertilityGrid.FertilityAt(cell) <
                plantDef.plant.fertilityMin)
            {
                return false;
            }

            if (Map.TileInfo.MinTemperature >
                    plantDef.plant.maxGrowthTemperature ||
                Map.TileInfo.MaxTemperature <
                    plantDef.plant.minGrowthTemperature)
            {
                return false;
            }

            if (cell.IsPolluted(Map))
            {
                if (plantDef.plant.pollution == Pollution.CleanOnly)
                {
                    return false;
                }
            }
            else if (plantDef.plant.pollution ==
                     Pollution.PollutedOnly)
            {
                return false;
            }

            if (plantDef.plant.terraformable &&
                !CanTerraformAfterPlannedReplacement(cell))
            {
                return false;
            }

            var things = Map.thingGrid.ThingsListAt(cell);
            var hasPlantGrower = false;
            for (var i = 0; i < things.Count; i++)
            {
                if (!IsRemovedByPlannedReplacement(things[i]) &&
                    things[i] is Building_PlantGrower)
                {
                    hasPlantGrower = true;
                    break;
                }
            }

            if (!hasPlantGrower &&
                ((plantDef.plant.WildTerrainTags.Count > 0 &&
                  !plantDef.plant.WildTerrainTags.Overlaps(
                      terrain.tags.OrElseEmptyEnumerable())) ||
                 (plantDef.plant.terrainBlacklist != null &&
                  plantDef.plant.terrainBlacklist.Contains(terrain))))
            {
                return false;
            }

            for (var i = 0; i < things.Count; i++)
            {
                var thing = things[i];
                if (IsRemovedByPlannedReplacement(thing))
                {
                    continue;
                }

                if (!hasPlantGrower &&
                    thing.def.BlocksPlanting(false))
                {
                    return false;
                }

                if (plantDef.passability ==
                        Traversability.Impassable &&
                    (thing.def.category == ThingCategory.Pawn ||
                     thing.def.category == ThingCategory.Item ||
                     thing.def.category == ThingCategory.Building))
                {
                    return false;
                }
            }

            if (plantDef.passability != Traversability.Impassable)
            {
                return true;
            }

            for (var i = 0; i < 4; i++)
            {
                var adjacentCell = cell +
                                   GenAdj.CardinalDirections[i];
                if (!adjacentCell.InBounds(Map))
                {
                    continue;
                }

                var edifice = adjacentCell.GetEdifice(Map);
                if (edifice != null &&
                    edifice.def.IsDoor &&
                    !IsRemovedByPlannedReplacement(edifice))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanTerraformAfterPlannedReplacement(IntVec3 cell)
        {
            var edifice = cell.GetEdifice(Map);
            if (edifice != null &&
                !IsRemovedByPlannedReplacement(edifice))
            {
                return false;
            }

            var terrain = Map.terrainGrid.TerrainAt(cell);
            return terrain.canEverTerraform &&
                   terrain.passability != Traversability.Impassable &&
                   terrain.affordances.Contains(
                       TerrainAffordanceDefOf.Light) &&
                   !terrain.isFoundation;
        }

        private bool IsRemovedByPlannedReplacement(Thing thing)
        {
            return replacementDestroyTargets.Contains(thing) ||
                   relocationPlanScratch.ContainsKey(thing);
        }

        private bool TryPlanRelocations(
            IntVec3 source,
            List<Thing> things)
        {
            relocationPlanScratch.Clear();
            if (things.Count == 0)
            {
                return true;
            }

            relocationReservedScratch.Clear();
            foreach (var destination in relocationDestinations)
            {
                relocationReservedScratch.Add(destination);
            }

            foreach (var thing in things)
            {
                if (!TryFindRelocationCell(
                        source,
                        thing,
                        relocationReservedScratch,
                        out var destination))
                {
                    relocationPlanScratch.Clear();
                    return false;
                }

                relocationPlanScratch.Add(thing, destination);
                relocationReservedScratch.Add(destination);
            }

            return true;
        }

        private bool TryFindRelocationCell(
            IntVec3 source,
            Thing thing,
            HashSet<IntVec3> reserved,
            out IntVec3 destination)
        {
            foreach (var candidate in GenRadial.RadialCellsAround(
                         source,
                         RelocationSearchRadius,
                         useCenter: false))
            {
                if (!candidate.InBounds(Map) ||
                    reserved.Contains(candidate) ||
                    WasProcessed(candidate) ||
                    !candidate.Standable(Map))
                {
                    continue;
                }

                var occupants = Map.thingGrid.ThingsListAt(candidate);
                if (ContainsPlacementBlocker(
                        occupants,
                        terrainOnly: false))
                {
                    continue;
                }

                destination = candidate;
                return true;
            }

            destination = IntVec3.Invalid;
            return false;
        }

        private void MoveThing(Thing thing, IntVec3 destination)
        {
            var rotation = thing.Rotation;
            thing.DeSpawn(DestroyMode.Vanish);
            GenSpawn.Spawn(
                thing,
                destination,
                Map,
                rotation,
                WipeMode.Vanish);
            if (thing is Pawn pawn)
            {
                pawn.Notify_Teleported();
            }
        }

        private static bool IsSafeModeBlocker(Thing thing)
        {
            return thing is Pawn ||
                   thing is Building ||
                   thing is Plant ||
                   thing is Frame ||
                   thing is Blueprint ||
                   thing.def.category == ThingCategory.Item;
        }

        private static bool ContainsPlacementBlocker(
            List<Thing> things,
            bool terrainOnly)
        {
            for (var i = 0; i < things.Count; i++)
            {
                if (terrainOnly
                        ? IsTerrainSafeModeBlocker(things[i])
                        : IsSafeModeBlocker(things[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTerrainSafeModeBlocker(Thing thing)
        {
            return thing is Pawn ||
                   thing is Building ||
                   thing is Frame ||
                   thing is Blueprint;
        }

        private static bool IsManagedNaturalThing(Thing thing)
        {
            return thing is Plant ||
                   thing.def.IsNonResourceNaturalRock ||
                   TileBrushToolBase.IsResourceMineable(thing.def) ||
                   TileBrushToolBase.IsStoneChunk(thing.def);
        }

        private static bool IsNaturalRockStructure(Thing thing)
        {
            return thing is Building &&
                   thing.def.building != null &&
                   (thing.def.building.isNaturalRock ||
                     thing.def.building.isResourceRock);
        }

        private static void DestroyManagedThingForHistory(Thing thing)
        {
            if (IsNaturalRockStructure(thing))
            {
                ((Building)thing).canChangeTerrainOnDestroyed = false;
                thing.Destroy(DestroyMode.WillReplace);
                return;
            }

            thing.Destroy(DestroyMode.Vanish);
        }

        private static bool ShouldDestroyInFullReplacement(Thing thing)
        {
            return thing is Building ||
                   thing is Plant ||
                   thing is Filth ||
                   thing is Frame ||
                   thing is Blueprint ||
                   thing.def.category == ThingCategory.Item;
        }

        private static bool HasHeldLivingPawn(Thing thing)
        {
            return thing is IThingHolder holder &&
                   ThingOwnerUtility
                       .GetAllThingsRecursively(holder)
                       .Any(heldThing =>
                           heldThing is Pawn pawn &&
                           !pawn.Dead);
        }

        private void TrackErasedMountainEdges(Thing rock)
        {
            foreach (var occupiedCell in rock.OccupiedRect())
            {
                foreach (var offset in GenAdj.AdjacentCells)
                {
                    var neighbor = occupiedCell + offset;
                    if (neighbor.InBounds(Map))
                    {
                        mountainRevealCells.Add(neighbor);
                    }
                }
            }
        }

        private void RevealErasedMountainEdges()
        {
            foreach (var cell in mountainRevealCells)
            {
                if (!Map.fogGrid.IsFogged(cell))
                {
                    continue;
                }

                var edifice = cell.GetEdifice(Map);
                if (!IsNaturalRockStructure(edifice) ||
                    !edifice.def.MakeFog ||
                    !HasUnfoggedExteriorNeighbor(cell))
                {
                    continue;
                }

                Map.fogGrid.Unfog(cell);
            }
        }

        private void RebuildMountainFog()
        {
            if (mountainFogCells.Count == 0)
            {
                return;
            }

            var paintedMountainCells = mountainFogCells.ToList();
            foreach (var cell in paintedMountainCells)
            {
                foreach (var offset in GenAdj.AdjacentCells)
                {
                    var neighbor = cell + offset;
                    if (neighbor.InBounds(Map) &&
                        IsFogBlockingRock(neighbor))
                    {
                        mountainFogCells.Add(neighbor);
                    }
                }
            }

            var fogBlockingRocks = mountainFogCells
                .Where(IsFogBlockingRock)
                .ToList();
            var recordedCells = new HashSet<IntVec3>(
                activeRecord.Changes.Select(change => change.Cell));
            var neighboringRockBefore = fogBlockingRocks
                .Where(cell => !recordedCells.Contains(cell))
                .ToDictionary(
                    cell => cell,
                    cell => CellSnapshot.Capture(Map, cell));

            foreach (var cell in fogBlockingRocks)
            {
                Map.fogGrid.Refog(CellRect.SingleCell(cell));
            }

            foreach (var cell in fogBlockingRocks)
            {
                if (HasUnfoggedExteriorNeighbor(cell))
                {
                    Map.fogGrid.Unfog(cell);
                }
            }

            foreach (var pair in neighboringRockBefore)
            {
                if (pair.Value.Fogged ==
                    Map.fogGrid.IsFogged(pair.Key))
                {
                    continue;
                }

                activeRecord.Changes.Add(
                    new CellChange(
                        pair.Key,
                        pair.Value,
                        CellSnapshot.Capture(Map, pair.Key),
                        irreversible: false,
                        fogOnly: true));
            }
        }

        private bool IsFogBlockingRock(IntVec3 cell)
        {
            var edifice = cell.GetEdifice(Map);
            return IsNaturalRockStructure(edifice) &&
                   edifice.def.MakeFog;
        }

        private bool HasUnfoggedExteriorNeighbor(IntVec3 cell)
        {
            foreach (var offset in GenAdj.AdjacentCells)
            {
                var neighbor = cell + offset;
                if (!neighbor.InBounds(Map) ||
                    Map.fogGrid.IsFogged(neighbor))
                {
                    continue;
                }

                var edifice = neighbor.GetEdifice(Map);
                if (edifice == null || !edifice.def.MakeFog)
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshMountainAfterSnapshots()
        {
            if (mountainFogCells.Count == 0)
            {
                return;
            }

            for (var i = 0; i < activeRecord.Changes.Count; i++)
            {
                var change = activeRecord.Changes[i];
                if (mountainFogCells.Contains(change.Cell))
                {
                    change.SetAfter(
                        CellSnapshot.Capture(Map, change.Cell));
                }
            }
        }

        private static void NotifyPartialHistory(bool partial)
        {
            if (partial)
            {
                Messages.Message(
                    "WB_TileBrushPartialUndo".Translate(),
                    MessageTypeDefOf.CautionInput,
                    historical: false);
            }
        }

        private readonly struct PreservedPlantState
        {
            internal PreservedPlantState(Plant plant)
            {
                Plant = plant;
                Position = plant.Position;
                Rotation = plant.Rotation;
            }

            internal Plant Plant { get; }
            internal IntVec3 Position { get; }
            internal Rot4 Rotation { get; }
        }

        private sealed class StrokeRecord
        {
            internal readonly List<CellChange> Changes = new List<CellChange>();
            internal int Skipped;
            internal bool Irreversible;
            internal bool RelocationFailed;
        }

        [Flags]
        private enum CellChangeDomain
        {
            None = 0,
            Terrain = 1,
            Roof = 2,
            Fog = 4,
            Snow = 8,
            Pollution = 16,
            ManagedThings = 32,
            Blockers = 64,
            Sand = 128
        }

        private sealed class CellChange
        {
            internal CellChange(
                IntVec3 cell,
                CellSnapshot before,
                CellSnapshot after,
                bool irreversible,
                bool fogOnly = false)
            {
                Cell = cell;
                Before = before;
                After = after;
                FogOnly = fogOnly;
                Domains = fogOnly
                    ? CellChangeDomain.Fog
                    : before.Differences(after);
                Irreversible = irreversible ||
                               (Domains & CellChangeDomain.Blockers) != 0;
            }

            internal IntVec3 Cell { get; }
            internal CellSnapshot Before { get; }
            internal CellSnapshot After { get; set; }
            internal bool Irreversible { get; private set; }
            internal bool FogOnly { get; }
            internal CellChangeDomain Domains { get; private set; }
            internal CellChangeDomain VerificationDomains
            {
                get
                {
                    var domains = Domains;
                    if ((Domains & CellChangeDomain.ManagedThings) != 0)
                    {
                        domains |= CellChangeDomain.Terrain |
                                   CellChangeDomain.Roof |
                                   CellChangeDomain.Snow |
                                   CellChangeDomain.Sand |
                                   CellChangeDomain.Pollution |
                                   CellChangeDomain.Blockers;
                    }

                    if ((Domains & CellChangeDomain.Terrain) != 0)
                    {
                        domains |= CellChangeDomain.Snow |
                                   CellChangeDomain.Sand |
                                   CellChangeDomain.Blockers;
                    }

                    return domains;
                }
            }
            internal CellSnapshot RedoExpected { get; set; }

            internal void SetAfter(CellSnapshot after)
            {
                After = after;
                if (!FogOnly)
                {
                    Domains = Before.Differences(after);
                    Irreversible |=
                        (Domains & CellChangeDomain.Blockers) != 0;
                }
            }
        }

        private sealed class CellSnapshot
        {
            private static readonly List<ManagedThingState> NoManagedThings =
                new List<ManagedThingState>();
            private static readonly List<int> NoManagedThingIds =
                new List<int>();
            private static readonly List<int> NoBlockerIds =
                new List<int>();

            private readonly TerrainDef terrain;
            private readonly RoofDef roof;
            private readonly bool fogged;
            private readonly float snowDepth;
            private readonly float sandDepth;
            private readonly bool polluted;
            private readonly List<ManagedThingState> managedThings;
            private readonly List<int> managedThingIds;
            private readonly List<int> blockerIds;

            private CellSnapshot(
                TerrainDef terrain,
                RoofDef roof,
                bool fogged,
                float snowDepth,
                float sandDepth,
                bool polluted,
                List<ManagedThingState> managedThings,
                List<int> managedThingIds,
                List<int> blockerIds)
            {
                this.terrain = terrain;
                this.roof = roof;
                this.fogged = fogged;
                this.snowDepth = snowDepth;
                this.sandDepth = sandDepth;
                this.polluted = polluted;
                this.managedThings = managedThings;
                this.managedThingIds = managedThingIds;
                this.blockerIds = blockerIds;
            }

            internal bool Fogged => fogged;

            internal CellChangeDomain Differences(CellSnapshot other)
            {
                var differences = CellChangeDomain.None;
                if (terrain != other.terrain)
                {
                    differences |= CellChangeDomain.Terrain;
                }

                if (roof != other.roof)
                {
                    differences |= CellChangeDomain.Roof;
                }

                if (fogged != other.fogged)
                {
                    differences |= CellChangeDomain.Fog;
                }

                if (!Mathf.Approximately(snowDepth, other.snowDepth))
                {
                    differences |= CellChangeDomain.Snow;
                }

                if (!Mathf.Approximately(sandDepth, other.sandDepth))
                {
                    differences |= CellChangeDomain.Sand;
                }

                if (polluted != other.polluted)
                {
                    differences |= CellChangeDomain.Pollution;
                }

                if (!managedThings.SequenceEqual(other.managedThings) ||
                    !managedThingIds.SequenceEqual(
                        other.managedThingIds))
                {
                    differences |= CellChangeDomain.ManagedThings;
                }

                if (!blockerIds.SequenceEqual(other.blockerIds))
                {
                    differences |= CellChangeDomain.Blockers;
                }

                return differences;
            }

            internal static CellSnapshot Capture(Map map, IntVec3 cell)
            {
                return Capture(
                    map,
                    cell,
                    map.thingGrid.ThingsListAt(cell),
                    captureManagedThings: true,
                    captureBlockers: true);
            }

            internal static CellSnapshot Capture(
                Map map,
                IntVec3 cell,
                List<Thing> things)
            {
                return Capture(
                    map,
                    cell,
                    things,
                    captureManagedThings: true,
                    captureBlockers: true);
            }

            internal static CellSnapshot Capture(
                Map map,
                IntVec3 cell,
                CellChangeDomain domains)
            {
                var captureManagedThings =
                    (domains & CellChangeDomain.ManagedThings) != 0;
                var captureBlockers =
                    (domains & CellChangeDomain.Blockers) != 0;
                return Capture(
                    map,
                    cell,
                    captureManagedThings || captureBlockers
                        ? map.thingGrid.ThingsListAt(cell)
                        : null,
                    captureManagedThings,
                    captureBlockers);
            }

            private static CellSnapshot Capture(
                Map map,
                IntVec3 cell,
                List<Thing> things,
                bool captureManagedThings,
                bool captureBlockers)
            {
                CaptureThingState(
                    things,
                    captureManagedThings,
                    captureBlockers,
                    out var managedThings,
                    out var managedThingIds,
                    out var blockerIds);
                return new CellSnapshot(
                    map.terrainGrid.TerrainAt(cell),
                    cell.GetRoof(map),
                    map.fogGrid.IsFogged(cell),
                    map.snowGrid.GetDepth(cell),
                    map.sandGrid?.GetDepth(cell) ?? 0f,
                    ModsConfig.BiotechActive &&
                    map.pollutionGrid.IsPolluted(cell),
                    managedThings,
                    managedThingIds,
                    blockerIds);
            }

            internal bool Matches(
                TileBrushController controller,
                IntVec3 cell,
                CellChangeDomain domains)
            {
                var map = controller.Map;
                if ((domains & CellChangeDomain.Terrain) != 0 &&
                    map.terrainGrid.TerrainAt(cell) != terrain)
                {
                    return false;
                }

                if ((domains & CellChangeDomain.Roof) != 0 &&
                    cell.GetRoof(map) != roof)
                {
                    return false;
                }

                if ((domains & CellChangeDomain.Fog) != 0 &&
                    map.fogGrid.IsFogged(cell) != fogged)
                {
                    return false;
                }

                if ((domains & CellChangeDomain.Snow) != 0 &&
                    !Mathf.Approximately(
                        map.snowGrid.GetDepth(cell),
                        snowDepth))
                {
                    return false;
                }

                if ((domains & CellChangeDomain.Sand) != 0 &&
                    !Mathf.Approximately(
                        map.sandGrid?.GetDepth(cell) ?? 0f,
                        sandDepth))
                {
                    return false;
                }

                if ((domains & CellChangeDomain.Pollution) != 0 &&
                    ModsConfig.BiotechActive &&
                    map.pollutionGrid.IsPolluted(cell) != polluted)
                {
                    return false;
                }

                if ((domains &
                     (CellChangeDomain.ManagedThings |
                      CellChangeDomain.Blockers)) == 0)
                {
                    return true;
                }

                var currentThings = map.thingGrid.ThingsListAt(cell);
                CaptureThingState(
                    currentThings,
                    captureManagedThings:
                        (domains & CellChangeDomain.ManagedThings) != 0,
                    captureBlockers:
                        (domains & CellChangeDomain.Blockers) != 0,
                    out var currentManaged,
                    out var currentManagedIds,
                    out var currentBlockers);
                if ((domains & CellChangeDomain.ManagedThings) != 0 &&
                    (!managedThings.SequenceEqual(currentManaged) ||
                     !controller.ManagedThingIdsMatch(
                         managedThingIds,
                         currentManagedIds)))
                {
                    return false;
                }

                return (domains & CellChangeDomain.Blockers) == 0 ||
                       blockerIds.SequenceEqual(currentBlockers);
            }

            internal void Restore(
                TileBrushController controller,
                IntVec3 cell,
                CellChangeDomain domains,
                bool allowCreateManagedThings)
            {
                var map = controller.Map;
                if ((domains & CellChangeDomain.ManagedThings) != 0)
                {
                    foreach (var thing in map.thingGrid
                                 .ThingsListAt(cell)
                                 .Where(IsManagedNaturalThing)
                                 .ToList())
                    {
                        DestroyManagedThingForHistory(thing);
                    }
                }

                if ((domains & CellChangeDomain.Terrain) != 0 &&
                    map.terrainGrid.TerrainAt(cell) != terrain)
                {
                    controller.SetTerrainPreservingPlants(
                        cell,
                        terrain,
                        captureBefore: false);
                }

                if ((domains & CellChangeDomain.Roof) != 0 &&
                    cell.GetRoof(map) != roof)
                {
                    map.roofGrid.SetRoof(cell, roof);
                }

                if ((domains & CellChangeDomain.Snow) != 0 &&
                    !Mathf.Approximately(
                        map.snowGrid.GetDepth(cell),
                        snowDepth))
                {
                    map.snowGrid.SetDepth(cell, snowDepth);
                }

                if ((domains & CellChangeDomain.Sand) != 0 &&
                    map.sandGrid != null &&
                    !Mathf.Approximately(
                        map.sandGrid.GetDepth(cell),
                        sandDepth))
                {
                    map.sandGrid.SetDepth(cell, sandDepth);
                }

                if ((domains & CellChangeDomain.Pollution) != 0 &&
                    ModsConfig.BiotechActive &&
                    map.pollutionGrid.IsPolluted(cell) != polluted)
                {
                    map.pollutionGrid.SetPolluted(cell, polluted);
                }

                if ((domains & CellChangeDomain.ManagedThings) != 0 &&
                    allowCreateManagedThings)
                {
                    foreach (var state in managedThings)
                    {
                        var spawned = state.Spawn(map, cell);
                        if (spawned != null)
                        {
                            controller.RegisterManagedThingIdAlias(
                                state.ThingId,
                                spawned.thingIDNumber);
                        }
                    }
                }

                if ((domains & CellChangeDomain.Fog) != 0)
                {
                    RestoreFog(map, cell);
                }
            }

            internal void RestoreFog(Map map, IntVec3 cell)
            {
                if (map.fogGrid.IsFogged(cell) != fogged)
                {
                    if (fogged)
                    {
                        map.fogGrid.Refog(CellRect.SingleCell(cell));
                    }
                    else
                    {
                        map.fogGrid.Unfog(cell);
                    }
                }
            }

            private static void CaptureThingState(
                List<Thing> things,
                bool captureManagedThings,
                bool captureBlockers,
                out List<ManagedThingState> managedThings,
                out List<int> managedThingIds,
                out List<int> blockerIds)
            {
                managedThings = null;
                managedThingIds = null;
                blockerIds = null;
                if (things != null)
                {
                    foreach (var thing in things)
                    {
                        if (captureManagedThings &&
                            IsManagedNaturalThing(thing))
                        {
                            if (managedThings == null)
                            {
                                managedThings =
                                    new List<ManagedThingState>(1);
                            }

                            managedThings.Add(
                                ManagedThingState.Capture(thing));
                            if (managedThingIds == null)
                            {
                                managedThingIds = new List<int>(1);
                            }

                            managedThingIds.Add(thing.thingIDNumber);
                        }
                        else if (captureBlockers &&
                                 IsHistoryBlocker(thing))
                        {
                            if (blockerIds == null)
                            {
                                blockerIds = new List<int>(1);
                            }

                            blockerIds.Add(thing.thingIDNumber);
                        }
                    }
                }

                if (managedThings == null)
                {
                    managedThings = NoManagedThings;
                }
                else if (managedThings.Count > 1)
                {
                    managedThings.Sort(ManagedThingState.Compare);
                }

                if (managedThingIds == null)
                {
                    managedThingIds = NoManagedThingIds;
                }
                else if (managedThingIds.Count > 1)
                {
                    managedThingIds.Sort();
                }

                if (blockerIds == null)
                {
                    blockerIds = NoBlockerIds;
                }
                else if (blockerIds.Count > 1)
                {
                    blockerIds.Sort();
                }
            }

            private static bool IsHistoryBlocker(Thing thing)
            {
                return !IsManagedNaturalThing(thing) &&
                       (IsSafeModeBlocker(thing) || thing is Filth);
            }
        }

        private readonly struct ManagedThingState : IEquatable<ManagedThingState>
        {
            private ManagedThingState(
                int thingId,
                ThingDef def,
                ThingDef stuff,
                int stackCount,
                int hitPoints,
                Rot4 rotation,
                float growth)
            {
                ThingId = thingId;
                Def = def;
                Stuff = stuff;
                StackCount = stackCount;
                HitPoints = hitPoints;
                Rotation = rotation;
                Growth = growth;
            }

            internal int ThingId { get; }
            internal ThingDef Def { get; }
            internal ThingDef Stuff { get; }
            internal int StackCount { get; }
            internal int HitPoints { get; }
            internal Rot4 Rotation { get; }
            internal float Growth { get; }

            internal static ManagedThingState Capture(Thing thing)
            {
                return new ManagedThingState(
                    thing.thingIDNumber,
                    thing.def,
                    thing.Stuff,
                    thing.stackCount,
                    thing.HitPoints,
                    thing.Rotation,
                    thing is Plant plant ? plant.Growth : -1f);
            }

            internal static int Compare(
                ManagedThingState left,
                ManagedThingState right)
            {
                var result = string.CompareOrdinal(
                    left.Def.defName,
                    right.Def.defName);
                if (result != 0)
                {
                    return result;
                }

                result = string.CompareOrdinal(
                    left.Stuff?.defName ?? string.Empty,
                    right.Stuff?.defName ?? string.Empty);
                if (result != 0)
                {
                    return result;
                }

                result = left.StackCount.CompareTo(right.StackCount);
                if (result != 0)
                {
                    return result;
                }

                result = left.HitPoints.CompareTo(right.HitPoints);
                if (result != 0)
                {
                    return result;
                }

                result = left.Rotation.AsInt.CompareTo(
                    right.Rotation.AsInt);
                return result != 0
                    ? result
                    : left.Growth.CompareTo(right.Growth);
            }

            internal Thing Spawn(Map map, IntVec3 cell)
            {
                var thing = ThingMaker.MakeThing(Def, Stuff);
                thing.stackCount = Mathf.Clamp(StackCount, 1, Def.stackLimit);
                if (Def.useHitPoints)
                {
                    thing.HitPoints = Mathf.Clamp(
                        HitPoints,
                        1,
                        thing.MaxHitPoints);
                }

                if (thing is Plant plant && Growth >= 0f)
                {
                    plant.Growth = Mathf.Clamp01(Growth);
                }

                return GenSpawn.Spawn(
                    thing,
                    cell,
                    map,
                    Rotation,
                    WipeMode.Vanish);
            }

            public bool Equals(ManagedThingState other)
            {
                return Def == other.Def &&
                       Stuff == other.Stuff &&
                       StackCount == other.StackCount &&
                       HitPoints == other.HitPoints &&
                       Rotation == other.Rotation &&
                       Mathf.Approximately(Growth, other.Growth);
            }

            public override bool Equals(object obj)
            {
                return obj is ManagedThingState other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = Def.GetHashCode();
                    hash = (hash * 397) ^
                           (Stuff?.GetHashCode() ?? 0);
                    hash = (hash * 397) ^ StackCount;
                    hash = (hash * 397) ^ HitPoints;
                    hash = (hash * 397) ^ Rotation.AsInt;
                    hash = (hash * 397) ^ Growth.GetHashCode();
                    return hash;
                }
            }
        }
    }
}
