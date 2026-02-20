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
        private static string worldPresetName;
        private static List<string> factionPopulationKeysWorkingList = new List<string>();
        private static List<FactionPopulationData> factionPopulationValuesWorkingList = new List<FactionPopulationData>();

        private struct OriginalFactionValues
        {
            public string pawnSingular;
            public string pawnsPlural;
            public string leaderTitle;
            public TechLevel techLevel;
            public bool permanentEnemy;
        }

        private static Dictionary<FactionDef, OriginalFactionValues> originalFactionDefValues = new Dictionary<FactionDef, OriginalFactionValues>();

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
        public static Dictionary<int, string> factionDescriptionsById = new Dictionary<int, string>();
        public static Dictionary<int, string> factionNamesById = new Dictionary<int, string>();
        public static Dictionary<FactionDef, string> individualFactionDescriptions = new Dictionary<FactionDef, string>();
        public static Dictionary<FactionDef, string> individualFactionNames = new Dictionary<FactionDef, string>();
        public static Dictionary<FactionDef, string> individualFactionIcons = new Dictionary<FactionDef, string>();
        public static Dictionary<FactionDef, IdeoIconDef> individualFactionIdeoIcons = new Dictionary<FactionDef, IdeoIconDef>();
        private static List<string> settlementKeysWorkingList = new List<string>();
        private static List<SettlementCustomData> settlementValuesWorkingList = new List<SettlementCustomData>();
        public static bool showCustomization = true;
        public static WorldGenerationData worldGenerationData;
        public static void ApplyPopulationCustomization(FactionDef def, FactionPopulationData data)
        {
            if (def == null || data == null) return;

            if (!originalFactionDefValues.ContainsKey(def))
            {
                originalFactionDefValues[def] = new OriginalFactionValues
                {
                    pawnSingular = def.pawnSingular,
                    pawnsPlural = def.pawnsPlural,
                    leaderTitle = def.leaderTitle,
                    techLevel = def.techLevel,
                    permanentEnemy = def.permanentEnemy
                };
            }

            if (!string.IsNullOrEmpty(data.pawnSingular)) def.pawnSingular = data.pawnSingular;
            if (!string.IsNullOrEmpty(data.pawnsPlural)) def.pawnsPlural = data.pawnsPlural;
            if (!string.IsNullOrEmpty(data.leaderTitle)) def.leaderTitle = data.leaderTitle;
            if (data.techLevel.HasValue) def.techLevel = data.techLevel.Value;
            if (data.permanentEnemy.HasValue) def.permanentEnemy = data.permanentEnemy.Value;
        }

        public static void CleanWorldData()
        {
            foreach (var kvp in originalFactionDefValues)
            {
                var def = kvp.Key;
                var vals = kvp.Value;
                def.pawnSingular = vals.pawnSingular;
                def.pawnsPlural = vals.pawnsPlural;
                def.leaderTitle = vals.leaderTitle;
                def.techLevel = vals.techLevel;
                def.permanentEnemy = vals.permanentEnemy;
            }
            originalFactionDefValues.Clear();

            MarkerDataManager.ClearData();
            playerFactionName = null;
            worldPresetName = null;
            CustomizationDataCollections.settlementCustomizationData = new Dictionary<string, SettlementCustomData>();
            CustomizationDataCollections.thingCustomizationData = new Dictionary<Thing, CustomizationData>();
            CustomizationDataCollections.playerDefaultCustomizationData = new Dictionary<ThingDef, CustomizationData>();
            CustomizationDataCollections.explicitlyCustomizedThings = new HashSet<Thing>();
            CustomizationDataCollections.factionPopulationData.Clear();
            worldStories = new List<Story>();
            factionDescriptionsById = new Dictionary<int, string>();
            factionNamesById = new Dictionary<int, string>();
            individualFactionDescriptions = new Dictionary<FactionDef, string>();
            individualFactionNames = new Dictionary<FactionDef, string>();
            individualFactionIcons = new Dictionary<FactionDef, string>();
            individualFactionIdeoIcons = new Dictionary<FactionDef, IdeoIconDef>();
            WorldPresetManager.CurrentlyLoadedPreset = null;
            World_FinalizeInit_Patch.axialTilt = AxialTilt.Normal;
            worldGenerationData = new WorldGenerationData();
            worldGenerationData.Init();
        }

        public static void Postfix()
        {
            try
            {
                Scribe_Collections.Look(ref CustomizationDataCollections.playerDefaultCustomizationData,
                    "playerDefaultCustomizationData", LookMode.Def, LookMode.Deep);
                Scribe_Collections.Look(ref CustomizationDataCollections.settlementCustomizationData,
                    "settlementCustomizationData", LookMode.Value, LookMode.Deep, ref settlementKeysWorkingList, ref settlementValuesWorkingList);
                Scribe_Collections.Look(ref CustomizationDataCollections.factionPopulationData, "factionPopulationData", LookMode.Value, LookMode.Deep, ref factionPopulationKeysWorkingList, ref factionPopulationValuesWorkingList);
                Scribe_Collections.Look(ref factionDescriptionsById, "factionDescriptionsById", LookMode.Value, LookMode.Value);
                Scribe_Collections.Look(ref factionNamesById, "factionNamesById", LookMode.Value, LookMode.Value);
                Scribe_Collections.Look(ref individualFactionDescriptions, "individualFactionDescriptions", LookMode.Def, LookMode.Value);
                Scribe_Collections.Look(ref individualFactionNames, "individualFactionNames", LookMode.Def, LookMode.Value);
                Scribe_Collections.Look(ref individualFactionIcons, "individualFactionIcons", LookMode.Def, LookMode.Value);
                Scribe_Collections.Look(ref individualFactionIdeoIcons, "individualFactionIdeoIcons", LookMode.Def, LookMode.Def);
                Scribe_Values.Look(ref worldPresetName, "worldPresetName");
                Scribe_Values.Look(ref playerFactionName, "playerFactionName");
                Scribe_Collections.Look(ref worldStories, "worldStories", LookMode.Deep);
                Scribe_Values.Look(ref showCustomization, "showCustomization", defaultValue: true);
                Scribe_Values.Look(ref World_FinalizeInit_Patch.axialTilt, "axialTilt", AxialTilt.Normal, true);
                Scribe_Deep.Look(ref worldGenerationData, "worldGenerationData");
            }
            catch (System.Exception ex)
            {
                Log.Error($"Worldbuilder: Error saving world data: {ex.Message}");
            }

            CustomizationDataCollections.playerDefaultCustomizationData ??= new Dictionary<ThingDef, CustomizationData>();
            worldStories ??= new List<Story>();
            CustomizationDataCollections.settlementCustomizationData ??= new Dictionary<string, SettlementCustomData>();
            CustomizationDataCollections.factionPopulationData ??= new Dictionary<string, FactionPopulationData>();
            factionDescriptionsById ??= new Dictionary<int, string>();
            factionNamesById ??= new Dictionary<int, string>();
            individualFactionDescriptions ??= new Dictionary<FactionDef, string>();
            individualFactionNames ??= new Dictionary<FactionDef, string>();
            individualFactionIcons ??= new Dictionary<FactionDef, string>();
            individualFactionIdeoIcons ??= new Dictionary<FactionDef, IdeoIconDef>();
            try
            {
                if (Scribe.mode == LoadSaveMode.PostLoadInit && Find.World?.factionManager != null && (individualFactionDescriptions.Count > 0 || individualFactionNames.Count > 0) && factionDescriptionsById.Count == 0 && factionNamesById.Count == 0)
                {
                    foreach (var faction in Find.World.factionManager.AllFactionsListForReading)
                    {
                        if (individualFactionDescriptions.TryGetValue(faction.def, out var description))
                        {
                            factionDescriptionsById[faction.loadID] = description;
                        }
                        if (individualFactionNames.TryGetValue(faction.def, out var name))
                        {
                            factionNamesById[faction.loadID] = name;
                        }
                    }
                    individualFactionDescriptions.Clear();
                    individualFactionNames.Clear();
                }

                if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    foreach (var kvp in CustomizationDataCollections.factionPopulationData)
                    {
                        if (!kvp.Key.StartsWith("Faction_")) continue;
                        if (!int.TryParse(kvp.Key.Substring("Faction_".Length), out int loadID)) continue;
                        var faction = Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f => f.loadID == loadID);
                        if (faction != null)
                        {
                            ApplyPopulationCustomization(faction.def, kvp.Value);
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"Worldbuilder: Error migrating faction data: {ex.Message}");
            }
        }
    }
}
