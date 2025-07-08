using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Settlement), nameof(Settlement.ExposeData))]
    public static class Settlement_ExposeData_Patch
    {
        public static void Postfix(Settlement __instance)
        {
            string scribeLabel = "WB_customData_" + __instance.ID;
            SettlementCustomData data = __instance.GetCustomizationData();
            Scribe_Deep.Look(ref data, scribeLabel);
            if (data != null)
            {
                CustomizationDataCollections.settlementCustomizationData[__instance] = data;
            }
        }
    }
}
