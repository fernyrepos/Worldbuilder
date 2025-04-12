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
                Command_Action customizeSettlementGizmo = new Command_Action
                {
                    icon = GizmoUtility.CustomizeGizmoIcon
                };

                if (settlement.Faction == Faction.OfPlayer)
                {
                    customizeSettlementGizmo.defaultLabel = "WB_GizmoCustomizeColonyLabel".Translate();
                    customizeSettlementGizmo.defaultDesc = "WB_GizmoCustomizeColonyDesc".Translate();
                    customizeSettlementGizmo.action = () => Find.WindowStack.Add(new Window_ColonyCustomization(settlement));
                }
                else
                {
                    customizeSettlementGizmo.defaultLabel = "WB_GizmoCustomizeLabel".Translate();
                    customizeSettlementGizmo.defaultDesc = "WB_GizmoCustomizeDesc".Translate();
                    customizeSettlementGizmo.action = () => Find.WindowStack.Add(new Window_FactionBaseCustomization(settlement));
                }
                yield return customizeSettlementGizmo;
            }
            else if (__instance.def.defName == "Worldbuilder_MapMarker")
            {
                Command_Action customizeMarkerGizmo = new Command_Action
                {
                    defaultLabel = "WB_GizmoCustomizeLabel".Translate(),
                    defaultDesc = "WB_GizmoCustomizeMarkerDesc".Translate(),
                    icon = GizmoUtility.CustomizeGizmoIcon,
                    action = () => Find.WindowStack.Add(new Window_MarkerCustomization(__instance))
                };
                yield return customizeMarkerGizmo;
                Command_Action eraseMarkerGizmo = new Command_Action
                {
                    defaultLabel = "WB_GizmoEraseMarkerLabel".Translate(),
                    defaultDesc = "WB_GizmoEraseMarkerDesc".Translate(),
                    icon = GizmoUtility.EraseGizmoIcon,
                    action = () =>
                    {
                        MarkerDataManager.RemoveData(__instance);
                        __instance.Destroy();
                        Messages.Message("WB_MarkerErasedMessage".Translate(), MessageTypeDefOf.PositiveEvent);
                    }
                };
                yield return eraseMarkerGizmo;
                if (GizmoUtility.TryCreateNarrativeGizmo(__instance, out var narrativeGizmo))
                {
                    yield return narrativeGizmo;
                }
            }
        }
    }
}