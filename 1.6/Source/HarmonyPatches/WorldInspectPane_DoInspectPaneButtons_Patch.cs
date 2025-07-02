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
    [HarmonyPatch(typeof(WorldInspectPane), nameof(WorldInspectPane.DoInspectPaneButtons))]
    public static class WorldInspectPane_DoInspectPaneButtons_Patch
    {
        public static readonly Texture2D AddMarkerGizmoIcon = ContentFinder<Texture2D>.Get("UI/Buttons/Add", true);
        public static readonly Texture2D EditTextGizmoIcon = ContentFinder<Texture2D>.Get("UI/Buttons/Rename", true);

        public static void Postfix(WorldInspectPane __instance)
        {
            if (Find.WorldSelector.NumSelectedObjects == 0 && Find.WorldSelector.selectedTile >= 0)
            {
                int selectedTile = Find.WorldSelector.selectedTile;
                bool tileIsSuitable = !Find.World.Impassable(selectedTile);

                if (tileIsSuitable)
                {
                    List<Gizmo> tileGizmos = new List<Gizmo>();
                    if (!Find.WorldObjects.AnyWorldObjectAt(selectedTile, DefDatabase<WorldObjectDef>.GetNamed("Worldbuilder_MapMarker")))
                    {
                        Command_Action addMarkerGizmo = new Command_Action
                        {
                            defaultLabel = "WB_GizmoAddMarkerLabel".Translate(),
                            defaultDesc = "WB_GizmoAddMarkerDesc".Translate(),
                            icon = AddMarkerGizmoIcon,
                            action = () => AddMarkerAction(selectedTile)
                        };
                        tileGizmos.Add(addMarkerGizmo);
                    }


                    Command_Action editMapTextGizmo = new Command_Action
                    {
                        defaultLabel = "WB_GizmoEditMapTextLabel".Translate(),
                        defaultDesc = "WB_GizmoEditMapTextDesc".Translate(),
                        icon = EditTextGizmoIcon,
                        action = () => Find.WindowStack.Add(new Window_MapTextEditor(selectedTile))
                    };
                    tileGizmos.Add(editMapTextGizmo);
                    if (tileGizmos.Any())
                    {
                        float startX = 7f;
                        GizmoGridDrawer.DrawGizmoGrid(tileGizmos, startX, out _, null, null, null);
                    }
                }
            }
        }

        private static void AddMarkerAction(int tile)
        {
            WorldObject newMarker = WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("Worldbuilder_MapMarker"));
            newMarker.Tile = tile;
            Find.WorldObjects.Add(newMarker);
            Find.WorldSelector.ClearSelection();
            Find.WorldSelector.Select(newMarker);
            Messages.Message("Marker added. Select it to customize.", MessageTypeDefOf.PositiveEvent);
        }

    }
}
