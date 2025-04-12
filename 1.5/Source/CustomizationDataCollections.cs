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
            if (thingCustomizationData.TryGetValue(thing, out CustomizationData data))
            {
                return data;
            }
            if (thing.IsPlayerItem() && playerDefaultCustomizationData.TryGetValue(thing.def, out data))
            {
                return data;
            }
            var currentPreset = WorldPresetManager.CurrentlyLoadedPreset;
            if (currentPreset != null)
            {
                if (currentPreset.customizationDefaults != null &&
                    currentPreset.customizationDefaults.TryGetValue(thing.def, out data))
                {
                    return data;
                }
            }
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