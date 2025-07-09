using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Verse.Profile;
using RimWorld.Planet;

namespace Worldbuilder
{
    [HotSwappable]
    public class Window_TransitionWorld : Window
    {
        private Vector2 scrollPosition = Vector2.zero;
        private List<WorldPreset> allPresets;

        public override Vector2 InitialSize => new Vector2(400f, 500f);

        public Window_TransitionWorld()
        {
            forcePause = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            draggable = true;
            allPresets = WorldPresetManager.GetAllPresets(true).ToList();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);

            Text.Font = GameFont.Medium;
            listing.Label("WB_TransitionWorldLabel".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(10f);

            listing.Label("WB_TransitionWorldExplanation".Translate());
            listing.Gap(10f);

            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, allPresets.Count * 30f);
            Rect scrollRect = new Rect(0f, listing.CurHeight, inRect.width, inRect.height - listing.CurHeight - 40f);
            Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);

            float currentY = 0f;
            foreach (var preset in allPresets)
            {
                Rect rowRect = new Rect(0f, currentY, viewRect.width, 28f);
                if (Widgets.ButtonText(rowRect, preset.name))
                {
                    World_ExposeData_Patch.WorldPresetName = preset.name;
                }
                currentY += 30f;
            }
            Widgets.EndScrollView();

            float buttonWidth = 120f;
            float buttonHeight = 35f;
            Rect closeButtonRect = new Rect(inRect.width / 2f - buttonWidth / 2f, inRect.height - buttonHeight, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(closeButtonRect, "Close".Translate()))
            {
                Close();
            }

            listing.End();
        }
    }
}
