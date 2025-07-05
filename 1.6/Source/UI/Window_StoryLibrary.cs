using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace Worldbuilder
{
    [HotSwappable]
    public class Window_StoryLibrary : Window
    {
        private Vector2 scrollPosition = Vector2.zero;
        public override Vector2 InitialSize => new Vector2(850f, 600f);

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
            Text.Font = GameFont.Medium;
            float titleHeight = Text.LineHeight;
            Rect titleRect = new Rect(inRect.x, inRect.y, inRect.width, titleHeight);
            Widgets.Label(titleRect, "WB_StoryLibraryTitle".Translate());
            Text.Font = GameFont.Small;

            float cardWidth = 180f;
            float cardHeight = 230f;
            float horizontalSpacing = 20f;
            float verticalSpacing = 10f;
            int numColumns = 4;

            float buttonHeight = 40f;
            float gapAfterTitle = 12f;
            float gapBeforeButtons = 12f;

            Rect scrollRectOuter = new Rect(
                inRect.x,
                inRect.y + titleHeight + gapAfterTitle,
                inRect.width,
                inRect.height - titleHeight - gapAfterTitle - gapBeforeButtons - buttonHeight
            );

            var stories = World_ExposeData_Patch.worldStories;

            int numRows = Mathf.CeilToInt((float)stories.Count / numColumns);
            float totalGridContentHeight = (numRows * cardHeight) + (Mathf.Max(0, numRows - 1) * verticalSpacing);
            float totalGridContentWidth = (cardWidth * numColumns) + (horizontalSpacing * (numColumns - 1));

            float scrollRectViewWidth = Mathf.Max(scrollRectOuter.width - 16f, totalGridContentWidth);
            Rect scrollRectView = new Rect(0f, 0f, scrollRectViewWidth, totalGridContentHeight);

            Widgets.BeginScrollView(scrollRectOuter, ref scrollPosition, scrollRectView);

            float gridStartXInScrollView = (scrollRectView.width > totalGridContentWidth) ? (scrollRectView.width - totalGridContentWidth) / 2f : 0f;

            for (int i = 0; i < stories.Count; i++)
            {
                var story = stories[i];
                int col = i % numColumns;
                int row = i / numColumns;

                float currentX = gridStartXInScrollView + (col * (cardWidth + horizontalSpacing));
                float currentY = row * (cardHeight + verticalSpacing);

                Rect cardRect = new Rect(currentX, currentY, cardWidth, cardHeight);
                Widgets.DrawWindowBackground(cardRect);

                Rect storyTitleRect = new Rect(cardRect.x + 5f, cardRect.y + 5f, cardRect.width - 10f, cardHeight - 45f);
                Widgets.DrawMenuSection(storyTitleRect);
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Medium;
                Widgets.Label(storyTitleRect, story.title);
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;

                float iconButtonHeight = 25f;

                float buttonAreaHeight = 40f;
                Rect buttonAreaRect = new Rect(cardRect.x, cardRect.yMax - buttonAreaHeight, cardRect.width, buttonAreaHeight);

                float iconButtonY = buttonAreaRect.y + (buttonAreaRect.height - iconButtonHeight) / 2f;

                // Calculate Rects for the backgrounds, occupying each half
                Rect viewBackgroundRect = new Rect(buttonAreaRect.x, buttonAreaRect.y, buttonAreaRect.width / 2f, buttonAreaRect.height);
                Rect editBackgroundRect = new Rect(buttonAreaRect.x + buttonAreaRect.width / 2f, buttonAreaRect.y, buttonAreaRect.width / 2f, buttonAreaRect.height);
                viewBackgroundRect = viewBackgroundRect.ContractedBy(5f);
                editBackgroundRect = editBackgroundRect.ContractedBy(5f);

                // Calculate Rects for the icons, centered within their respective halves
                float viewButtonHalfWidth = buttonAreaRect.width / 2f;
                float viewButtonX = buttonAreaRect.x + (viewButtonHalfWidth / 2f) - (iconButtonHeight / 2f);
                Rect viewButtonRect = new Rect(viewButtonX, iconButtonY, iconButtonHeight, iconButtonHeight);

                float editButtonHalfWidth = buttonAreaRect.width / 2f;
                float editButtonX = buttonAreaRect.x + viewButtonHalfWidth + (editButtonHalfWidth / 2f) - (iconButtonHeight / 2f);
                Rect editButtonRect = new Rect(editButtonX, iconButtonY, iconButtonHeight, iconButtonHeight);

                DrawWindowBackground(viewBackgroundRect, new ColorInt(147, 142, 142).ToColor);
                if (Widgets.ButtonImage(viewButtonRect, GizmoUtility.ReadIcon))
                {
                    Find.WindowStack.Add(new NarrativeWindow(story
                    .title + "\n\n" + story.text));
                }

                DrawWindowBackground(editBackgroundRect, new ColorInt(132, 125, 125).ToColor);
                if (Widgets.ButtonImage(editButtonRect, GizmoUtility.EditIcon))
                {
                    Find.WindowStack.Add(new Window_StoryEditor(story));
                }
            }

            Widgets.EndScrollView();

            float buttonWidth = 200f;
            float buttonSpacing = 150f;
            
            float totalButtonsWidth = (buttonWidth * 2) + buttonSpacing;
            float startX = inRect.x + (inRect.width - totalButtonsWidth) / 2f;

            Rect createButtonRect = new Rect(
                startX,
                inRect.yMax - buttonHeight,
                buttonWidth,
                buttonHeight
            );

            if (Widgets.ButtonText(createButtonRect, "WB_StoryLibraryCreate".Translate()))
            {
                Find.WindowStack.Add(new Window_StoryEditor());
            }

            Rect doneButtonRect = new Rect(
                startX + buttonWidth + buttonSpacing,
                inRect.yMax - buttonHeight,
                buttonWidth,
                buttonHeight
            );

            if (Widgets.ButtonText(doneButtonRect, "WB_Done".Translate()))
            {
                this.Close();
            }
        }

        public static void DrawWindowBackground(Rect rect, Color colorFactor)
        {
            Color color = GUI.color;
            GUI.color = colorFactor;
            GUI.DrawTexture(rect, BaseContent.WhiteTex);
            GUI.color = colorFactor;
            Widgets.DrawBox(rect);
            GUI.color = color;
        }
    }
}
