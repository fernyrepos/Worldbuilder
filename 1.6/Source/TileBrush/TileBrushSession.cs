using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Worldbuilder
{
    internal sealed class TileBrushSession
    {
        private bool editorClosedThisGuiEvent;
        private Window_TileBrushEditor editorWindow;

        internal TileBrushSession(Map map)
        {
            Map = map;
            Settings = new TileBrushSettings();
            Controller = new TileBrushController(map, Settings);
        }

        internal Map Map { get; }
        internal TileBrushSettings Settings { get; }
        internal TileBrushController Controller { get; }
        internal bool BrushActive { get; private set; }
        internal bool EditorOpen => editorWindow?.IsOpen == true;

        internal Window_TileBrushEditor EditorWindow =>
            editorWindow ??= new Window_TileBrushEditor(this);

        internal void Activate()
        {
            TileBrushSessionManager.Activate(this);
        }

        internal void ActivateInternal()
        {
            BrushActive = true;
            Find.DesignatorManager?.Deselect();
        }

        internal void DeactivateInternal()
        {
            Controller.CancelStroke();
            BrushActive = false;
        }

        internal void NotifyEditorClosed()
        {
            editorClosedThisGuiEvent =
                BrushActive &&
                CanEditCurrentMap();
        }

        internal void CloseEditor(bool doCloseSound)
        {
            if (EditorOpen)
            {
                editorWindow.Close(doCloseSound);
            }
        }

        internal bool CanEditCurrentMap()
        {
            return Current.ProgramState == ProgramState.Playing &&
                   Find.CurrentMap == Map &&
                   Find.Maps.Contains(Map) &&
                   WorldRendererUtility.DrawingMap &&
                   MapGenerator.mapBeingGenerated == null;
        }

        internal void HandleMapOnGUI()
        {
            if (!BrushActive || !CanEditCurrentMap())
            {
                return;
            }

            var currentEvent = Event.current;
            var editorJustClosed = editorClosedThisGuiEvent;
            editorClosedThisGuiEvent = false;
            if (Controller.StrokeActive &&
                currentEvent.rawType == EventType.MouseUp &&
                currentEvent.button == 0)
            {
                Controller.EndStroke();
                if (currentEvent.type != EventType.Used &&
                    Find.WindowStack.GetWindowAt(
                        UI.MousePositionOnUIInverted) == null)
                {
                    currentEvent.Use();
                }

                return;
            }

            if (!EditorOpen &&
                !editorJustClosed &&
                !Find.WindowStack.NonImmediateDialogWindowOpen &&
                KeyBindingDefOf.Cancel.KeyDownEvent)
            {
                DeactivateFromInput(currentEvent);
                return;
            }

            var mouseCell = UI.MouseCell();
            var pointerOverUi = PointerOverMapUi();
            var inputBlocked =
                pointerOverUi ||
                Find.WindowStack.NonImmediateDialogWindowOpen ||
                !mouseCell.InBounds(Map);

            if (!pointerOverUi &&
                !Find.WindowStack.NonImmediateDialogWindowOpen &&
                currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 1)
            {
                DeactivateFromInput(currentEvent);
                return;
            }

            if (inputBlocked)
            {
                Controller.SuspendPath();
                return;
            }

            if (Settings.EyedropperActive &&
                currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 0)
            {
                TileBrushToolRegistry.TryPickAtMouse(
                    Settings,
                    Map,
                    mouseCell);
                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 0)
            {
                Find.DesignatorManager?.Deselect();
                Controller.BeginStroke(mouseCell);
                currentEvent.Use();
            }
            else if (Controller.StrokeActive &&
                     currentEvent.type == EventType.MouseDrag &&
                     currentEvent.button == 0)
            {
                Controller.ContinueStroke(mouseCell);
                currentEvent.Use();
            }
        }

        internal void DrawMapPreview()
        {
            if (!BrushActive ||
                !CanEditCurrentMap() ||
                Find.WindowStack.NonImmediateDialogWindowOpen ||
                PointerOverMapUi())
            {
                return;
            }

            var mouseCell = UI.MouseCell();
            if (mouseCell.InBounds(Map))
            {
                Controller.DrawPreview(
                    mouseCell,
                    Settings.EyedropperActive);
            }
        }

        private void DeactivateFromInput(Event currentEvent)
        {
            TileBrushSessionManager.Deactivate(this);
            SoundDefOf.CancelMode.PlayOneShotOnCamera();
            Messages.Message(
                "WB_TileBrushBrushDeactivated".Translate(),
                MessageTypeDefOf.NeutralEvent,
                historical: false);
            currentEvent.Use();
        }

        private static bool PointerOverMapUi()
        {
            var mouse = UI.MousePositionOnUIInverted;
            if (Find.WindowStack.GetWindowAt(mouse) != null ||
                Find.ColonistBar?.AnyColonistOrCorpseAt(mouse) == true)
            {
                return true;
            }

            if (mouse.y >= UI.screenHeight - 55f ||
                mouse.x >= UI.screenWidth - 55f)
            {
                return true;
            }

            if (mouse.x <= 140f &&
                mouse.y <= UI.screenHeight - 180f)
            {
                return true;
            }

            return Find.MainTabsRoot.OpenTab == null &&
                   mouse.x <= 360f &&
                   mouse.y >= UI.screenHeight - 230f;
        }
    }

    internal static class TileBrushSessionManager
    {
        private static readonly ConditionalWeakTable<Map, TileBrushSession>
            Sessions = new ConditionalWeakTable<Map, TileBrushSession>();

        private static TileBrushSession activeSession;

        internal static void OpenEditor(Map map)
        {
            if (map == null)
            {
                return;
            }

            var session = Sessions.GetValue(
                map,
                key => new TileBrushSession(key));
            Activate(session);

            var editor = session.EditorWindow;
            if (editor.IsOpen)
            {
                Find.WindowStack.Notify_ManuallySetFocus(editor);
                return;
            }

            Find.WindowStack.Add(editor);
        }

        internal static void Activate(TileBrushSession session)
        {
            if (activeSession != session)
            {
                activeSession?.DeactivateInternal();
                activeSession = session;
            }

            session.ActivateInternal();
        }

        internal static void Deactivate(TileBrushSession session)
        {
            session.DeactivateInternal();
            if (activeSession == session)
            {
                activeSession = null;
            }
        }

        internal static void Release(Map map)
        {
            if (map == null || !Sessions.TryGetValue(map, out var session))
            {
                return;
            }

            session.DeactivateInternal();
            session.CloseEditor(doCloseSound: false);
            session.Controller.ClearHistory();

            if (activeSession == session)
            {
                activeSession = null;
            }

            Sessions.Remove(map);
        }

        internal static void HandleMapOnGUI()
        {
            var session = activeSession;
            if (session == null)
            {
                return;
            }

            if (!session.CanEditCurrentMap())
            {
                Deactivate(session);
                session.CloseEditor(doCloseSound: false);
                return;
            }

            if (Find.DesignatorManager?.SelectedDesignator != null)
            {
                Deactivate(session);
                return;
            }

            session.HandleMapOnGUI();
        }

        internal static void DrawMapPreview()
        {
            activeSession?.DrawMapPreview();
        }
    }

    [HarmonyPatch(
        typeof(MapInterface),
        nameof(MapInterface.MapInterfaceOnGUI_AfterMainTabs))]
    internal static class MapInterface_TileBrushSessionPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            TileBrushSessionManager.HandleMapOnGUI();
        }
    }

    [HarmonyPatch(
        typeof(MapInterface),
        nameof(MapInterface.MapInterfaceUpdate))]
    internal static class MapInterface_TileBrushPreviewPatch
    {
        [HarmonyPostfix]
        private static void Postfix()
        {
            TileBrushSessionManager.DrawMapPreview();
        }
    }

    [HarmonyPatch(typeof(Map), nameof(Map.Dispose))]
    internal static class Map_TileBrushSessionDisposalPatch
    {
        [HarmonyPrefix]
        private static void Prefix(Map __instance)
        {
            TileBrushSessionManager.Release(__instance);
        }
    }
}
