using HarmonyLib;
using Verse;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using System;
using RimWorld.Planet;

namespace Worldbuilder
{
    [HotSwappable]
    public static class CustomizationDataCollections
    {
        public static Dictionary<Thing, CustomizationData> thingCustomizationData = new Dictionary<Thing, CustomizationData>();
        public static Dictionary<ThingDef, CustomizationData> playerDefaultCustomizationData = new Dictionary<ThingDef, CustomizationData>();
        public static Dictionary<Settlement, SettlementCustomData> settlementCustomizationData = new Dictionary<Settlement, SettlementCustomData>();
        public static HashSet<Thing> explicitlyCustomizedThings = new HashSet<Thing>();
        public static CustomizationData GetCustomizationData(this Thing thing)
        {
            if (thing.Customizable() is false)
            {
                thing.LogMessage("Not customizable");
                return null;
            }
            if (thingCustomizationData.TryGetValue(thing, out CustomizationData data))
            {
                thing.LogMessage("Found customization data");
                return data;
            }
            var currentPreset = WorldPresetManager.CurrentlyLoadedPreset;
            thing.LogMessage("Found world preset: " + currentPreset?.name);
            if (currentPreset != null)
            {
                if (currentPreset.customizationDefaults != null &&
                    currentPreset.customizationDefaults.TryGetValue(thing.def.defName, out data))
                {
                    thing.LogMessage("Found customization data in preset");
                    return data;
                }
            }
            thing.LogMessage("No customization data found");
            return null;
        }

        public static CustomizationData GetCustomizationDataPlayer(this ThingDef def)
        {
            if (def.Customizable() is false)
            {
                return null;
            }
            if (playerDefaultCustomizationData.TryGetValue(def, out var data))
            {
                def.LogMessage("Found playerDefaultCustomizationData customization data for player");
                return data;
            }
            var preset = WorldPresetManager.CurrentlyLoadedPreset;
            if (preset?.customizationDefaults != null &&
                preset.customizationDefaults.TryGetValue(def.defName, out var presetDefaultData))
            {
                def.LogMessage("Found customization data in preset");
                return presetDefaultData;
            }
            def.LogMessage("No customization data found: current preset: " + preset?.name);
            return null;
        }

        public static bool Customizable(this Thing thing)
        {
            if (thing is Corpse) return false;
            return thing.def.Customizable();
        }
        public static bool Customizable(this ThingDef def)
        {
            return def.category == ThingCategory.Item ||
                   def.category == ThingCategory.Building ||
                   def.category == ThingCategory.Plant ||
                   def.category == ThingCategory.Pawn;
        }
        public static void TryAssignPlayerDefault(Pawn worker, Thing t)
        {
            if (worker is null || worker != null && worker.Faction == Faction.OfPlayer)
            {
                if (playerDefaultCustomizationData.TryGetValue(t.def, out var data))
                {
                    thingCustomizationData[t] = data.Copy();
                }
            }
        }


        public static SettlementCustomData GetCustomizationData(this Settlement settlement)
        {
            if (settlement == null) return null;
            settlementCustomizationData ??= new Dictionary<Settlement, SettlementCustomData>();
            if (settlementCustomizationData.TryGetValue(settlement, out var data))
            {
                return data;
            }
            var currentPreset = WorldPresetManager.CurrentlyLoadedPreset;
            if (currentPreset?.factionSettlementCustomizationDefaults != null && settlement.Faction != null)
            {
                if (currentPreset.factionSettlementCustomizationDefaults.TryGetValue(settlement.Faction.def.defName, out var presetDefaultData))
                {
                    return presetDefaultData;
                }
            }

            return null;
        }
    }
}
