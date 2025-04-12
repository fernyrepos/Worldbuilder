using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(WorldGenStep_Terrain), nameof(WorldGenStep_Terrain.GenerateGridIntoWorld))]
    public static class WorldGenStep_Terrain_GenerateGridIntoWorld_Patch
    {
        public static bool Prefix()
        {
            if (World_FinalizeInit_Patch.loadedGridFromPreset != null)
            {
                var world = Find.World;
                world.grid = World_FinalizeInit_Patch.loadedGridFromPreset;
                World_FinalizeInit_Patch.loadedGridFromPreset = null;
                return false;
            }
            return true;
        }
    }
}