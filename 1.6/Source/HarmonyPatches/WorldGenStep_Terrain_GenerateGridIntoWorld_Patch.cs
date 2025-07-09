using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    //[HarmonyPatch(typeof(GameSetupStep_Grids), nameof(GameSetupStep_Grids.GenerateFresh))]
    //public static class GameSetupStep_Grids_GenerateFresh_Patch
    //{
    //    public static bool Prefix()
    //    {
    //        if (World_FinalizeInit_Patch.loadedGridFromPreset != null)
    //        {
    //            var world = Find.World;
    //            var savedSurfaceData = World_FinalizeInit_Patch.loadedGridFromPreset.FirstLayerOfDef//(PlanetLayerDefOf.Surface) as SurfaceLayer;
    //            var worldSurfaceData = world.grid.FirstLayerOfDef(PlanetLayerDefOf.Surface) as //SurfaceLayer;                
    //            worldSurfaceData.tileBiome = savedSurfaceData.tileBiome;
    //            worldSurfaceData.tileElevation = savedSurfaceData.tileElevation;
    //            worldSurfaceData.tileHilliness = savedSurfaceData.tileHilliness;
    //            worldSurfaceData.tileTemperature = savedSurfaceData.tileTemperature;
    //            worldSurfaceData.tileRainfall = savedSurfaceData.tileRainfall;
    //            worldSurfaceData.tileSwampiness = savedSurfaceData.tileSwampiness;
    //            worldSurfaceData.tileFeature = savedSurfaceData.tileFeature;
    //            worldSurfaceData.tilePollution = savedSurfaceData.tilePollution;
    //            worldSurfaceData.tileRoadOrigins = savedSurfaceData.tileRoadOrigins;
    //            worldSurfaceData.tileRoadAdjacency = savedSurfaceData.tileRoadAdjacency;
    //            worldSurfaceData.tileRoadDef = savedSurfaceData.tileRoadDef;
    //            worldSurfaceData.tileRiverOrigins = savedSurfaceData.tileRiverOrigins;
    //            worldSurfaceData.tileRiverAdjacency = savedSurfaceData.tileRiverAdjacency;
    //            worldSurfaceData.tileRiverDef = savedSurfaceData.tileRiverDef;
    //            worldSurfaceData.tileRiverDistances = savedSurfaceData.tileRiverDistances;
    //            worldSurfaceData.tileMutatorTiles = savedSurfaceData.tileMutatorTiles;
    //            worldSurfaceData.tileMutatorDefs = savedSurfaceData.tileMutatorDefs;
    //            World_FinalizeInit_Patch.loadedGridFromPreset = null;
    //            return false;
    //        }
    //        return true;
    //    }
    //}
}
