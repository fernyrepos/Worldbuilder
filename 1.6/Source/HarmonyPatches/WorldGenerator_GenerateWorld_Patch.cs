using HarmonyLib;
using RimWorld.Planet;
using RimWorld;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GenerateWorld))]
    public static class WorldGenerator_GenerateWorld_Patch
    {
        public static void Prefix()
        {
            var preset = WorldPresetManager.CurrentlyLoadedPreset;
            if (preset == null) return;

            ModCompatibilityHelper.TrySetMLPSubcount(preset.myLittlePlanetSubcount);
            if (preset.worldTechLevel != TechLevel.Undefined)
            {
                ModCompatibilityHelper.TrySetWTL(preset.worldTechLevel);
            }
        }
    }
}