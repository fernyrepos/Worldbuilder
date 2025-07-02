using HarmonyLib;
using Verse;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Worldbuilder
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    [HarmonyPatch(typeof(Thing), "GetGizmos")]
    public static class Thing_GetGizmos_Patch
    {
        public static IEnumerable<Gizmo> Postfix(IEnumerable<Gizmo> __result, Thing __instance)
        {
            var gizmos = __result.ToList();
            if (__instance is not Pawn && WorldbuilderMod.settings.showCustomizeGizmoOnThings && __instance.Customizable())
            {
                var customizeGizmo = MakeCustomizeThingGizmo(__instance);
                if (customizeGizmo != null) gizmos.Add(customizeGizmo);
                if (GizmoUtility.TryCreateNarrativeGizmo(__instance, out var narrativeGizmo)) gizmos.Add(narrativeGizmo);
            }
            return gizmos;
        }

        public static Command_Action MakeCustomizeThingGizmo(Thing thing)
        {
            var things = Find.Selector.SelectedObjects.OfType<Thing>().Where(x => x.def == thing.def).ToList();
            if (!things.Any()) return null;
            var gizmo = new Command_Action
            {
                defaultLabel = "WB_GizmoCustomizeLabel".Translate(),
                defaultDesc = "WB_GizmoCustomizeDesc".Translate(),
                icon = GizmoUtility.CustomizeGizmoIcon,
                action = () => Find.WindowStack.Add(new Window_ThingCustomization(things, thing.GetCustomizationData()))
            };
            return gizmo;
        }
    }
}