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
        public static HashSet<Thing> explicitlyCustomizedThings = new HashSet<Thing>();
        public static CustomizationData GetCustomizationData(this Thing thing)
        {
            if (thing.Customizable() is false) return null;
            if (thingCustomizationData.TryGetValue(thing, out CustomizationData data))
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


        public static bool Customizable(this Thing thing)
        {
            if (thing is Corpse) return false;
            return thing.def.category == ThingCategory.Item ||
                   thing.def.category == ThingCategory.Building ||
                   thing.def.category == ThingCategory.Plant ||
                   thing.def.category == ThingCategory.Pawn;
        }

        public static void TryAssignPlayerDefault(Pawn worker, Thing t)
        {
            if (worker is null || worker != null && worker.Faction == Faction.OfPlayer)
            {
                if (playerDefaultCustomizationData.TryGetValue(t.def, out var defaultData))
                {
                    thingCustomizationData[t] = defaultData.Copy();
                }
            }
        }
    }
}
