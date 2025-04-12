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
        private Vector2 scrollPosition = Vector2.zero;

        public override Vector2 InitialSize => new Vector2(700f, 600f);
        public Window_StoryEditor()
        {
            this.story = new Story();
            this.forcePause = true;
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
            this.draggable = true;
        }
        public Window_StoryEditor(Story story)
        {
            this.story = story;
            this.currentTitle = story.Title;
            this.currentContent = story.Content;
            this.forcePause = true;
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
            this.draggable = true;
        }


        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            Text.Font = GameFont.Medium;
            listing.Label("WB_StoryEditorTitle".Translate());
            Text.Font = GameFont.Small;
            listing.GapLine();

            listing.Label("Title".Translate() + ":");
            currentTitle = listing.TextEntry(currentTitle);

            listing.Gap(12f);

            listing.Label("Content".Translate() + ":");
            Rect textRect = listing.GetRect(inRect.height - 180f);
            currentContent = Widgets.TextArea(textRect, currentContent);

            listing.Gap(12f);

            if (listing.ButtonText("WB_StoryEditorSaveButton".Translate()))
            {
                story.Title = currentTitle;
                story.Content = currentContent;
                Messages.Message("Saved".Translate(), MessageTypeDefOf.PositiveEvent);
                Close();
            }

            listing.End();
        }
    }
}