using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder;

[HarmonyPatch(typeof(WorldGenStep_Terrain), nameof(WorldGenStep_Terrain.SetupElevationNoise))]
public static class WorldGenStep_Terrain_SetupElevationNoise_Patch
{
    public static void Prefix(ref FloatRange ___ElevationRange)
    {
        if (Page_CreateWorldParams_DoWindowContents_Patch.tmpGenerationData is null)
        {
            Page_CreateWorldParams_DoWindowContents_Patch.tmpGenerationData = new WorldGenerationData();
            Page_CreateWorldParams_DoWindowContents_Patch.tmpGenerationData.Init();
        }

        ___ElevationRange =
            new FloatRange(-500f * Page_CreateWorldParams_DoWindowContents_Patch.tmpGenerationData.seaLevel, 5000f);
    }
}
