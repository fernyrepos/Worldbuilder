using HarmonyLib;
using RimWorld;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Faction), nameof(Faction.GetReportText), MethodType.Getter)]
    public static class Faction_GetReportText_Patch
    {
        public static void Postfix(Faction __instance, ref string __result)
        {
            if (World_ExposeData_Patch.individualFactionDescriptions.TryGetValue(__instance.def, out var individualDescription) && !individualDescription.NullOrEmpty())
            {
                __result = individualDescription + (__instance.def.HasRoyalTitles ? ("\n\n" + RoyalTitleUtility.GetTitleProgressionInfo(__instance)) : "");
            }
            else
            {
                var preset = WorldPresetManager.CurrentlyLoadedPreset;
                if (preset != null && preset.factionDescriptionOverrides != null && preset.factionDescriptionOverrides.TryGetValue(__instance.def, out var presetDescription) && !presetDescription.NullOrEmpty())
                {
                    __result = presetDescription + (__instance.def.HasRoyalTitles ? ("\n\n" + RoyalTitleUtility.GetTitleProgressionInfo(__instance)) : "");
                }
            }
        }
    }
}
