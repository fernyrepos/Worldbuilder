using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public class Window_StoryEditor : Window
    {
        private Story story;
        private string currentTitle = "";
        private string currentContent = "";
        private IdeoIconDef currentIcon;
        private Color currentIconColor = Color.white;

        private const float CoverColumnWidth = 150f;
        private const float CoverSize = 150f;

        public override Vector2 InitialSize => new Vector2(760f, 540f);

        public Window_StoryEditor() : this(new Story())
        {
        }

        public Window_StoryEditor(Story story)
        {
            this.story = story;
            this.currentTitle = story.title ?? "";
            this.currentContent = story.text ?? "";
            this.currentIcon = story.iconDef;
            this.currentIconColor = story.iconColor;

            this.forcePause = true;
            this.doCloseX = true;
            this.absorbInputAroundWindow = true;
            this.draggable = true;
            this.closeOnAccept = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            string titleString = "WB_StoryEditorTitle".Translate();
            var titleRect = new Rect(inRect.x, inRect.y, inRect.width, Text.LineHeight);
            Widgets.Label(titleRect, titleString);

            if (World_ExposeData_Patch.worldStories.Contains(story))
            {
                var deleteRect = new Rect(titleRect.x + Text.CalcSize(titleString).x + 8f, titleRect.y + 4f, 24f, 24f);
                if (Widgets.ButtonImage(deleteRect, TexButton.Delete))
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("WB_StoryEditorDeleteConfirm".Translate(), delegate
                    {
                        World_ExposeData_Patch.worldStories.Remove(story);
                        Close();
                    }, destructive: true));
                }
            }
            Text.Font = GameFont.Small;

            float bodyTop = titleRect.yMax + 10f;
            float saveHeight = 38f;
            float bodyBottom = inRect.yMax - saveHeight - 10f;

            bool showCover = Window_IdeoIconPicker.Available;
            float textLeft = inRect.x;

            if (showCover)
            {
                DrawCoverColumn(new Rect(inRect.x, bodyTop, CoverColumnWidth, bodyBottom - bodyTop));
                textLeft = inRect.x + CoverColumnWidth + 16f;
            }

            DrawTextColumn(new Rect(textLeft, bodyTop, inRect.xMax - textLeft, bodyBottom - bodyTop));

            if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - saveHeight, inRect.width, saveHeight), "WB_StoryEditorSave".Translate()))
            {
                Save();
            }
        }

        private void DrawCoverColumn(Rect rect)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            var labelRect = new Rect(rect.x, rect.y, rect.width, 22f);
            Widgets.Label(labelRect, "WB_StoryCover".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            var coverRect = new Rect(rect.x, labelRect.yMax + 2f, CoverSize, CoverSize);
            StoryCoverUtility.DrawCover(coverRect, currentIcon?.Icon ?? GizmoUtility.NarrativeGizmoIcon, currentIconColor);
            Widgets.DrawHighlightIfMouseover(coverRect);
            TooltipHandler.TipRegion(coverRect, "WB_StoryChooseIcon".Translate());
            if (Widgets.ButtonInvisible(coverRect))
            {
                OpenIconPicker();
            }

            float y = coverRect.yMax + 8f;
            if (Widgets.ButtonText(new Rect(rect.x, y, rect.width, 28f), "WB_StoryChooseIcon".Translate()))
            {
                OpenIconPicker();
            }
            y += 32f;

            var colorRect = new Rect(rect.x, y, rect.width, 28f);
            if (Widgets.ButtonText(colorRect, "WB_CustomizeSetColor".Translate()))
            {
                Find.WindowStack.Add(new Window_ColorPicker(currentIconColor,
                    color => currentIconColor = color,
                    onColorPreview: color => currentIconColor = color));
            }
            y += 32f;

            var swatchRect = new Rect(rect.x, y, rect.width, 16f);
            Widgets.DrawBoxSolid(swatchRect, currentIconColor);
            Widgets.DrawBox(swatchRect);
        }

        private void OpenIconPicker()
        {
            Find.WindowStack.Add(new Window_IdeoIconPicker(currentIcon, icon => currentIcon = icon));
        }

        private void DrawTextColumn(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);

            listing.Label("Title".Translate() + ":");
            currentTitle = listing.TextEntry(currentTitle);
            listing.Gap(12f);

            listing.Label("WB_Story".Translate() + ":");
            var textRect = listing.GetRect(rect.yMax - listing.CurHeight - rect.y);
            currentContent = Widgets.TextArea(textRect, currentContent);

            listing.End();
        }

        private void Save()
        {
            story.title = currentTitle;
            story.text = currentContent;
            story.iconDef = currentIcon;
            story.iconColor = currentIconColor;
            story.ClearIconCache();

            if (!World_ExposeData_Patch.worldStories.Contains(story))
            {
                World_ExposeData_Patch.worldStories.Add(story);
            }
            Close();
        }
    }
}
