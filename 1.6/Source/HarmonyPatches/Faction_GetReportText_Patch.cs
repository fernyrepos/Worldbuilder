using HarmonyLib;
using RimWorld;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Faction), nameof(Faction.GetReportText), MethodType.Getter)]
    public static class Faction_GetReportText_Patch
    {
        public static void Postfix(Faction __instance, ref string __result)
        {
            var description = __instance.def.GetPresetDescription();
            __result = description + (__instance.def.HasRoyalTitles ? ("\n\n" + RoyalTitleUtility.GetTitleProgressionInfo(__instance)) : "");
        }
    }
}
