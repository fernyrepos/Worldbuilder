using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System.Linq;
using System;

namespace Worldbuilder
{
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
            ModCompatibilityHelper.TrySetWTL(preset.worldTechLevel);
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

            if (preset.savedTilePollution != null)
            {
                foreach (var kvp in preset.savedTilePollution)
                {
                    if (kvp.Key >= 0 && kvp.Key < worldGrid.TilesCount)
                    {
                        worldGrid[kvp.Key].pollution = kvp.Value;
                    }
                }

                if (preset.savedRoadsData != null)
                {
                    foreach (SurfaceTile tile in Find.WorldGrid.Surface.Tiles.Cast<SurfaceTile>())
                    {
                        tile.potentialRoads = null;
                    }

                    foreach (var roadData in preset.savedRoadsData)
                    {
                        if (roadData.fromTileID >= 0 && roadData.fromTileID < worldGrid.TilesCount &&
                            roadData.toTileID >= 0 && roadData.toTileID < worldGrid.TilesCount &&
                            roadData.roadDef != null)
                        {
                            worldGrid.OverlayRoad(new PlanetTile(roadData.fromTileID, Find.WorldGrid.Surface), new PlanetTile(roadData.toTileID, Find.WorldGrid.Surface), roadData.roadDef);
                        }
                    }
                }
            }
        }
    }
}
