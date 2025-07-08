using HarmonyLib;
using RimWorld;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.ThingStyleDefForPreview), MethodType.Getter)]
    public static class Designator_Build_ThingStyleDefForPreview_Getter_Patch
    {
        public static void Postfix(Designator_Build __instance, ref ThingStyleDef __result)
        {
            if (__result == null && __instance.PlacingDef is ThingDef def)
            {
                CustomizationData data = def.GetCustomizationDataPlayer();
                if (data != null)
                {
                    if (data.styleDef != null && string.IsNullOrEmpty(data.selectedImagePath) && !data.variationIndex.HasValue)
                    {
                        __result = data.styleDef;
                    }
                }
            }
        }
    }
}
