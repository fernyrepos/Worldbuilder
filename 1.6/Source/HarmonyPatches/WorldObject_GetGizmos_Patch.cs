using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using System.Linq;

namespace Worldbuilder
{
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(WorldObject), nameof(WorldObject.GetGizmos))]
    public static class WorldObject_GetGizmos_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, WorldObject __instance)
        {
            foreach (Gizmo originalGizmo in __result)
            {
                yield return originalGizmo;
            }
            if (__instance is Settlement settlement)
            {
                if (World_ExposeData_Patch.showCustomization)
                {
                    Command_Action customizeSettlementGizmo = new Command_Action
                    {
                        icon = GizmoUtility.CustomizeGizmoIcon
                    };

                    if (settlement.Faction == Faction.OfPlayer)
                    {
                        customizeSettlementGizmo.defaultLabel = "WB_GizmoCustomizeColonyLabel".Translate();
                        customizeSettlementGizmo.defaultDesc = "WB_GizmoCustomizeColonyDesc".Translate();
                    }
                    else
                    {
                        customizeSettlementGizmo.defaultLabel = "WB_GizmoCustomizeLabel".Translate();
                        customizeSettlementGizmo.defaultDesc = "WB_GizmoCustomizeDesc".Translate();
                    }
                    customizeSettlementGizmo.action = () => Find.WindowStack.Add(new Window_SettlementCustomization(settlement));
                    yield return customizeSettlementGizmo;
                }

                if (GizmoUtility.TryCreateNarrativeGizmo(settlement, out var narrativeGizmo))
                {
                    yield return narrativeGizmo;
                }
            }
        }
    }
}
