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

            private readonly TerrainDef baseTerrain;
            private readonly TerrainDef temporaryTerrain;
            private readonly RoofDef roof;
            private readonly bool fogged;
            private readonly float snowDepth;
            private readonly float sandDepth;
            private readonly bool polluted;
            private readonly List<ManagedThingState> managedThings;
            private readonly List<int> managedThingIds;
            private readonly List<int> blockerIds;

            private CellSnapshot(
                TerrainDef baseTerrain,
                TerrainDef temporaryTerrain,
                RoofDef roof,
                bool fogged,
                float snowDepth,
                float sandDepth,
                bool polluted,
                List<ManagedThingState> managedThings,
                List<int> managedThingIds,
                List<int> blockerIds)
            {
                this.baseTerrain = baseTerrain;
                this.temporaryTerrain = temporaryTerrain;
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
                if (baseTerrain != other.baseTerrain ||
                    temporaryTerrain != other.temporaryTerrain)
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
                var terrainIndex = map.cellIndices.CellToIndex(cell);
                return new CellSnapshot(
                    map.terrainGrid.TerrainAtIgnoreTemp(terrainIndex),
                    map.terrainGrid.TempTerrainAt(terrainIndex),
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
                var terrainIndex = map.cellIndices.CellToIndex(cell);
                if ((domains & CellChangeDomain.Terrain) != 0 &&
                    (map.terrainGrid.TerrainAtIgnoreTemp(terrainIndex) !=
                         baseTerrain ||
                     map.terrainGrid.TempTerrainAt(terrainIndex) !=
                         temporaryTerrain))
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

                var terrainIndex = map.cellIndices.CellToIndex(cell);
                if ((domains & CellChangeDomain.Terrain) != 0 &&
                    (map.terrainGrid.TerrainAtIgnoreTemp(terrainIndex) !=
                         baseTerrain ||
                     map.terrainGrid.TempTerrainAt(terrainIndex) !=
                         temporaryTerrain))
                {
                    controller.RestoreTerrainLayersPreservingPlants(
                        cell,
                        baseTerrain,
                        temporaryTerrain);
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
