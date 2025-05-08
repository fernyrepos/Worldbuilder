using HarmonyLib;
using RimWorld.Planet;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(World), nameof(World.ExposeData))]
    public static class World_ExposeData_Patch
    {
        public static string worldPresetName;
        public static string playerFactionName;
        public static List<Story> worldStories = new List<Story>();
        private static List<Settlement> settlementKeysWorkingList = new List<Settlement>();
        private static List<SettlementCustomData> settlementValuesWorkingList = new List<SettlementCustomData>();

        public static void CleanWorldData()
        {
            playerFactionName = null;
            CustomizationDataCollections.settlementCustomizationData = new Dictionary<Settlement, SettlementCustomData>();
            CustomizationDataCollections.thingCustomizationData = new Dictionary<Thing, CustomizationData>();
            CustomizationDataCollections.playerDefaultCustomizationData = new Dictionary<ThingDef, CustomizationData>();
            CustomizationDataCollections.explicitlyCustomizedThings = new HashSet<Thing>();
            worldStories = new List<Story>();
            worldPresetName = null;
        }
        public static void Prefix()
        {
            try
            {
                Scribe_Collections.Look(ref CustomizationDataCollections.playerDefaultCustomizationData,
                    "playerDefaultCustomizationData", LookMode.Def, LookMode.Deep);
                Scribe_Collections.Look(ref CustomizationDataCollections.settlementCustomizationData,
                    "settlementCustomizationData", LookMode.Reference, LookMode.Deep, ref settlementKeysWorkingList, ref settlementValuesWorkingList);

                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    worldPresetName = WorldPresetManager.CurrentlyLoadedPreset?.name;
                    SettlementCustomDataManager.CleanupOrphanedData();
                }
                Scribe_Values.Look(ref worldPresetName, "worldPresetName");
                Scribe_Values.Look(ref playerFactionName, "playerFactionName");

                if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    CustomizationDataCollections.playerDefaultCustomizationData ??= new Dictionary<ThingDef, CustomizationData>();
                    worldStories ??= new List<Story>();
                    CustomizationDataCollections.settlementCustomizationData ??= new Dictionary<Settlement, SettlementCustomData>();
                }

                var currentPreset = WorldPresetManager.CurrentlyLoadedPreset;
                bool saveToPreset = currentPreset != null && currentPreset.saveStorykeeperEntries;

                LookMode storyLookMode = saveToPreset ? LookMode.Deep : LookMode.Deep;

                if (saveToPreset)
                {
                    Scribe_Collections.Look(ref currentPreset.presetStories, "presetStories", storyLookMode, new object[0]);
                }
                else
                {
                    Scribe_Collections.Look(ref worldStories, "worldStories", storyLookMode, new object[0]);
                }

                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    List<WorldObject> currentMarkers = Find.WorldObjects.AllWorldObjects.FindAll(wo => wo.def == WorldbuilderDefOf.Worldbuilder_MapMarker);
                    MarkerDataManager.CleanupOrphanedData(currentMarkers);
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"Worldbuilder: Error saving world data: {ex.Message}");
            }
        }
        public static SettlementCustomData GetPresetSettlementCustomizationData(Settlement settlement)
        {
            if (settlement == null) return null;
            CustomizationDataCollections.settlementCustomizationData ??= new Dictionary<Settlement, SettlementCustomData>();
            if (CustomizationDataCollections.settlementCustomizationData.TryGetValue(settlement, out var customData))
            {
                return customData;
            }
            var currentPreset = WorldPresetManager.CurrentlyLoadedPreset;
            if (currentPreset?.factionSettlementCustomizationDefaults != null && settlement.Faction != null)
            {
                if (currentPreset.factionSettlementCustomizationDefaults.TryGetValue(settlement.Faction.def, out var presetDefaultData))
                {
                    return presetDefaultData;
                }
            }

            return null;
        }
    }
}
