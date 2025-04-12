using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(WorldGenStep_Terrain), nameof(WorldGenStep_Terrain.GenerateGridIntoWorld))]
    public static class WorldGenStep_Terrain_GenerateGridIntoWorld_Patch
    {
        public static bool Prefix() // Corrected signature: no parameters
        {
            // Check if a grid was loaded from the preset in World_FinalizeInit_Patch
            if (World_FinalizeInit_Patch.loadedGridFromPreset != null)
            {
                var world = Find.World; // Get world instance
                // Replace the world's grid instance with the one loaded from the preset
                world.grid = World_FinalizeInit_Patch.loadedGridFromPreset;

                // Clear the static holder now that we've used it
                World_FinalizeInit_Patch.loadedGridFromPreset = null;

                // Skip the original terrain generation method
                return false;
            }

            // No preset grid loaded, proceed with original terrain generation
            return true;
        }
    }
}