using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.GetDescription))]
    public static class WorldObject_GetDescription_Patch
    {
        public static void Postfix(WorldObject __instance, ref string __result)
        {
            if (__instance is Settlement settlement)
            {
                var customData = SettlementCustomDataManager.GetData(settlement);
                if (customData != null && !customData.description.NullOrEmpty())
                {
                    __result = customData.description;
                }
            }
        }
    }
}
