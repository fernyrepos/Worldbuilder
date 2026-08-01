using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    public sealed class WorldComponent_RockOverrides : WorldComponent
    {
        private readonly Dictionary<PlanetTile, List<ThingDef>> overrides =
            new Dictionary<PlanetTile, List<ThingDef>>();

        private List<TileRockOverrideRecord> serializedRecords = new List<TileRockOverrideRecord>();

        public WorldComponent_RockOverrides(RimWorld.Planet.World world) : base(world)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                serializedRecords = CreateRecords();
            }

            Scribe_Collections.Look(ref serializedRecords, "tileRockOverrides", LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ReplaceFromRecords(serializedRecords, "save");
                serializedRecords = new List<TileRockOverrideRecord>();
            }
        }

        internal bool TryGetOverride(PlanetTile tile, out List<ThingDef> rocks)
        {
            return overrides.TryGetValue(tile, out rocks) && rocks.Count > 0;
        }

        internal bool HasOverride(PlanetTile tile)
        {
            return overrides.ContainsKey(tile);
        }

        internal bool SetOverride(PlanetTile tile, IEnumerable<ThingDef> rocks)
        {
            if (!RockOverrideService.IsEditableSurfaceTile(world, tile) ||
                rocks == null)
            {
                return false;
            }

            var normalized = rocks
                .Where(TileRockOverrideRecord.IsValidRock)
                .Distinct()
                .OrderBy(rock => rock.defName, StringComparer.Ordinal)
                .ToList();

            if (normalized.Count == 0)
            {
                return false;
            }

            overrides[tile] = normalized;
            return true;
        }

        internal void ClearOverride(PlanetTile tile)
        {
            overrides.Remove(tile);
        }

        internal List<TileRockOverrideRecord> CreateRecords()
        {
            return overrides
                .Where(pair => RockOverrideService.IsEditableSurfaceTile(world, pair.Key) && pair.Value.Count > 0)
                .OrderBy(pair => pair.Key.tileId)
                .Select(pair => new TileRockOverrideRecord(pair.Key, pair.Value))
                .ToList();
        }

        internal void ReplaceFromRecords(IEnumerable<TileRockOverrideRecord> records, string sourceDescription)
        {
            overrides.Clear();
            var invalidRecordCount = 0;
            var invalidDefCount = 0;

            foreach (var record in records ?? Enumerable.Empty<TileRockOverrideRecord>())
            {
                if (record == null ||
                    record.dataVersion < 1 ||
                    record.dataVersion > TileRockOverrideRecord.CurrentVersion ||
                    record.tileId < 0 ||
                    record.tileId >= world.grid.TilesCount)
                {
                    invalidRecordCount++;
                    continue;
                }

                var tile = new PlanetTile(record.tileId, world.grid.Surface);
                var rocks = record.ResolveRocks(out var invalidForRecord);
                invalidDefCount += invalidForRecord;
                if (rocks.Count == 0)
                {
                    invalidRecordCount++;
                    continue;
                }

                overrides[tile] = rocks;
            }

            if (invalidRecordCount > 0 || invalidDefCount > 0)
            {
                Log.Warning(
                    $"Worldbuilder: Ignored {invalidRecordCount} invalid tile rock override record(s) " +
                    $"and {invalidDefCount} missing or invalid rock def(s) while loading {sourceDescription}. " +
                    "Valid rocks were retained; records with no valid rocks fall back to vanilla biome/default rocks.");
            }
        }
    }
}
