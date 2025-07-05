using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Worldbuilder;

namespace Worldbuilder
{
    [HotSwappable]
    [HarmonyPatch(typeof(WITab_Planet), nameof(WITab_Planet.FillTab))]
    public static class WITab_Planet_FillTab_Patch
    {
        public static void Postfix(WITab_Planet __instance)
        {
            Rect rect = new Rect(5f, __instance.size.y - 40f, 150, 32);
            if (Widgets.ButtonText(rect, "WB_GizmoEditMapTextLabel".Translate()))
            {
                Find.WindowStack.Add(new Window_MapTextEditor());
            }
        }
    }
}
