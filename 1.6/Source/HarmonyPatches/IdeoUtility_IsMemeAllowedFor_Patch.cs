using HarmonyLib;
using RimWorld;
using System.Linq;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(IdeoUtility), "IsMemeAllowedFor")]
    [HotSwappable]
    public static class IdeoUtility_IsMemeAllowedFor_Patch
    {
        public static void Postfix(FactionDef faction, ref bool __result)
        {
            if (__result || Find.FactionManager == null) return;

            var matchingFaction = Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f => f.def == faction);
            if (matchingFaction != null)
            {
                var data = matchingFaction.GetPopulationData();
                if (data != null && data.disableMemeRequirements)
                {
                    __result = true;
                }
            }
        }
    }
}
