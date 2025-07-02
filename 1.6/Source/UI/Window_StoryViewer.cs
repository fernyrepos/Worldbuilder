using Verse;
using UnityEngine;

namespace Worldbuilder
{
    [HotSwappable]
    public class Window_StoryViewer : Window
    {
        private Story story;
        private Vector2 scrollPosition = Vector2.zero;

        public override Vector2 InitialSize => new Vector2(700f, 600f);

        public Window_StoryViewer(Story story)
        {
            this.story = story;
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
            listing.Label(story.Title);
            Text.Font = GameFont.Small;
            listing.GapLine();

            Rect textRect = listing.GetRect(inRect.height - 100f);
            Widgets.LabelScrollable(textRect, story.Content, ref scrollPosition);

            listing.Gap(12f);

            if (listing.ButtonText("Edit".Translate()))
            {
                Find.WindowStack.Add(new Window_StoryEditor(story));
                Close();
            }

            listing.End();
        }
    }
}