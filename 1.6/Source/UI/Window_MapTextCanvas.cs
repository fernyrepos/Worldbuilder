using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Worldbuilder
{
    public struct MapTextEntry
    {
        public WorldFeature feature;
        public Rect rect;
    }

    [HotSwappable]
    public class Window_MapTextCanvas : Window
    {
        private enum DragMode
        {
            None,
            Move,
            Scale,
            Rotate
        }

        private const string TextControlName = "WB_MapTextInlineField";
        private const float HandleHitRadius = 12f;
        private const float MoveThreshold = 6f;

        private static Window_MapEditor suspendedEditor;
        private static bool restoreShowWorldFeatures;

        private WorldFeature selected;
        private DragMode dragMode = DragMode.None;
        private float dragStartDistance;
        private float dragStartSize;
        private float dragStartAngle;
        private float dragPointerStartAngle;
        private float dragAngleSign = 1f;
        private PlanetTile lastMoveTile = PlanetTile.Invalid;
        private Vector2 movePressPoint;
        private bool moveStarted;

        private Dialog_MapTextRename renameDialog;

        private static readonly Color BoxColor = new Color(0.95f, 0.85f, 0.45f);
        private static readonly Color BoxColorHover = new Color(1f, 1f, 1f, 0.5f);
        private static readonly Color HandleFill = new Color(0.98f, 0.92f, 0.6f);

        public override Vector2 InitialSize => new Vector2(560f, 128f);

        public Window_MapTextCanvas()
        {
            forcePause = true;
            doCloseX = false;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
            closeOnClickedOutside = false;
            closeOnAccept = false;
            draggable = true;
            layer = WindowLayer.GameUI;
        }

        public static void Open(Window_MapEditor editor)
        {
            suspendedEditor = editor;
            restoreShowWorldFeatures = !Find.PlaySettings.showWorldFeatures;
            Find.PlaySettings.showWorldFeatures = true;
            editor?.Close(false);
            Find.WindowStack.Add(new Window_MapTextCanvas());
        }

        public override void SetInitialSizeAndPosition()
        {
            windowRect = new Rect((UI.screenWidth - InitialSize.x) / 2f, 8f, InitialSize.x, InitialSize.y).Rounded();
        }

        public override void PostClose()
        {
            base.PostClose();
            if (restoreShowWorldFeatures)
            {
                restoreShowWorldFeatures = false;
                Find.PlaySettings.showWorldFeatures = false;
            }

            var editor = suspendedEditor;
            suspendedEditor = null;
            if (editor != null)
            {
                Find.WindowStack.Add(editor);
            }
        }

        public override void OnCancelKeyPressed()
        {
            if (selected != null)
            {
                selected = null;
                Event.current.Use();
                return;
            }

            base.OnCancelKeyPressed();
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (WorldRendererUtility.WorldRendered is false)
            {
                Close();
                return;
            }

            Text.Font = GameFont.Small;
            var titleRect = new Rect(inRect.x, inRect.y, inRect.width, 24f);
            Widgets.Label(titleRect, "WB_MapTextCanvasTitle".Translate());

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(1f, 1f, 1f, 0.65f);
            Widgets.Label(new Rect(inRect.x, titleRect.yMax, inRect.width, 34f), "WB_MapTextCanvasHint".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            float buttonHeight = 30f;
            float spacing = 6f;
            float buttonWidth = (inRect.width - spacing * 3f) / 4f;
            float y = inRect.yMax - buttonHeight;

            var addRect = new Rect(inRect.x, y, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(addRect, "WB_AddMapTextButton".Translate()))
            {
                AddAtScreenCenter();
            }

            var deleteRect = new Rect(addRect.xMax + spacing, y, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(deleteRect, "WB_MapTextDelete".Translate(), active: selected != null))
            {
                DeleteSelected();
            }

            var listRect = new Rect(deleteRect.xMax + spacing, y, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(listRect, "WB_MapTextList".Translate()))
            {
                Find.WindowStack.Add(new Window_MapTextEditor());
            }

            var doneRect = new Rect(listRect.xMax + spacing, y, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(doneRect, "DoneButton".Translate()))
            {
                Close();
            }
        }

        public override void ExtraOnGUI()
        {
            base.ExtraOnGUI();
            if (WorldRendererUtility.WorldRendered is false)
            {
                return;
            }

            Find.WorldSelector.dragBox.active = false;

            if (renameDialog != null && Find.WindowStack.IsOpen(renameDialog) is false)
            {
                renameDialog = null;
            }

            var entries = CollectEntries();
            if (renameDialog == null)
            {
                HandleInput(entries);
            }

            DrawEntries(entries);
        }

        private List<MapTextEntry> CollectEntries()
        {
            var result = new List<MapTextEntry>();
            var features = Find.World.features.features;
            for (var i = 0; i < features.Count; i++)
            {
                if (MapTextGeometry.TryGetScreenRect(i, features[i], out var rect))
                {
                    result.Add(new MapTextEntry { feature = features[i], rect = rect });
                }
            }

            return result;
        }

        private void HandleInput(List<MapTextEntry> entries)
        {
            var e = Event.current;
            var mouse = UI.MousePositionOnUIInverted;

            if (dragMode != DragMode.None)
            {
                if (!Input.GetMouseButton(0))
                {
                    dragMode = DragMode.None;
                    lastMoveTile = PlanetTile.Invalid;
                    moveStarted = false;
                }
                else
                {
                    ApplyDrag(entries, mouse);
                    if (e.isMouse)
                    {
                        e.Use();
                    }

                    return;
                }
            }

            if (e.rawType == EventType.KeyDown && selected != null &&
                (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace))
            {
                DeleteSelected();
                e.Use();
                return;
            }

            if (e.rawType != EventType.MouseDown || e.button != 0 || Mouse.IsOver(windowRect))
            {
                return;
            }

            if (selected != null && TryGetEntry(entries, selected, out var selectedEntry) &&
                BeginHandleDrag(selectedEntry, mouse))
            {
                e.Use();
                return;
            }

            var hit = HitTest(entries, mouse);
            if (hit == null)
            {
                selected = null;
                return;
            }

            selected = hit;
            SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();

            if (e.clickCount >= 2)
            {
                OpenRenameDialog(TryGetEntry(entries, hit, out var hitEntry) ? hitEntry.rect : (Rect?)null);
            }
            else
            {
                dragMode = DragMode.Move;
                lastMoveTile = PlanetTile.Invalid;
                movePressPoint = mouse;
                moveStarted = false;
            }

            e.Use();
        }

        private bool BeginHandleDrag(MapTextEntry entry, Vector2 mouse)
        {
            if ((mouse - MapTextGeometry.RotateHandleFor(entry.rect)).magnitude <= HandleHitRadius)
            {
                dragMode = DragMode.Rotate;
                dragStartAngle = entry.feature.drawAngle;
                dragPointerStartAngle = PointerAngle(entry.rect.center, mouse);
                float here = MapTextGeometry.ScreenAngleOfRightAxis(entry.feature, dragStartAngle);
                float ahead = MapTextGeometry.ScreenAngleOfRightAxis(entry.feature, dragStartAngle + 10f);
                float delta = Mathf.DeltaAngle(here, ahead);
                dragAngleSign = Mathf.Abs(delta) < 0.01f ? 1f : Mathf.Sign(delta);
                return true;
            }

            for (var i = 0; i < 4; i++)
            {
                if ((mouse - MapTextGeometry.CornerOf(entry.rect, i)).magnitude <= HandleHitRadius)
                {
                    dragMode = DragMode.Scale;
                    dragStartSize = entry.feature.maxDrawSizeInTiles;
                    dragStartDistance = Mathf.Max(1f, (mouse - entry.rect.center).magnitude);
                    return true;
                }
            }

            return false;
        }

        private void ApplyDrag(List<MapTextEntry> entries, Vector2 mouse)
        {
            if (selected == null)
            {
                dragMode = DragMode.None;
                return;
            }

            switch (dragMode)
            {
                case DragMode.Move:
                {
                    if (moveStarted is false)
                    {
                        if ((mouse - movePressPoint).magnitude < MoveThreshold)
                        {
                            break;
                        }

                        moveStarted = true;
                    }

                    var tile = GenWorld.TileAt(UI.MousePositionOnUI);
                    if (tile.Valid && tile != lastMoveTile)
                    {
                        lastMoveTile = tile;
                        MapTextUtility.MoveFeatureToTile(selected, tile);
                    }

                    break;
                }

                case DragMode.Scale:
                {
                    if (!TryGetEntry(entries, selected, out var entry))
                    {
                        break;
                    }

                    float distance = Mathf.Max(1f, (mouse - entry.rect.center).magnitude);
                    float size = dragStartSize * (distance / dragStartDistance);
                    if (Event.current.shift)
                    {
                        size = Mathf.Round(size / 5f) * 5f;
                    }

                    MapTextUtility.SetDrawSize(selected, Mathf.Round(size));
                    break;
                }

                case DragMode.Rotate:
                {
                    if (!TryGetEntry(entries, selected, out var entry))
                    {
                        break;
                    }

                    float pointer = PointerAngle(entry.rect.center, mouse);
                    float angle = dragStartAngle + dragAngleSign * Mathf.DeltaAngle(dragPointerStartAngle, pointer);
                    if (Event.current.shift)
                    {
                        angle = Mathf.Round(angle / 15f) * 15f;
                    }

                    MapTextUtility.SetDrawAngle(selected, angle);
                    break;
                }
            }
        }

        private void DrawEntries(List<MapTextEntry> entries)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            var hovered = renameDialog != null ? null : HitTest(entries, UI.MousePositionOnUIInverted);

            foreach (var entry in entries)
            {
                bool isSelected = entry.feature == selected;
                if (!isSelected && entry.feature != hovered)
                {
                    continue;
                }

                GUI.color = isSelected ? BoxColor : BoxColorHover;
                Widgets.DrawBox(entry.rect, isSelected ? 2 : 1);
                GUI.color = Color.white;

                if (!isSelected)
                {
                    continue;
                }

                for (var i = 0; i < 4; i++)
                {
                    DrawHandle(MapTextGeometry.CornerOf(entry.rect, i));
                }

                var rotateHandle = MapTextGeometry.RotateHandleFor(entry.rect);
                Widgets.DrawLine(new Vector2(entry.rect.center.x, entry.rect.yMin), rotateHandle, BoxColor, 1f);
                DrawHandle(rotateHandle);
            }
        }

        private static void DrawHandle(Vector2 position)
        {
            float half = MapTextGeometry.HandleSize / 2f;
            var rect = new Rect(position.x - half, position.y - half, MapTextGeometry.HandleSize, MapTextGeometry.HandleSize);
            Widgets.DrawBoxSolid(rect, HandleFill);
            Widgets.DrawBox(rect);
        }

        private void OpenRenameDialog(Rect? anchor)
        {
            if (selected == null)
            {
                return;
            }

            dragMode = DragMode.None;
            lastMoveTile = PlanetTile.Invalid;
            moveStarted = false;
            renameDialog = new Dialog_MapTextRename(selected, anchor);
            Find.WindowStack.Add(renameDialog);
        }

        private void AddAtScreenCenter()
        {
            var tile = GenWorld.TileAt(new Vector2(UI.screenWidth / 2f, UI.screenHeight / 2f));
            if (!tile.Valid)
            {
                Messages.Message("WB_MapTextInvalidPosition".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            var feature = MapTextUtility.AddFeature("WB_NewMapTextLabel".Translate(), tile);
            if (feature == null)
            {
                return;
            }

            MapTextUtility.FocusCameraOn(feature);
            selected = feature;
            OpenRenameDialog(new Rect(UI.screenWidth / 2f - 60f, UI.screenHeight / 2f - 44f, 120f, 88f));
        }

        private void DeleteSelected()
        {
            if (selected == null)
            {
                return;
            }

            if (renameDialog != null)
            {
                renameDialog.Close(false);
                renameDialog = null;
            }

            MapTextUtility.RemoveFeature(selected);
            selected = null;
            dragMode = DragMode.None;
        }

        private static WorldFeature HitTest(List<MapTextEntry> entries, Vector2 mouse)
        {
            WorldFeature best = null;
            float bestArea = float.MaxValue;
            foreach (var entry in entries)
            {
                if (!entry.rect.Contains(mouse))
                {
                    continue;
                }

                float area = entry.rect.width * entry.rect.height;
                if (area < bestArea)
                {
                    bestArea = area;
                    best = entry.feature;
                }
            }

            return best;
        }

        private static bool TryGetEntry(List<MapTextEntry> entries, WorldFeature feature, out MapTextEntry entry)
        {
            foreach (var candidate in entries)
            {
                if (candidate.feature == feature)
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = default;
            return false;
        }

        private static float PointerAngle(Vector2 center, Vector2 mouse)
        {
            var direction = mouse - center;
            return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }
    }
}
