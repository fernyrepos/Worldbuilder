using HarmonyLib;
using RimWorld;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Page_CreateWorldParams), nameof(Page_CreateWorldParams.Reset))]
    public static class Page_CreateWorldParams_Reset_Patch
    {
        public static void Postfix(Page_CreateWorldParams __instance)
        {
            var preset = WorldPresetManager.CurrentlyLoadedPreset;

            if (preset != null && preset.name != "Default" && preset.saveGenerationParameters && preset.generationData != null)
            {
                World_ExposeData_Patch.worldGenerationData = preset.generationData.MakeCopy();

                if (preset.disableExtraBiomes)
                {
                    foreach (var biomeDef in Utils.GetValidBiomes())
                    {
                        if (!World_ExposeData_Patch.worldGenerationData.biomeCommonalities.ContainsKey(biomeDef.defName))
                        {
                            World_ExposeData_Patch.worldGenerationData.biomeCommonalities[biomeDef.defName] = 0;
                        }
                    }
                }
            }

            Page_CreateWorldParams_DoWindowContents_Patch.curPlanetName = NameGenerator.GenerateName(RulePackDefOf.NamerWorld);

            PlanetLayerSettingsDefOf.Surface.settings.subdivisions = 10;

            ModCompatibilityHelper.TrySetMLPSubcount(10);

            if (ModsConfig.IsActive(ModCompatibilityHelper.WorldTechLevelPackageId))
            {
                ModCompatibilityHelper.TrySetWTL(TechLevel.Archotech);
            }

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
            else if (preset.saveGenerationParameters && preset.generationData != null)
            {
                __instance.planetCoverage = preset.generationData.planetCoverage;
                __instance.pollution = preset.generationData.pollution;
                __instance.rainfall = preset.generationData.rainfall;
                __instance.temperature = preset.generationData.temperature;
                __instance.population = preset.generationData.population;
                __instance.landmarkDensity = preset.generationData.landmarkDensity;
                if (!string.IsNullOrEmpty(preset.generationData.seedString))
                {
                    __instance.seedString = preset.generationData.seedString;
                }
            }

            if (preset.saveWorldTechLevel)
            {
                ModCompatibilityHelper.TrySetWTL(preset.worldTechLevel);
            }

            ModCompatibilityHelper.TrySetMLPSubcount(preset.myLittlePlanetSubcount);
            PlanetLayerSettingsDefOf.Surface.settings.subdivisions = preset.myLittlePlanetSubcount;
        }
    }
}
