using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using System.Linq;
using System.Reflection.Emit;

namespace Worldbuilder
{
    [HotSwappable]
    [HarmonyPatch(typeof(WorldGizmoUtility), nameof(WorldGizmoUtility.WorldUIOnGUI))]
    public static class WorldGizmoUtility_WorldUIOnGUI_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var drawGizmoGridForMethod = AccessTools.Method(typeof(GizmoGridDrawer), nameof(GizmoGridDrawer.DrawGizmoGridFor));

            foreach (var instruction in instructions)
            {
                if (instruction.Calls(drawGizmoGridForMethod))
                {
                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(WorldGizmoUtility), "tmpObjectsList"));
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(WorldGizmoUtility_WorldUIOnGUI_Patch), nameof(AddWorldbuilderGizmos)));
                }
                yield return instruction;
            }
        }

        private static void AddWorldbuilderGizmos(List<object> tmpObjectsList)
        {
            if (Find.WorldSelector.SelectedTile == PlanetTile.Invalid) return;

            if (!Find.WorldObjects.AnyWorldObjectAt(Find.WorldSelector.selectedTile, DefsOf.WB_MapMarker) && Find.WorldSelector.SelectedObjects.OfType<WorldObject_MapMarker>().Any() is false)
            {
                var addMarkerGizmo = new Command_Action
                {
                    defaultLabel = "WB_GizmoAddMarkerLabel".Translate(),
                    defaultDesc = "WB_GizmoAddMarkerDesc".Translate(),
                    icon = GizmoUtility.AddMarkerGizmoIcon,
                    action = () => AddMarkerAction(Find.WorldSelector.selectedTile)
                };
                tmpObjectsList.Add(addMarkerGizmo);
            }
        }

        private static void AddMarkerAction(PlanetTile tile)
        {
            var newMarker = WorldObjectMaker.MakeWorldObject(DefsOf.WB_MapMarker) as WorldObject_MapMarker;
            newMarker.Tile = tile;
            Find.WorldObjects.Add(newMarker);
            Find.WorldSelector.ClearSelection();
            Find.WorldSelector.Select(newMarker);
            newMarker.MarkerData.color = Color.red;
            Find.WindowStack.Add(new Window_MapMarkerCustomization(newMarker));
        }
    }
}
