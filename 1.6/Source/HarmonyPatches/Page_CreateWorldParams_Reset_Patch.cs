using HarmonyLib;
using RimWorld;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Page_CreateWorldParams), nameof(Page_CreateWorldParams.Reset))]
    public static class Page_CreateWorldParams_Reset_Patch
    {
        public static void Postfix(Page_CreateWorldParams __instance)
        {
            if (Page_CreateWorldParams_DoWindowContents_Patch.tmpGenerationData != null)
            {
                Page_CreateWorldParams_DoWindowContents_Patch.tmpGenerationData.Reset();
            }

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
            if (preset.saveWorldTechLevel)
            {
                ModCompatibilityHelper.TrySetWTL(preset.worldTechLevel);
            }
        }
    }
}
