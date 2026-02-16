using HarmonyLib;
using RimWorld;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Faction), nameof(Faction.LeaderTitle), MethodType.Getter)]
    [HotSwappable]
    public static class Faction_LeaderTitle_Patch
    {
        public static void Postfix(Faction __instance, ref string __result)
        {
            var data = __instance.GetPopulationData();
            if (data != null && !string.IsNullOrEmpty(data.leaderTitle))
            {
                __result = data.leaderTitle;
            }
        }
    }
}
