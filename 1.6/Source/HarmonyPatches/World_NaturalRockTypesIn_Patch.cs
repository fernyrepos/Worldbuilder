using System.Collections.Generic;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(RimWorld.Planet.World), nameof(RimWorld.Planet.World.NaturalRockTypesIn))]
    internal static class World_NaturalRockTypesIn_Patch
    {
        private static bool Prefix(
            RimWorld.Planet.World __instance,
            PlanetTile tile,
            ref IEnumerable<ThingDef> __result)
        {
            if (!RockOverrideService.TryGetOverride(__instance, tile, out var rocks))
            {
                return true;
            }

            __result = rocks;
            return false;
        }
    }
}
