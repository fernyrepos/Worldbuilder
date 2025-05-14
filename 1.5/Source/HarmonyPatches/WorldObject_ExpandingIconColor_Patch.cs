using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(WorldObject), "ExpandingIconColor", MethodType.Getter)]
    public static class WorldObject_ExpandingIconColor_Patch
    {
        public static void Postfix(WorldObject __instance, ref Color __result)
        {
            if (__instance is not Settlement settlement) return;

            var customData = SettlementCustomDataManager.GetData(settlement) ?? World_ExposeData_Patch.GetPresetSettlementCustomizationData(settlement);

            if (customData != null && customData.color.HasValue)
            {
                __result = customData.color.Value;
            }
        }
    }
}
