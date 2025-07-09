using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(GameSetupStep_Grids), nameof(GameSetupStep_Grids.GenerateFresh))]
    public static class GameSetupStep_Grids_GenerateFresh_Patch
    {
        public static bool Prefix()
        {
            if (World_FinalizeInit_Patch.loadedGridFromPreset != null)
            {
                var world = Find.World;
                var savedSurfaceData = World_FinalizeInit_Patch.loadedGridFromPreset.FirstLayerOfDef(PlanetLayerDefOf.Surface);
                var worldSurfaceData = world.grid.FirstLayerOfDef(PlanetLayerDefOf.Surface);
                
                World_FinalizeInit_Patch.loadedGridFromPreset = null;
                return false;
            }
            return true;
        }
    }
}
