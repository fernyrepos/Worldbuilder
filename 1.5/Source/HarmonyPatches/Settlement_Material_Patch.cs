using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using System.Collections.Generic;
using System;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Settlement), nameof(Settlement.Material), MethodType.Getter)]
    public static class Settlement_Material_Patch
    {
        public static void Postfix(Settlement __instance, ref Material __result)
        {
            if (__instance == null) return;

            var customData = SettlementCustomDataManager.GetData(__instance) ?? World_ExposeData_Patch.GetPresetSettlementCustomizationData(__instance);

            if (customData != null)
            {
                Material customMaterial = customData.GetMaterial(__instance);
                if (customMaterial != null)
                {
                    __result = customMaterial;
                }
            }
        }
    }
}
