using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder;

[HarmonyPatch(typeof(WorldGenStep_AncientSites), nameof(WorldGenStep_AncientSites.GenerateAncientSites))]
public static class WorldGenStep_AncientSites_GenerateAncientSites_Patch
{
    private static void Prefix(WorldGenStep_AncientSites __instance, out FloatRange __state)
    {
        __state = __instance.ancientSitesPer100kTiles;
        __instance.ancientSitesPer100kTiles *=
            World_ExposeData_Patch.worldGenerationData.ancientRoadDensity;
    }

    private static void Postfix(WorldGenStep_AncientSites __instance, FloatRange __state)
    {
        __instance.ancientSitesPer100kTiles = __state;
    }
}
