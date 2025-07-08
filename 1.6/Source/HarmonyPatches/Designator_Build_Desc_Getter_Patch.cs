using HarmonyLib;
using RimWorld;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Designator_Build), nameof(Designator_Build.Desc), MethodType.Getter)]
    public static class Designator_Build_Desc_Getter_Patch
    {
        public static void Postfix(Designator_Build __instance, ref string __result)
        {
            if (__instance.PlacingDef is ThingDef def)
            {
                if (CustomizationDataCollections.playerDefaultCustomizationData.TryGetValue(def, out var defaultData))
                {
                    if (!string.IsNullOrEmpty(defaultData.descriptionOverride))
                    {
                        __result = defaultData.descriptionOverride;
                    }
                }
            }
        }
    }
}
