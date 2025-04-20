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
            string scribeLabel = "worldbuilder_customData_" + __instance.ID;
            SettlementCustomData data = SettlementCustomDataManager.GetData(__instance);
            Scribe_Deep.Look(ref data, scribeLabel);
            if (data != null)
            {
                SettlementCustomDataManager.LoadData(__instance, data);
            }
        }
    }
}
