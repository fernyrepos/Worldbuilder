using HarmonyLib;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(CompressibilityDeciderUtility), "IsSaveCompressible")]
    public static class CompressibilityDeciderUtility_IsSaveCompressible_Patch
    {
        public static void Postfix(ref bool __result, Thing t)
        {
            if (__result && t != null && CustomizationDataCollections.thingCustomizationData.ContainsKey(t))
            {
                __result = false;
            }
        }
    }
}