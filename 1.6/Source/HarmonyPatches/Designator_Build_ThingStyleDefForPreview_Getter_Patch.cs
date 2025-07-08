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
                if (CustomizationDataCollections.playerDefaultCustomizationData.TryGetValue(def, out var defaultData))
                {
                    if (defaultData?.styleDef != null && string.IsNullOrEmpty(defaultData.selectedImagePath) && !defaultData.variationIndex.HasValue)
                    {
                        __result = defaultData.styleDef;
                    }
                }
            }
        }
    }
}
