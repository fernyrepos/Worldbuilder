using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System.Linq;

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
                var marker = (WorldObject)WorldObjectMaker.MakeWorldObject(DefsOf.WB_MapMarker);
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
            foreach (var sData in preset.savedSettlementsData)
            {
                var settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                settlement.Tile = sData.tileID;
                if (!string.IsNullOrEmpty(sData.name))
                    settlement.Name = sData.name;
                if (!string.IsNullOrEmpty(sData.factionDefName))
                {
                    var factionDef = DefDatabase<FactionDef>.GetNamedSilentFail(sData.factionDefName);
                    if (factionDef != null)
                    {
                        if (factionDef.isPlayer) continue;
                        
                        var faction = Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f => f.def == factionDef);
                        if (faction != null)
                            settlement.SetFaction(faction);
                        else
                        {
                            Log.Error($"Failed to find faction {sData.factionDefName} for settlement {sData.name}");
                            continue;
                        }
                    }
                    else
                    {
                        Log.Error($"Failed to find faction def {sData.factionDefName} for settlement {sData.name}");
                        continue;
                    }
                }
                world.worldObjects.Add(settlement);
                if (sData.data != null)
                    CustomizationDataCollections.settlementCustomizationData[settlement] = sData.data;
            }
        }
        private static void RestoreTerrain(World world, WorldGrid worldGrid, WorldPreset preset)
        {
            if (!string.IsNullOrEmpty(preset.savedPlanetName))
                world.info.name = preset.savedPlanetName;
            if (preset.savedPlanetCoverage >= 0f)
                world.info.planetCoverage = preset.savedPlanetCoverage;
            if (!string.IsNullOrEmpty(preset.savedSeedString))
                world.info.seedString = preset.savedSeedString;
            if (preset.worldInfo != null)
            {
                world.info = preset.worldInfo;
            }
            //if (preset.WorldGrid != null)
            //{
            //    loadedGridFromPreset = preset.WorldGrid;
            //}
            
            if (preset.savedTilePollution != null)
            {
                foreach (var kvp in preset.savedTilePollution)
                {
                    if (kvp.Key >= 0 && kvp.Key < worldGrid.TilesCount)
                    {
                        worldGrid[kvp.Key].pollution = kvp.Value;
                    }
                }
            }
        }
    }
}
