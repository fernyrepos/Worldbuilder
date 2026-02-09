using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(WorldObject), "ExpandingIconColor", MethodType.Getter)]
    public static class WorldObject_ExpandingIconColor_Patch
    {
        public static void Postfix(WorldObject __instance, ref Color __result)
        {
            if (__instance is not Settlement settlement) return;
            var data = settlement.GetCustomizationData();
            if (data != null && data.color.HasValue)
            {
                __result = data.color.Value;
            }
        }
    }
}
