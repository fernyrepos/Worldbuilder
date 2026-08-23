using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Worldbuilder
{
    [HotSwappable]
    public class Window_StoryLibrary : Window
    {
        private Vector2 scrollPosition = Vector2.zero;

        private int pressIndex = -1;
        private int pressKind;
        private Rect pressRect;
        private Vector2 dragMouseStart;
        private Vector2 dragGrabOffset;

        private int draggingIndex = -1;
        private int dropIndex = -1;
        private int dropCol;
        private int dropRow;

        private const int PressCard = 0;
        private const int PressRead = 1;
        private const int PressEdit = 2;

        private const float CardWidth = 180f;
        private const float CoverHeight = 122f;
        private const float TitleStripHeight = 46f;
        private const float ActionStripHeight = 32f;
        private const float CardHeight = CoverHeight + TitleStripHeight + ActionStripHeight;
        private const float HorizontalSpacing = 18f;
        private const float VerticalSpacing = 16f;
        private const int NumColumns = 4;
        private const float RowHeight = CardHeight + VerticalSpacing;
        private const float ColumnPitch = CardWidth + HorizontalSpacing;

        private const float DragThreshold = 6f;
        private const float AutoScrollEdge = 44f;
        private const float AutoScrollSpeed = 14f;

        private static readonly Color ShelfLine = new Color(0.36f, 0.31f, 0.25f);
        private static readonly Color TitleStrip = new Color(0.18f, 0.17f, 0.16f);
        private static readonly Color DropSlot = new Color(0.85f, 0.76f, 0.55f);
        private static readonly Color DragHole = new Color(0f, 0f, 0f, 0.3f);

        public override Vector2 InitialSize => new Vector2(850f, 640f);

        public Window_StoryLibrary()
        {
            this.forcePause = true;
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
            this.draggable = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            var stories = World_ExposeData_Patch.worldStories;

            Text.Font = GameFont.Medium;
            float titleHeight = Text.LineHeight;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, titleHeight), "WB_StoryLibraryTitle".Translate());
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerRight;
            GUI.color = new Color(1f, 1f, 1f, 0.55f);
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width - 34f, titleHeight),
                "WB_StoryLibraryCount".Translate(stories.Count));
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            float ruleY = inRect.y + titleHeight + 4f;
            Widgets.DrawBoxSolid(new Rect(inRect.x, ruleY, inRect.width, 1f), ShelfLine);

            float buttonHeight = 40f;
            var shelfRect = new Rect(inRect.x, ruleY + 12f, inRect.width,
                inRect.height - (ruleY - inRect.y) - 12f - buttonHeight - 12f);

            if (stories.Count == 0)
            {
                ClearPress();
                DrawEmptyState(shelfRect);
            }
            else
            {
                DrawShelf(shelfRect, stories);
            }

            DrawBottomButtons(inRect, buttonHeight);
        }

        private void DrawEmptyState(Rect rect)
        {
            Widgets.DrawMenuSection(rect);

            var iconRect = new Rect(rect.center.x - 32f, rect.center.y - 52f, 64f, 64f);
            GUI.color = new Color(1f, 1f, 1f, 0.25f);
            Widgets.DrawTextureFitted(iconRect, GizmoUtility.NarrativeGizmoIcon, 1f);
            GUI.color = Color.white;

            Text.Anchor = TextAnchor.UpperCenter;
            GUI.color = new Color(1f, 1f, 1f, 0.6f);
            Widgets.Label(new Rect(rect.x + 20f, iconRect.yMax + 10f, rect.width - 40f, 60f), "WB_StoryLibraryEmpty".Translate());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void DrawShelf(Rect rect, List<Story> stories)
        {
            int rows = Mathf.CeilToInt((float)stories.Count / NumColumns);
            float gridWidth = (CardWidth * NumColumns) + (HorizontalSpacing * (NumColumns - 1));

            var viewRect = new Rect(0f, 0f, Mathf.Max(rect.width - 16f, gridWidth), rows * RowHeight);
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            float startX = (viewRect.width > gridWidth) ? (viewRect.width - gridWidth) / 2f : 0f;

            for (int row = 0; row < rows; row++)
            {
                Widgets.DrawBoxSolid(new Rect(startX, row * RowHeight + CardHeight + 5f, gridWidth, 2f), ShelfLine);
            }

            if (draggingIndex >= stories.Count || pressIndex >= stories.Count)
            {
                ClearPress();
            }

            Event e = Event.current;
            Vector2 mouse = e.mousePosition;
            bool dragging = draggingIndex >= 0;

            if (dragging)
            {
                ComputeDropSlot(mouse, startX, rows, stories.Count);
            }

            for (int i = 0; i < stories.Count; i++)
            {
                var cardRect = CardRectAt(i, startX);
                if (i == draggingIndex)
                {
                    Widgets.DrawBoxSolid(cardRect, DragHole);
                    continue;
                }
                DrawStoryCard(cardRect, stories[i], i, dragging);
            }

            if (dragging)
            {
                float slotX = startX + dropCol * ColumnPitch - HorizontalSpacing * 0.5f - 1.5f;
                Widgets.DrawBoxSolid(new Rect(slotX, dropRow * RowHeight, 3f, CardHeight), DropSlot);

                var floatRect = new Rect(mouse - dragGrabOffset, new Vector2(CardWidth, CardHeight));
                Widgets.DrawBoxSolid(floatRect.ExpandedBy(3f), new Color(0f, 0f, 0f, 0.45f));
                DrawStoryCard(floatRect, stories[draggingIndex], -1, true);

                if (e.type == EventType.Repaint)
                {
                    AutoScroll(rect, viewRect, mouse);
                }
            }
            else if (pressIndex >= 0 && pressKind == PressCard && Input.GetMouseButton(0)
                && (e.rawType == EventType.MouseDrag || e.type == EventType.Repaint)
                && (mouse - dragMouseStart).sqrMagnitude > DragThreshold * DragThreshold)
            {
                draggingIndex = pressIndex;
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
            }

            if (e.rawType == EventType.MouseUp && e.button == 0)
            {
                if (dragging)
                {
                    ApplyDrop(stories);
                }
                else
                {
                    ResolveClick(stories, mouse);
                }
                ClearPress();
            }
            else if (e.type == EventType.Repaint && !Input.GetMouseButton(0) && (dragging || pressIndex >= 0))
            {
                if (dragging)
                {
                    ApplyDrop(stories);
                }
                ClearPress();
            }

            Widgets.EndScrollView();
        }

        private void ResolveClick(List<Story> stories, Vector2 mouse)
        {
            if (pressIndex < 0 || pressIndex >= stories.Count || !pressRect.Contains(mouse)) return;

            var story = stories[pressIndex];
            if (pressKind == PressEdit)
            {
                Find.WindowStack.Add(new Window_StoryEditor(story));
            }
            else
            {
                OpenStory(story);
            }
        }

        private static Rect CardRectAt(int index, float startX)
        {
            return new Rect(
                startX + (index % NumColumns) * ColumnPitch,
                (index / NumColumns) * RowHeight,
                CardWidth,
                CardHeight);
        }

        private void ComputeDropSlot(Vector2 mouse, float startX, int rows, int count)
        {
            int col = Mathf.Clamp(Mathf.RoundToInt((mouse.x - startX + HorizontalSpacing * 0.5f) / ColumnPitch), 0, NumColumns);
            int row = Mathf.Clamp(Mathf.FloorToInt(mouse.y / RowHeight), 0, rows - 1);

            dropIndex = Mathf.Clamp(row * NumColumns + col, 0, count);
            if (dropIndex == count && count % NumColumns == 0)
            {
                dropRow = rows - 1;
                dropCol = NumColumns;
            }
            else
            {
                dropRow = dropIndex / NumColumns;
                dropCol = dropIndex % NumColumns;
            }
        }

        private void ApplyDrop(List<Story> stories)
        {
            if (dropIndex < 0) return;

            int target = dropIndex;
            if (target > draggingIndex)
            {
                target--;
            }
            target = Mathf.Clamp(target, 0, stories.Count - 1);
            if (target == draggingIndex) return;

            var story = stories[draggingIndex];
            stories.RemoveAt(draggingIndex);
            stories.Insert(target, story);
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
        }

        private void AutoScroll(Rect outRect, Rect viewRect, Vector2 mouse)
        {
            float maxScroll = Mathf.Max(0f, viewRect.height - outRect.height);
            if (maxScroll <= 0f) return;

            if (mouse.y < scrollPosition.y + AutoScrollEdge)
            {
                scrollPosition.y = Mathf.Max(0f, scrollPosition.y - AutoScrollSpeed);
            }
            else if (mouse.y > scrollPosition.y + outRect.height - AutoScrollEdge)
            {
                scrollPosition.y = Mathf.Min(maxScroll, scrollPosition.y + AutoScrollSpeed);
            }
        }

        private void ClearPress()
        {
            pressIndex = -1;
            pressKind = PressCard;
            draggingIndex = -1;
            dropIndex = -1;
        }

        private void RegisterPress(Rect rect, int index, int kind)
        {
            Event e = Event.current;
            if (e.rawType != EventType.MouseDown || e.button != 0 || !rect.Contains(e.mousePosition)) return;

            pressIndex = index;
            pressKind = kind;
            pressRect = rect;
            dragMouseStart = e.mousePosition;
            e.Use();
        }

        private void DrawStoryCard(Rect cardRect, Story story, int index, bool dragging)
        {
            var coverRect = new Rect(cardRect.x, cardRect.y, cardRect.width, CoverHeight);
            var titleRect = new Rect(cardRect.x, coverRect.yMax, cardRect.width, TitleStripHeight);
            var actionRect = new Rect(cardRect.x, titleRect.yMax, cardRect.width, ActionStripHeight);

            StoryCoverUtility.DrawCover(coverRect, story.CoverIcon, story.iconColor);

            Widgets.DrawBoxSolid(titleRect, TitleStrip);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titleRect.ContractedBy(6f, 2f), story.title);
            Text.Anchor = TextAnchor.UpperLeft;

            var readableRect = new Rect(cardRect.x, cardRect.y, cardRect.width, CoverHeight + TitleStripHeight);
            bool live = index >= 0 && !dragging;

            if (live)
            {
                MouseoverSounds.DoRegion(readableRect);
                Widgets.DrawHighlightIfMouseover(readableRect);
                TooltipHandler.TipRegion(readableRect, BuildTooltip(story));

                RegisterPress(readableRect, index, PressCard);
                if (pressIndex == index && pressKind == PressCard && Event.current.rawType == EventType.MouseDown)
                {
                    dragGrabOffset = Event.current.mousePosition - cardRect.position;
                }
            }

            var readRect = new Rect(actionRect.x, actionRect.y, actionRect.width / 2f, actionRect.height);
            var editRect = new Rect(actionRect.center.x, actionRect.y, actionRect.width / 2f, actionRect.height);

            DrawActionButton(readRect, GizmoUtility.ReadIcon, "WB_StoryRead".Translate(), live);
            DrawActionButton(editRect, GizmoUtility.EditIcon, "WB_StoryEdit".Translate(), live);

            if (live)
            {
                RegisterPress(readRect, index, PressRead);
                RegisterPress(editRect, index, PressEdit);
            }

            Widgets.DrawBox(cardRect);
        }

        private static string BuildTooltip(Story story)
        {
            string tip = story.title;
            if (!story.text.NullOrEmpty())
            {
                string snippet = story.text.Length > 220 ? story.text.Substring(0, 220) + "..." : story.text;
                tip = tip.NullOrEmpty() ? snippet : tip + "\n\n" + snippet;
            }
            if (!tip.NullOrEmpty())
            {
                tip += "\n\n";
            }
            return tip + "WB_StoryReorderHint".Translate().Colorize(ColoredText.SubtleGrayColor);
        }

        private static void DrawActionButton(Rect rect, Texture2D icon, string tooltip, bool interactive)
        {
            Widgets.DrawBoxSolid(rect, TitleStrip);
            if (interactive)
            {
                Widgets.DrawHighlightIfMouseover(rect);
                TooltipHandler.TipRegion(rect, tooltip);
            }

            float size = rect.height - 10f;
            var iconRect = new Rect(rect.center.x - size / 2f, rect.center.y - size / 2f, size, size);
            GUI.color = (interactive && Mouse.IsOver(rect)) ? Color.white : new Color(1f, 1f, 1f, 0.75f);
            Widgets.DrawTextureFitted(iconRect, icon, 1f);
            GUI.color = Color.white;
        }

        private static void OpenStory(Story story)
        {
            Find.WindowStack.Add(new NarrativeWindow(story.title, story.text, story.CoverIcon, story.iconColor));
        }

        private void DrawBottomButtons(Rect inRect, float buttonHeight)
        {
            float buttonWidth = 200f;
            float spacing = 40f;
            float totalWidth = buttonWidth * 2f + spacing;
            float startX = inRect.x + (inRect.width - totalWidth) / 2f;
            float y = inRect.yMax - buttonHeight;

            if (Widgets.ButtonText(new Rect(startX, y, buttonWidth, buttonHeight), "WB_StoryLibraryCreate".Translate()))
            {
                Find.WindowStack.Add(new Window_StoryEditor());
            }
            if (Widgets.ButtonText(new Rect(startX + buttonWidth + spacing, y, buttonWidth, buttonHeight), "DoneButton".Translate()))
            {
                Close();
            }
        }
    }
}
