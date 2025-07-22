using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(FactionDef), "Description", MethodType.Getter)]
    public static class FactionDef_Description_Patch
    {
        public static bool Prefix(FactionDef __instance, ref string __result)
        {
            __result = __instance.GetPresetDescription();
            if (ModsConfig.BiotechActive && __instance.humanlikeFaction)
            {
                List<XenotypeChance> list = new List<XenotypeChance>();
                __result = __result + "\n\n" + ("MemberXenotypeChances".Translate() + ":").AsTipTitle() + "\n";
                if (__instance.BaselinerChance > 0f)
                {
                    list.Add(new XenotypeChance(XenotypeDefOf.Baseliner, __instance.BaselinerChance));
                }
                if (__instance.xenotypeSet != null)
                {
                    for (int i = 0; i < __instance.xenotypeSet.Count; i++)
                    {
                        if (__instance.xenotypeSet[i].xenotype != XenotypeDefOf.Baseliner)
                        {
                            list.Add(__instance.xenotypeSet[i]);
                        }
                    }
                }
                if (list.Any())
                {
                    list.SortBy((XenotypeChance x) => 0f - x.chance);
                    __result += list.Select((XenotypeChance x) => $"{x.xenotype.LabelCap}: {Mathf.Min(x.chance, 1f).ToStringPercent()}").ToLineList("  - ");
                }
            }
            return false;
        }

        public static string GetPresetDescription(this FactionDef __instance)
        {
            if (World_ExposeData_Patch.individualFactionDescriptions.TryGetValue(__instance, out var individualDescription) && !individualDescription.NullOrEmpty())
            {
                return individualDescription;
            }
            else
            {
                var preset = WorldPresetManager.CurrentlyLoadedPreset;
                if (preset != null && preset.factionDescriptionOverrides != null && preset.factionDescriptionOverrides.TryGetValue(__instance.defName, out var presetDescription) && !presetDescription.NullOrEmpty())
                {
                    return presetDescription;
                }
            }
            return __instance.description;
        }
    }
}
