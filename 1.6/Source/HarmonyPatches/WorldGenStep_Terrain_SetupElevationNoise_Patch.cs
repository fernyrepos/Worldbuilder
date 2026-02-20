using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder;

[HarmonyPatch(typeof(WorldGenStep_Terrain), nameof(WorldGenStep_Terrain.SetupElevationNoise))]
public static class WorldGenStep_Terrain_SetupElevationNoise_Patch
{
    public static void Prefix(ref FloatRange ___ElevationRange)
    {
        if (World_ExposeData_Patch.worldGenerationData is null)
        {
            World_ExposeData_Patch.worldGenerationData = new WorldGenerationData();
            World_ExposeData_Patch.worldGenerationData.Init();
        }

        ___ElevationRange =
            new FloatRange(-500f * World_ExposeData_Patch.worldGenerationData.seaLevel, 5000f);
    }
}
