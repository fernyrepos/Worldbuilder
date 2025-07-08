using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using System;
using System.IO;
using System.Collections.Generic;
using VEF.Buildings;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.IconDrawColor), MethodType.Getter)]
    public static class Designator_Build_IconDrawColor_Getter_Patch
    {
        public static void Postfix(Designator_Build __instance, ref Color __result)
        {
            if (__instance.PlacingDef is ThingDef def)
            {
                CustomizationData data = def.GetCustomizationDataPlayer();
                if (data?.color != null)
                {
                    __result = data.color.Value;
                }
            }
        }
    }
}
