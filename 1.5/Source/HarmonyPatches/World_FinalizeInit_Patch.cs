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
        public static WorldGrid loadedGridFromPreset = null; // Added static field

        public static void Postfix(World __instance)
        {
            var preset = WorldPresetManager.CurrentlyLoadedPreset;
            if (preset == null) return;

            if (preset.saveTerrain)
            {
                SaveTerrain(__instance, __instance.grid, preset);
            }

            if (preset.saveBases && preset.savedSettlementsData != null)
            {
                SaveBases(__instance, preset);
            }

            if (preset.saveMapMarkers && preset.savedMapMarkersData != null)
            {
                SaveMapMarkets(__instance, preset);
            }

            if (preset.saveWorldFeatures && preset.savedWorldFeaturesData != null)
            {
                SaveWorldFeatures(__instance, preset);
            }
            ModCompatibilityHelper.TrySetMLPSubcount(preset.myLittlePlanetSubcount);
            ModCompatibilityHelper.TrySetWTL(preset.worldTechLevel);
        }
        private static void SaveWorldFeatures(World world, WorldPreset preset)
        {
            world.features.features.RemoveAll(f => f.def == WorldbuilderDefOf.Worldbuilder_MapLabelFeature);
            foreach (var tData in preset.savedWorldFeaturesData)
            {
                var feature = new WorldFeature();
                feature.def = WorldbuilderDefOf.Worldbuilder_MapLabelFeature;
                feature.uniqueID = Find.UniqueIDsManager.GetNextWorldFeatureID();
                feature.name = tData.labelText;
                world.features.features.Add(feature);
                if (tData.tileID >= 0 && tData.tileID < world.grid.TilesCount)
                {
                    world.grid[tData.tileID].feature = feature;
                }
            }
        }
        private static void SaveMapMarkets(World world, WorldPreset preset)
        {
            world.worldObjects.AllWorldObjects.RemoveAll(x => x.def == WorldbuilderDefOf.Worldbuilder_MapMarker);
            foreach (var mData in preset.savedMapMarkersData)
            {
                var marker = (WorldObject)WorldObjectMaker.MakeWorldObject(WorldbuilderDefOf.Worldbuilder_MapMarker);
                marker.Tile = mData.tileID;
                world.worldObjects.Add(marker);
                if (mData.markerData != null)
                    MarkerDataManager.LoadData(marker, mData.markerData);
            }
        }
        private static void SaveBases(World world, WorldPreset preset)
        {
            world.worldObjects.AllWorldObjects.RemoveAll(obj => obj is Settlement);
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
                        var faction = Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f => f.def == factionDef);
                        if (faction != null)
                            settlement.SetFaction(faction);
                    }
                }
                world.worldObjects.Add(settlement);
                if (sData.customData != null)
                    SettlementCustomDataManager.LoadData(settlement, sData.customData);
            }
        }
        private static void SaveTerrain(World world, WorldGrid worldGrid, WorldPreset preset)
        {
            // Apply basic WorldInfo properties regardless of full grid save
            if (!string.IsNullOrEmpty(preset.savedPlanetName))
                world.info.name = preset.savedPlanetName;
            if (preset.savedPlanetCoverage >= 0f)
                world.info.planetCoverage = preset.savedPlanetCoverage;
            if (!string.IsNullOrEmpty(preset.savedSeedString))
                world.info.seedString = preset.savedSeedString;
            // world.info.persistentRandomValue = preset.savedPersistentRandomValue; // Still potentially problematic

            // Check if a full grid was saved in the preset
            if (preset.savedWorldGrid != null)
            {
                loadedGridFromPreset = preset.savedWorldGrid;
                // Do NOT apply individual tile pollution from preset, as it's part of the loaded grid.
            }
            else
            {
                // Apply specific tile pollution ONLY if the full grid wasn't saved
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
}