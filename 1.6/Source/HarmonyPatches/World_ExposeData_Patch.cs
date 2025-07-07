using HarmonyLib;
using RimWorld;
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
        public static Dictionary<FactionDef, string> individualFactionDescriptions = new Dictionary<FactionDef, string>();
        public static Dictionary<FactionDef, string> individualFactionNames = new Dictionary<FactionDef, string>();
        private static List<FactionDef> factionKeysWorkingList, factionKeysWorkingList2 = new List<FactionDef>();
        private static List<string> factionValuesWorkingList, factionValuesWorkingList2 = new List<string>();
        private static List<Settlement> settlementKeysWorkingList = new List<Settlement>();
        private static List<SettlementCustomData> settlementValuesWorkingList = new List<SettlementCustomData>();
        public static bool showCustomization = true;

        public static void CleanWorldData()
        {
            MarkerDataManager.ClearData();
            playerFactionName = null;
            CustomizationDataCollections.settlementCustomizationData = new Dictionary<Settlement, SettlementCustomData>();
            CustomizationDataCollections.thingCustomizationData = new Dictionary<Thing, CustomizationData>();
            CustomizationDataCollections.playerDefaultCustomizationData = new Dictionary<ThingDef, CustomizationData>();
            CustomizationDataCollections.explicitlyCustomizedThings = new HashSet<Thing>();
            worldStories = new List<Story>();
            individualFactionDescriptions = new Dictionary<FactionDef, string>();
            individualFactionNames = new Dictionary<FactionDef, string>();
            worldPresetName = null;
            showCustomization = true;
        }
        public static void Prefix()
        {
            try
            {
                Scribe_Collections.Look(ref CustomizationDataCollections.playerDefaultCustomizationData,
                    "playerDefaultCustomizationData", LookMode.Def, LookMode.Deep);
                Scribe_Collections.Look(ref CustomizationDataCollections.settlementCustomizationData,
                    "settlementCustomizationData", LookMode.Reference, LookMode.Deep, ref settlementKeysWorkingList, ref settlementValuesWorkingList);
                Scribe_Collections.Look(ref individualFactionDescriptions, "individualFactionDescriptions", LookMode.Def, LookMode.Value, ref factionKeysWorkingList, ref factionValuesWorkingList);
                Scribe_Collections.Look(ref individualFactionNames, "individualFactionNames", LookMode.Def, LookMode.Value, ref factionKeysWorkingList2, ref factionValuesWorkingList2);
                if (Scribe.mode == LoadSaveMode.Saving)
                {
                    worldPresetName = WorldPresetManager.CurrentlyLoadedPreset?.name;
                    SettlementCustomDataManager.CleanupOrphanedData();
                }
                Scribe_Values.Look(ref worldPresetName, "worldPresetName");
                Scribe_Values.Look(ref playerFactionName, "playerFactionName");
                Scribe_Collections.Look(ref worldStories, "worldStories", LookMode.Deep);
                Scribe_Values.Look(ref showCustomization, "showCustomization", defaultValue: true);

                if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    CustomizationDataCollections.playerDefaultCustomizationData ??= new Dictionary<ThingDef, CustomizationData>();
                    worldStories ??= new List<Story>();
                    CustomizationDataCollections.settlementCustomizationData ??= new Dictionary<Settlement, SettlementCustomData>();
                    individualFactionDescriptions ??= new Dictionary<FactionDef, string>();
                    individualFactionNames ??= new Dictionary<FactionDef, string>();
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
