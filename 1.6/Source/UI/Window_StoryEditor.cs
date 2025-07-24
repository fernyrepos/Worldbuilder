using Verse;
using UnityEngine;
using RimWorld;

namespace Worldbuilder
{
    [HotSwappable]
    public class Window_StoryEditor : Window
    {
        private Story story;
        private string currentTitle = "";
        private string currentContent = "";
        public override Vector2 InitialSize => new Vector2(700f, 500f);
        public Window_StoryEditor()
        {
            this.story = new Story();
            this.forcePause = true;
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
            this.draggable = true;
            this.closeOnAccept = false;
        }
        public Window_StoryEditor(Story story)
        {
            this.story = story;
            this.currentTitle = story.title;
            this.currentContent = story.text;
            this.forcePause = true;
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
            this.draggable = true;
            this.closeOnAccept = false;
        }


        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            Text.Font = GameFont.Medium;
            string titleString = "WB_StoryEditorTitle".Translate();
            Rect titleLineRect = listing.GetRect(Text.CalcHeight(titleString, listing.ColumnWidth));
            Vector2 titleSize = Text.CalcSize(titleString);
            Widgets.Label(new Rect(titleLineRect.x, titleLineRect.y, titleSize.x, titleSize.y), titleString);
            if (World_ExposeData_Patch.worldStories.Contains(this.story))
            {
                Rect buttonRect = new Rect(titleLineRect.x + titleSize.x + 5f, titleLineRect.y + 5, 24f, 24f);
                if (Widgets.ButtonImage(buttonRect, TexButton.Delete))
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("WB_StoryEditorDeleteConfirm".Translate(), delegate
                    {
                        World_ExposeData_Patch.worldStories.Remove(this.story);
                        Close();
                    }, destructive: true));
                }
            }
            listing.Gap(listing.verticalSpacing);
            Text.Font = GameFont.Small;

            listing.Label("Title".Translate() + ":");
            currentTitle = listing.TextEntry(currentTitle);

            listing.Gap(12f);

            listing.Label("WB_Story".Translate() + ":");
            Rect textRect = listing.GetRect(inRect.height - 157f);
            currentContent = Widgets.TextArea(textRect, currentContent);

            listing.Gap(12f);

            if (listing.ButtonText("WB_StoryEditorSave".Translate()))
            {
                story.title = currentTitle;
                story.text = currentContent;
                var existingStory = World_ExposeData_Patch.worldStories.FirstOrDefault(s => s == this.story);
                if (existingStory != null)
                {
                    existingStory.title = currentTitle;
                    existingStory.text = currentContent;
                }
                else
                {
                    World_ExposeData_Patch.worldStories.Add(story);
                }
                Close();
            }

            listing.End();
        }
    }
}
