using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(World), nameof(World.ExposeData))]
    public static class World_ExposeData_Patch
    {
        private static string worldPresetName;

        public static string WorldPresetName
        {
            get
            {
                return worldPresetName;
            }
            set
            {
                worldPresetName = value;
                WorldPresetManager.CurrentlyLoadedPreset = null;
            }
        }
        public static string playerFactionName;
        public static List<Story> worldStories = new List<Story>();
        public static Dictionary<FactionDef, string> individualFactionDescriptions = new Dictionary<FactionDef, string>();
        public static Dictionary<FactionDef, string> individualFactionNames = new Dictionary<FactionDef, string>();
        private static List<Settlement> settlementKeysWorkingList = new List<Settlement>();
        private static List<SettlementCustomData> settlementValuesWorkingList = new List<SettlementCustomData>();
        public static bool showCustomization = true;
        public static Dictionary<int, TileChanges> tileChanges = new Dictionary<int, TileChanges>();

        public static void CleanWorldData()
        {
            MarkerDataManager.ClearData();
            playerFactionName = null;
            worldPresetName = null;
            CustomizationDataCollections.settlementCustomizationData = new Dictionary<Settlement, SettlementCustomData>();
            CustomizationDataCollections.thingCustomizationData = new Dictionary<Thing, CustomizationData>();
            CustomizationDataCollections.playerDefaultCustomizationData = new Dictionary<ThingDef, CustomizationData>();
            CustomizationDataCollections.explicitlyCustomizedThings = new HashSet<Thing>();
            worldStories = new List<Story>();
            individualFactionDescriptions = new Dictionary<FactionDef, string>();
            individualFactionNames = new Dictionary<FactionDef, string>();
            tileChanges = new Dictionary<int, TileChanges>();
        }

        public static void Prefix()
        {
            try
            {
                Scribe_Collections.Look(ref CustomizationDataCollections.playerDefaultCustomizationData,
                    "playerDefaultCustomizationData", LookMode.Def, LookMode.Deep);
                Scribe_Collections.Look(ref CustomizationDataCollections.settlementCustomizationData,
                    "settlementCustomizationData", LookMode.Reference, LookMode.Deep, ref settlementKeysWorkingList, ref settlementValuesWorkingList);
                Scribe_Collections.Look(ref individualFactionDescriptions, "individualFactionDescriptions", LookMode.Def, LookMode.Value);
                Scribe_Collections.Look(ref individualFactionNames, "individualFactionNames", LookMode.Def, LookMode.Value);
                Scribe_Values.Look(ref worldPresetName, "worldPresetName");
                Scribe_Values.Look(ref playerFactionName, "playerFactionName");
                Scribe_Collections.Look(ref worldStories, "worldStories", LookMode.Deep);
                Scribe_Values.Look(ref showCustomization, "showCustomization", defaultValue: true);
                Scribe_Collections.Look(ref tileChanges, "tileChanges", LookMode.Value, LookMode.Deep);
            }
            catch (System.Exception ex)
            {
                Log.Error($"Worldbuilder: Error saving world data: {ex.Message}");
            }

            CustomizationDataCollections.playerDefaultCustomizationData ??= new Dictionary<ThingDef, CustomizationData>();
            worldStories ??= new List<Story>();
            CustomizationDataCollections.settlementCustomizationData ??= new Dictionary<Settlement, SettlementCustomData>();
            individualFactionDescriptions ??= new Dictionary<FactionDef, string>();
            individualFactionNames ??= new Dictionary<FactionDef, string>();
            tileChanges ??= new Dictionary<int, TileChanges>();
        }
    }
}
