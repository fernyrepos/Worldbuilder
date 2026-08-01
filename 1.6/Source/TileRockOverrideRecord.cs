using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    public sealed class TileRockOverrideRecord : IExposable
    {
        internal const int CurrentVersion = 1;

        public int dataVersion = CurrentVersion;
        public int tileId = -1;
        public List<string> rockDefNames = new List<string>();

        public TileRockOverrideRecord()
        {
        }

        internal TileRockOverrideRecord(PlanetTile tile, IEnumerable<ThingDef> rocks)
        {
            tileId = tile.tileId;
            rockDefNames = rocks
                .Where(IsValidRock)
                .Select(rock => rock.defName)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(defName => defName, StringComparer.Ordinal)
                .ToList();
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref dataVersion, "dataVersion", CurrentVersion);
            Scribe_Values.Look(ref tileId, "tileId", -1);
            Scribe_Collections.Look(ref rockDefNames, "rockDefNames", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                rockDefNames ??= new List<string>();
            }
        }

        internal List<ThingDef> ResolveRocks(out int invalidDefCount)
        {
            invalidDefCount = 0;
            var rocks = new List<ThingDef>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var defName in rockDefNames ?? Enumerable.Empty<string>())
            {
                if (defName.NullOrEmpty() || !seen.Add(defName))
                {
                    continue;
                }

                var rock = DefDatabase<ThingDef>.GetNamedSilentFail(defName);
                if (!IsValidRock(rock))
                {
                    invalidDefCount++;
                    continue;
                }

                rocks.Add(rock);
            }

            rocks.Sort((left, right) => string.CompareOrdinal(left.defName, right.defName));
            return rocks;
        }

        internal static bool IsValidRock(ThingDef rock)
        {
            return rock != null && rock.IsNonResourceNaturalRock;
        }
    }
}
