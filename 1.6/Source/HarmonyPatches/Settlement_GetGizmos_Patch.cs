using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace Worldbuilder
{
    [HarmonyPatch(typeof(Settlement), nameof(Settlement.GetGizmos))]
    public static class Settlement_GetGizmos_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Settlement __instance)
        {
            foreach (var originalGizmo in __result)
            {
                yield return originalGizmo;
            }
            if (Find.WorldSelector.NumSelectedObjects == 1)
            {
                if (World_ExposeData_Patch.showCustomization)
                {
                    var customizeSettlementGizmo = new Command_Action
                    {
                        icon = GizmoUtility.CustomizeGizmoIcon
                    };

                    if (__instance.Faction == Faction.OfPlayer)
                    {
                        customizeSettlementGizmo.defaultLabel = "WB_GizmoCustomizeColonyLabel".Translate();
                        customizeSettlementGizmo.defaultDesc = "WB_GizmoCustomizeColonyDesc".Translate();
                    }
                    else
                    {
                        customizeSettlementGizmo.defaultLabel = "WB_GizmoCustomizeLabel".Translate();
                        customizeSettlementGizmo.defaultDesc = "WB_GizmoCustomizeDesc".Translate(__instance.Name);
                    }
                    customizeSettlementGizmo.action = () => Find.WindowStack.Add(new Window_SettlementCustomization(__instance));
                    yield return customizeSettlementGizmo;
                }

                if (GizmoUtility.TryCreateNarrativeGizmo(__instance, out var narrativeGizmo))
                {
                    yield return narrativeGizmo;
                }
            }
        }
    }
}
