using HarmonyLib;
using RimWorld;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(InspectPaneUtility), "PluralLabelFor")]
    public static class InspectPaneUtility_PluralLabelFor_Patch
    {
        public static void Postfix(Thing thing, ref string __result)
        {
            if (thing.GetCustomizationData() is CustomizationData customizationData && customizationData != null && !customizationData.labelOverride.NullOrEmpty())
            {
                __result = customizationData.labelOverride;
            }
        }
    }
}
