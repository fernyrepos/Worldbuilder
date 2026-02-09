using Verse;
using UnityEngine;

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
            var titleRect = new Rect(inRect.x, inRect.y, inRect.width, titleHeight);
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

            var scrollRectOuter = new Rect(
                inRect.x,
                inRect.y + titleHeight + gapAfterTitle,
                inRect.width,
                inRect.height - titleHeight - gapAfterTitle - gapBeforeButtons - buttonHeight
            );

            var stories = World_ExposeData_Patch.worldStories;

            var numRows = Mathf.CeilToInt((float)stories.Count / numColumns);
            float totalGridContentHeight = (numRows * cardHeight) + (Mathf.Max(0, numRows - 1) * verticalSpacing);
            float totalGridContentWidth = (cardWidth * numColumns) + (horizontalSpacing * (numColumns - 1));

            var scrollRectViewWidth = Mathf.Max(scrollRectOuter.width - 16f, totalGridContentWidth);
            var scrollRectView = new Rect(0f, 0f, scrollRectViewWidth, totalGridContentHeight);

            Widgets.BeginScrollView(scrollRectOuter, ref scrollPosition, scrollRectView);

            float gridStartXInScrollView = (scrollRectView.width > totalGridContentWidth) ? (scrollRectView.width - totalGridContentWidth) / 2f : 0f;

            for (int i = 0; i < stories.Count; i++)
            {
                var story = stories[i];
                int col = i % numColumns;
                int row = i / numColumns;

                float currentX = gridStartXInScrollView + (col * (cardWidth + horizontalSpacing));
                float currentY = row * (cardHeight + verticalSpacing);

                var cardRect = new Rect(currentX, currentY, cardWidth, cardHeight);
                Widgets.DrawWindowBackground(cardRect);

                var storyTitleRect = new Rect(cardRect.x + 5f, cardRect.y + 5f, cardRect.width - 10f, cardHeight - 45f);
                Widgets.DrawMenuSection(storyTitleRect);
                Text.Anchor = TextAnchor.MiddleCenter;
                Text.Font = GameFont.Medium;
                Widgets.Label(storyTitleRect, story.title);
                Text.Anchor = TextAnchor.UpperLeft;
                Text.Font = GameFont.Small;
                if (Widgets.ButtonInvisible(storyTitleRect))
                {
                    Find.WindowStack.Add(new NarrativeWindow(story.title, story.text));
                }
                float iconButtonHeight = 25f;

                float buttonAreaHeight = 40f;
                var buttonAreaRect = new Rect(cardRect.x, cardRect.yMax - buttonAreaHeight, cardRect.width, buttonAreaHeight);

                float iconButtonY = buttonAreaRect.y + (buttonAreaRect.height - iconButtonHeight) / 2f;

                var viewBackgroundRect = new Rect(buttonAreaRect.x, buttonAreaRect.y, buttonAreaRect.width / 2f, buttonAreaRect.height);
                var editBackgroundRect = new Rect(buttonAreaRect.x + buttonAreaRect.width / 2f, buttonAreaRect.y, buttonAreaRect.width / 2f, buttonAreaRect.height);
                viewBackgroundRect = viewBackgroundRect.ContractedBy(5f);
                editBackgroundRect = editBackgroundRect.ContractedBy(5f);

                float viewButtonHalfWidth = buttonAreaRect.width / 2f;
                float viewButtonX = buttonAreaRect.x + (viewButtonHalfWidth / 2f) - (iconButtonHeight / 2f);
                var viewButtonRect = new Rect(viewButtonX, iconButtonY, iconButtonHeight, iconButtonHeight);

                float editButtonHalfWidth = buttonAreaRect.width / 2f;
                float editButtonX = buttonAreaRect.x + viewButtonHalfWidth + (editButtonHalfWidth / 2f) - (iconButtonHeight / 2f);
                var editButtonRect = new Rect(editButtonX, iconButtonY, iconButtonHeight, iconButtonHeight);

                DrawWindowBackground(viewBackgroundRect, new ColorInt(147, 142, 142).ToColor);
                if (Widgets.ButtonImage(viewButtonRect, GizmoUtility.ReadIcon))
                {
                    Find.WindowStack.Add(new NarrativeWindow(story
                    .title, story.text));
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

            var createButtonRect = new Rect(
                startX,
                inRect.yMax - buttonHeight,
                buttonWidth,
                buttonHeight
            );

            if (Widgets.ButtonText(createButtonRect, "WB_StoryLibraryCreate".Translate()))
            {
                Find.WindowStack.Add(new Window_StoryEditor());
            }

            var doneButtonRect = new Rect(
                startX + buttonWidth + buttonSpacing,
                inRect.yMax - buttonHeight,
                buttonWidth,
                buttonHeight
            );

            if (Widgets.ButtonText(doneButtonRect, "DoneButton".Translate()))
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
