using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(GenStep_RockChunks), nameof(GenStep_RockChunks.Generate))]
    internal static class GenStep_RockChunks_Generate_Patch
    {
        private sealed class NormalizationState
        {
            internal readonly HashSet<Thing> ExistingChunks;
            internal readonly List<ThingDef> AllowedChunkDefs;
            internal readonly int Seed;

            internal NormalizationState(HashSet<Thing> existingChunks, List<ThingDef> allowedChunkDefs, int seed)
            {
                ExistingChunks = existingChunks;
                AllowedChunkDefs = allowedChunkDefs;
                Seed = seed;
            }
        }

        private static void Prefix(Map map, out NormalizationState __state)
        {
            __state = null;
            if (map?.Biome?.forceRockTypes.NullOrEmpty() != false ||
                !RockOverrideService.TryGetOverride(Find.World, map.Tile, out var rocks))
            {
                return;
            }

            var allowedChunkDefs = rocks
                .Select(rock => rock.building?.mineableThing)
                .Where(IsStoneChunk)
                .Distinct()
                .OrderBy(chunk => chunk.defName)
                .ToList();

            if (allowedChunkDefs.Count == 0)
            {
                return;
            }

            var existingChunks = new HashSet<Thing>(
                map.listerThings.ThingsInGroup(ThingRequestGroup.Chunk).Where(thing => IsStoneChunk(thing.def)));
            __state = new NormalizationState(existingChunks, allowedChunkDefs, map.Tile.GetHashCode());
        }

        private static void Postfix(Map map, NormalizationState __state)
        {
            if (__state == null)
            {
                return;
            }

            var allowed = new HashSet<ThingDef>(__state.AllowedChunkDefs);
            var generatedChunks = map.listerThings
                .ThingsInGroup(ThingRequestGroup.Chunk)
                .Where(thing =>
                    thing.Spawned &&
                    IsStoneChunk(thing.def) &&
                    !__state.ExistingChunks.Contains(thing) &&
                    !allowed.Contains(thing.def))
                .ToList();

            foreach (var oldChunk in generatedChunks)
            {
                var position = oldChunk.Position;
                var replacementDef = __state.AllowedChunkDefs[
                    DeterministicIndex(position, __state.Seed, __state.AllowedChunkDefs.Count)];
                var replacement = ThingMaker.MakeThing(replacementDef);
                replacement.stackCount = oldChunk.stackCount;
                oldChunk.Destroy(DestroyMode.Vanish);
                using (new RandBlock(
                           DeterministicHash(position, __state.Seed)))
                {
                    GenSpawn.Spawn(
                        replacement,
                        position,
                        map,
                        WipeMode.Vanish);
                }
            }
        }

        private static bool IsStoneChunk(ThingDef def)
        {
            return def?.thingCategories?.Contains(ThingCategoryDefOf.StoneChunks) == true;
        }

        private static int DeterministicIndex(IntVec3 cell, int seed, int count)
        {
            return DeterministicHash(cell, seed) % count;
        }

        private static int DeterministicHash(IntVec3 cell, int seed)
        {
            unchecked
            {
                var hash = seed;
                hash = (hash * 397) ^ cell.x;
                hash = (hash * 397) ^ cell.z;
                return hash & int.MaxValue;
            }
        }
    }
}
