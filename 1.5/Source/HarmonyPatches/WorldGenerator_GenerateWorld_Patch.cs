using HarmonyLib;
using RimWorld.Planet;

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
            ModCompatibilityHelper.TrySetWTL(preset.worldTechLevel);
        }
    }
}