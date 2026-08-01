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

    }
}
