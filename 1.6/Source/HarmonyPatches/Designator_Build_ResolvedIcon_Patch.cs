using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    [HarmonyPatch(typeof(Designator_Build), "ResolvedIcon")]
    public static class Designator_Build_ResolvedIcon_Patch
    {
        public static void Postfix(Designator_Build __instance, ref Texture __result)
        {
            if (__instance.PlacingDef is ThingDef def)
            {
                CustomizationData data = def.GetCustomizationDataPlayer();
                if (data != null)
                {
                    if (data.styleDef != null)
                    {
                        __result = Widgets.GetIconFor(def, __instance.StuffDef, data.styleDef);
                    }
                    else
                    {
                        var texture = data.GetGraphicForDef(def, __instance.StuffDef)?.MatSingle?.mainTexture;
                        if (texture != null)
                        {
                            __result = texture;
                        }
                    }
                }
            }
        }
    }
}
