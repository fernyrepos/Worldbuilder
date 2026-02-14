using HarmonyLib;
using RimWorld;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Faction), nameof(Faction.CanChangeGoodwillFor))]
    public static class Faction_CanChangeGoodwillFor_Patch
    {
        public static void Postfix(Faction __instance, Faction other, ref bool __result)
        {
            if (other == null) return;
            
            var data = __instance.GetPopulationData();
            if (data?.permanentEnemy == false && !__result)
            {
                if (__instance.HasGoodwill && other.HasGoodwill && !__instance.defeated && !other.defeated && other != __instance)
                {
                    __result = true;
                }
            }
        }
    }
}
