using HarmonyLib;
using RimWorld;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Faction), nameof(Faction.GetReportText), MethodType.Getter)]
    public static class Faction_GetReportText_Patch
    {
        public static void Postfix(Faction __instance, ref string __result)
        {
            string description;
            if (World_ExposeData_Patch.factionDescriptionsById != null && World_ExposeData_Patch.factionDescriptionsById.TryGetValue(__instance.loadID, out var individualDescription) && !string.IsNullOrEmpty(individualDescription))
            {
                description = individualDescription;
            }
            else
            {
                description = __instance.def.GetPresetDescription();
            }
            __result = description + (__instance.def.HasRoyalTitles ? ("\n\n" + RoyalTitleUtility.GetTitleProgressionInfo(__instance)) : "");
        }
    }
}
