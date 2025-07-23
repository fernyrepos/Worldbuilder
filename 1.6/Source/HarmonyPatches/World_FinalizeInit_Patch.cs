using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Worldbuilder
{
    [HotSwappable]
    [HarmonyPatch(typeof(World), nameof(World.FinalizeInit))]
    public static class World_FinalizeInit_Patch
    {
        public static WorldGrid loadedGridFromPreset = null;

        public static void Postfix(World __instance, bool fromLoad)
        {
            if (fromLoad) return;
            var preset = WorldPresetManager.CurrentlyLoadedPreset;
            if (preset == null) return;
            RestoreWorld(__instance, preset);
        }

        public static void RestoreWorld(World __instance, WorldPreset preset)
        {
            if (preset.saveTerrain)
            {
                RestoreTerrain(__instance, __instance.grid, preset);
            }

            if (preset.saveBases && preset.savedSettlementsData != null)
            {
                RestoreBases(__instance, preset);
            }

            if (preset.saveMapMarkers && preset.savedMapMarkersData != null)
            {
                RestoreMapMarkets(__instance, preset);
            }

            if (preset.saveWorldFeatures && preset.savedWorldFeaturesData != null)
            {
                RestoreWorldFeatures(__instance, preset);
            }

            ModCompatibilityHelper.TrySetMLPSubcount(preset.myLittlePlanetSubcount);
            if (preset.saveWorldTechLevel)
            {
                ModCompatibilityHelper.TrySetWTL(preset.worldTechLevel);
            }
        }

        private static void RestoreWorldFeatures(World world, WorldPreset preset)
        {
            world.features.features.RemoveAll(f => f.def == DefsOf.WB_MapLabelFeature);
            foreach (var tData in preset.savedWorldFeaturesData)
            {
                var feature = new WorldFeature();
                feature.def = DefsOf.WB_MapLabelFeature;
                feature.uniqueID = Find.UniqueIDsManager.GetNextWorldFeatureID();
                feature.name = tData.labelText;
                world.features.features.Add(feature);
                if (tData.tileID >= 0 && tData.tileID < world.grid.TilesCount)
                {
                    world.grid[tData.tileID].feature = feature;
                }
            }
        }

        private static void RestoreMapMarkets(World world, WorldPreset preset)
        {
            Utils.GetSurfaceWorldObjects<WorldObject_MapMarker>().ToList().ForEach(x => world.worldObjects.Remove(x));
            foreach (var mData in preset.savedMapMarkersData)
            {
                var marker = WorldObjectMaker.MakeWorldObject(DefsOf.WB_MapMarker);
                marker.Tile = mData.tileID;
                world.worldObjects.Add(marker);
                if (mData.markerData != null)
                    MarkerDataManager.LoadData(marker, mData.markerData);
            }
        }
        private static void RestoreBases(World world, WorldPreset preset)
        {
            var settlements = Utils.GetSurfaceWorldObjects<Settlement>().ToList();
            settlements.ForEach(x => world.worldObjects.Remove(x));
            foreach (var data in preset.savedSettlementsData)
            {
                var settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                settlement.Tile = data.tileID;
                settlement.Name = data.name;
                if (data.faction != null)
                {
                    if (data.faction.isPlayer) continue;

                    var faction = Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f => f.def == data.faction);
                    if (faction != null)
                        settlement.SetFaction(faction);
                    else
                    {
                        Log.Error($"Failed to find faction {data.faction} for settlement {data.name}");
                        continue;
                    }
                }
                else
                {
                    Log.Error($"Failed to find faction for settlement {data.name}");
                    continue;
                }
                world.worldObjects.Add(settlement);
                if (data.data != null)
                    CustomizationDataCollections.settlementCustomizationData[settlement] = data.data;
            }
        }
        private static void RestoreTerrain(World world, WorldGrid worldGrid, WorldPreset preset)
        {
            if (preset.worldInfo != null)
            {
                world.info = preset.worldInfo;
            }
            var terrainData = preset.TerrainData;
            var gameSurface = Find.WorldGrid.Surface;
            if (terrainData != null && gameSurface != null)
            {
                gameSurface.tileBiome = terrainData.tileBiome;
                gameSurface.tileElevation = terrainData.tileElevation;
                gameSurface.tileHilliness = terrainData.tileHilliness;
                gameSurface.tileTemperature = terrainData.tileTemperature;
                gameSurface.tileRainfall = terrainData.tileRainfall;
                gameSurface.tileSwampiness = terrainData.tileSwampiness;
                gameSurface.tileFeature = terrainData.tileFeature;
                gameSurface.tilePollution = terrainData.tilePollution;
                gameSurface.tileRoadOrigins = terrainData.tileRoadOrigins;
                gameSurface.tileRoadAdjacency = terrainData.tileRoadAdjacency;
                gameSurface.tileRoadDef = terrainData.tileRoadDef;
                gameSurface.tileRiverOrigins = terrainData.tileRiverOrigins;
                gameSurface.tileRiverAdjacency = terrainData.tileRiverAdjacency;
                gameSurface.tileRiverDef = terrainData.tileRiverDef;
                gameSurface.tileRiverDistances = terrainData.tileRiverDistances;
                gameSurface.tileMutatorTiles = terrainData.tileMutatorTiles;
                gameSurface.tileMutatorDefs = terrainData.tileMutatorDefs;
                gameSurface.RawDataToTiles();
                WorldGrid grid = Find.WorldGrid;
                Find.WorldFeatures.features = terrainData.features.ToList();
                if (gameSurface.tileFeature != null && gameSurface.tileFeature.Length != 0)
                {
                    DataSerializeUtility.LoadUshort(gameSurface.tileFeature, grid.TilesCount, delegate (int i, ushort data)
                    {
                        grid[i].feature = ((data == ushort.MaxValue) ? null : Find.WorldFeatures.GetFeatureWithID(data));
                    });
                }
                Find.WorldFeatures.textsCreated = false;
                Find.World.landmarks.landmarks.Clear();
                if (terrainData.landmarks != null)
                {
                    foreach (var kvp in terrainData.landmarks)
                    {
                        Find.World.landmarks.landmarks.Add(kvp.Key, kvp.Value);
                    }
                }
            }
        }
    }
}
