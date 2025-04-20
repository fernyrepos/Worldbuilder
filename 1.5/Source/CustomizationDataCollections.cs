using HarmonyLib;
using Verse;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RimWorld;
using System;

namespace Worldbuilder
{
    [HarmonyPatch]
    public static class CustomizationDataCollections
    {
        public static Dictionary<Thing, CustomizationData> thingCustomizationData = new Dictionary<Thing, CustomizationData>();
        public static Dictionary<ThingDef, CustomizationData> playerDefaultCustomizationData = new Dictionary<ThingDef, CustomizationData>();
        public static Dictionary<Thing, bool> craftedItems = new Dictionary<Thing, bool>();
        public static HashSet<Thing> explicitlyCustomizedThings = new HashSet<Thing>();
        public static CustomizationData GetCustomizationData(this Thing thing)
        {
            if (thing.Customizable() is false) return null;
            bool shouldLog = false; //thing.def == ThingDefOf.StandingLamp;
            if (shouldLog) Log.Message($"GetCustomizationData called for StandingLamp: {thing.ThingID}");

            if (thingCustomizationData.TryGetValue(thing, out CustomizationData data))
            {
                if (shouldLog) Log.Message($"Found in thingCustomizationData: {data != null}");
                return data;
            }
            if (shouldLog) Log.Message($"Not found in thingCustomizationData.");
            playerDefaultCustomizationData ??= new Dictionary<ThingDef, CustomizationData>();
            if (thing.IsPlayerItem() && playerDefaultCustomizationData.TryGetValue(thing.def, out data))
            {
                if (shouldLog) Log.Message($"IsPlayerItem: {thing.IsPlayerItem()}, Found in playerDefaultCustomizationData: {data != null}");
                return data;
            }
            if (shouldLog) Log.Message($"Not found in playerDefaultCustomizationData or not IsPlayerItem.");

            var currentPreset = WorldPresetManager.CurrentlyLoadedPreset;
            if (shouldLog) Log.Message($"CurrentPreset: {currentPreset?.name ?? "null"}");
            if (currentPreset != null)
            {
                if (currentPreset.customizationDefaults != null &&
                    currentPreset.customizationDefaults.TryGetValue(thing.def, out data))
                {
                    if (shouldLog) Log.Message($"Found in currentPreset.customizationDefaults: {data != null}");
                    return data;
                }
                if (shouldLog) Log.Message($"Not found in currentPreset.customizationDefaults.");
            }
            if (shouldLog) Log.Message($"Returning null.");
            return null;
        }

        public static bool IsPlayerItem(this Thing thing)
        {
            return craftedItems.TryGetValue(thing, out bool value) && value;
        }

        public static bool Customizable(this Thing thing)
        {
            if (thing is Corpse) return false;
            return thing.def.category == ThingCategory.Item ||
                   thing.def.category == ThingCategory.Building ||
                   thing.def.category == ThingCategory.Plant ||
                   thing.def.category == ThingCategory.Pawn;
        }

        public static void MarkBuildingIfPlayerConstructed(Pawn worker, Thing building)
        {
            if (worker != null && worker.Faction == Faction.OfPlayer && building != null)
            {
                craftedItems[building] = true;
            }
        }
    }
}
