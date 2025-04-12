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

            __instance.rainfall = preset.rainfall;
            __instance.temperature = preset.temperature;
            __instance.population = preset.population;
            if (preset.saveTerrain)
            {
                if (!string.IsNullOrEmpty(preset.savedSeedString))
                {
                    __instance.seedString = preset.savedSeedString;
                }
                if (preset.savedPlanetCoverage >= 0f)
                {
                    __instance.planetCoverage = preset.savedPlanetCoverage;
                }
                if (preset.savedPollution >= 0f)
                {
                    __instance.pollution = preset.savedPollution;
                }
            }
            ModCompatibilityHelper.TrySetMLPSubcount(preset.myLittlePlanetSubcount);
            ModCompatibilityHelper.TrySetWTL(preset.worldTechLevel);
        }
    }
}