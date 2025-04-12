using HarmonyLib;
using Verse;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using VanillaFurnitureExpanded;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(CompRandomBuildingGraphic), "CompGetGizmosExtra")]
    public static class CompRandomBuildingGraphic_CompGetGizmosExtra_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, CompRandomBuildingGraphic __instance)
        {
            List<Gizmo> gizmos = __result?.ToList() ?? new List<Gizmo>();
            if (__instance.parent.Customizable())
            {
                return Enumerable.Empty<Gizmo>();
            }
            return gizmos;
        }
    }
}