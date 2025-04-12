using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace Worldbuilder
{
    [HotSwappable]
    public class Window_StoryLibrary : Window
    {
        private Vector2 scrollPosition = Vector2.zero;
        private List<Story> stories;

        public override Vector2 InitialSize => new Vector2(500f, 600f);

        public Window_StoryLibrary()
        {
            this.forcePause = true;
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
            this.draggable = true;
            stories = StoryUtility.GetStories();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            Text.Font = GameFont.Medium;
            listing.Label("WB_StoryLibraryTitle".Translate());
            Text.Font = GameFont.Small;
            listing.GapLine();
            listing.Label("Existing".Translate() + ":");
            Rect scrollRectOuter = listing.GetRect(200f);
            Rect scrollRectView = new Rect(0f, 0f, scrollRectOuter.width - 16f, stories.Count * 30f);
            Widgets.BeginScrollView(scrollRectOuter, ref scrollPosition, scrollRectView);
            foreach (var story in StoryUtility.GetStories())
            {
                if (Widgets.ButtonText(listing.GetRect(30f), story.Title))
                {
                    Find.WindowStack.Add(new Window_StoryViewer(story));
                }
            }
            Widgets.EndScrollView();

            listing.Gap(12f);

            if (listing.ButtonText("WB_StoryLibraryCreateButton".Translate()))
            {
                Find.WindowStack.Add(new Window_StoryEditor());
            }

            listing.End();
        }
    }
}