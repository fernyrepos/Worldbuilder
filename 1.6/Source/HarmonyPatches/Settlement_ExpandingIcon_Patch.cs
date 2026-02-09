using HarmonyLib;
using RimWorld.Planet;
using UnityEngine;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Settlement), nameof(Settlement.ExpandingIcon), MethodType.Getter)]
    public static class Settlement_ExpandingIcon_Patch
    {
        public static void Postfix(Settlement __instance, ref Texture2D __result)
        {
            var data = __instance.GetCustomizationData();
            if (data != null)
            {
                var customIcon = data.GetIcon();
                if (customIcon != null)
                {
                    __result = customIcon;
                }
            }
        }
    }
}
