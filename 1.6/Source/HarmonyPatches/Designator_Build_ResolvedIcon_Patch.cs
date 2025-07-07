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
                if (CustomizationDataCollections.playerDefaultCustomizationData.TryGetValue(def, out var defaultData))
                {
                    if (defaultData.styleDef != null)
                    {
                        __result = Widgets.GetIconFor(def, null, defaultData.styleDef);
                    }
                    else
                    {
                        var texture = defaultData.GetGraphicForDef(def)?.MatSingle?.mainTexture;
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
