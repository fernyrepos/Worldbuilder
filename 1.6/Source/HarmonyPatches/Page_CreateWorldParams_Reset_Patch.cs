using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Page_CreateWorldParams), nameof(Page_CreateWorldParams.Reset))]
    public static class Page_CreateWorldParams_Reset_Patch
    {
        public static void Postfix(Page_CreateWorldParams __instance)
        {
            var preset = WorldPresetManager.CurrentlyLoadedPreset;
            if (preset == null || preset.name == "Default") return;

            if (preset.saveTerrain)
            {
                if (preset.worldInfo != null)
                {
                    __instance.seedString = preset.worldInfo.seedString;
                    __instance.planetCoverage = preset.worldInfo.planetCoverage;
                    __instance.pollution = preset.worldInfo.pollution;
                    __instance.rainfall = preset.worldInfo.overallRainfall;
                    __instance.temperature = preset.worldInfo.overallTemperature;
                    __instance.population = preset.worldInfo.overallPopulation;
                    __instance.landmarkDensity = preset.worldInfo.landmarkDensity;
                }
            }
            ModCompatibilityHelper.TrySetMLPSubcount(preset.myLittlePlanetSubcount);
            ModCompatibilityHelper.TrySetWTL(preset.worldTechLevel);
        }
    }
}
