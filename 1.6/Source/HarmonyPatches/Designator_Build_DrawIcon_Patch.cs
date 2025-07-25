using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.DrawIcon))]
    public static class Designator_Build_DrawIcon_Patch
    {
        public static bool Prefix(Designator_Build __instance, Rect rect, Material buttonMat, GizmoRenderParms parms)
        {
            if (__instance.PlacingDef is ThingDef def)
            {
                CustomizationData data = def.GetCustomizationDataPlayer();
                if (data != null)
                {
                    Color color = parms.lowLight ? Command.LowLightIconColor : __instance.IconDrawColor;
                    CustomizationGraphicUtility.DrawCustomizedGraphicFor(
                        rect,
                        def, __instance.StuffDef,
                        data,
                        __instance.iconAngle,
                        __instance.iconDrawScale,
                        color
                    );
                    return false;
                }
            }
            return true;
        }
    }
}
