using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;

namespace Worldbuilder
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    public static class Pawn_GetGizmos_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Pawn __instance)
        {
            foreach (var g in __result)
            {
                yield return g;
            }
            if (WorldbuilderMod.settings.showCustomizeGizmoOnPawns && World_ExposeData_Patch.showCustomization && __instance.Customizable())
            {
                var customizeGizmo = MakeCustomizePawnGizmo(__instance, __result);
                if (customizeGizmo != null) yield return customizeGizmo;
            }
            if (GizmoUtility.TryCreateNarrativeGizmo(__instance, out var narrativeGizmo)) yield return narrativeGizmo;
        }

        public static Command_Action MakeCustomizePawnGizmo(Pawn pawn, IEnumerable<Gizmo> gizmos)
        {
            var gizmo = new Command_Action
            {
                defaultLabel = "WB_GizmoCustomizeLabel".Translate(pawn.LabelShortCap),
                defaultDesc = "WB_GizmoCustomizeDesc".Translate(pawn.LabelShortCap),
                icon = GizmoUtility.CustomizeGizmoIcon,
                action = () => Find.WindowStack.Add(new Window_PawnCustomization(pawn))
            };
            var order = float.MaxValue;
            foreach (var g in gizmos)
            {
                if (pawn.IsColonist && g is Command_Toggle t && t.defaultDesc == "CommandToggleDraftDesc".Translate())
                {
                    if (g.Order < order) order = g.Order;
                }
                else if (pawn.RaceProps.Animal && g is Designator_Slaughter or Designator_Hunt)
                {
                    if (g.Order < order) order = g.Order;
                }
            }
            if (order != float.MaxValue) gizmo.Order = order - 0.1f;
            return gizmo;
        }
    }
}
