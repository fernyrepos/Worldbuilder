using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using System.Collections.Generic;
using System.Linq;

namespace Worldbuilder
{
    public class SettlementCustomData : IExposable
    {
        public string narrativeText = "";
        public string selectedFactionIconDefName;
        public string selectedCulturalIconDefName;
        public string description;
        public FactionDef SelectedFactionIconDef => string.IsNullOrEmpty(selectedFactionIconDefName) ? null : DefDatabase<FactionDef>.GetNamed(selectedFactionIconDefName, false);
        public IdeoIconDef SelectedCulturalIconDef => string.IsNullOrEmpty(selectedCulturalIconDefName) ? null : DefDatabase<IdeoIconDef>.GetNamed(selectedCulturalIconDefName, false);

        public void ExposeData()
        {
            Scribe_Values.Look(ref narrativeText, "narrativeText", "");
            Scribe_Values.Look(ref selectedFactionIconDefName, "selectedFactionIconDefName");
            Scribe_Values.Look(ref selectedCulturalIconDefName, "selectedCulturalIconDefName");
            Scribe_Values.Look(ref description, "description");
        }
        public SettlementCustomData Copy()
        {
            return new SettlementCustomData
            {
                narrativeText = this.narrativeText,
                selectedFactionIconDefName = this.selectedFactionIconDefName,
                selectedCulturalIconDefName = this.selectedCulturalIconDefName,
                description = this.description
            };
        }
    }


    [HarmonyPatch(typeof(Game), "ExposeData")]
    public static class Game_ExposeData_Patch
    {
        public static string worldPresetName;
        public static List<Story> worldStories = new List<Story>();

        public static void Postfix()
        {
            Scribe_Collections.Look(ref CustomizationDataCollections.playerDefaultCustomizationData,
                "playerDefaultCustomizationData", LookMode.Def, LookMode.Deep);
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                worldPresetName = WorldPresetManager.CurrentlyLoadedPreset?.name;
            }
            Scribe_Values.Look(ref worldPresetName, "worldPresetName");
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                CustomizationDataCollections.playerDefaultCustomizationData ??= new Dictionary<ThingDef, CustomizationData>();
                worldStories ??= new List<Story>();
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
                List<WorldObject> currentMarkers = Find.WorldObjects.AllWorldObjects.FindAll(wo => wo.def.defName == "Worldbuilder_MapMarker");
                MarkerDataManager.CleanupOrphanedData(currentMarkers);
            }
        }
        public static SettlementCustomData GetPresetSettlementCustomizationData(Settlement settlement)
        {
            if (settlement == null) return null;

            var currentPreset = WorldPresetManager.CurrentlyLoadedPreset;
            if (currentPreset?.factionSettlementCustomizationDefaults != null && settlement.Faction != null)
            {
                if (currentPreset.factionSettlementCustomizationDefaults.TryGetValue(settlement.Faction.def, out var presetDefaultData))
                {
                    return presetDefaultData.Copy();
                }
            }
            return null;
        }
    }
}