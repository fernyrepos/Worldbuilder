using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    public class ScenPart_StartInWorld : ScenPart
    {
        public string worldPresetName;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref worldPresetName, "worldPresetName");
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            Rect rect = listing.GetScenPartRect(this, RowHeight);
            string buttonLabel = worldPresetName.NullOrEmpty() ? "WB_SelectAPreset".Translate() : worldPresetName;
            if (Widgets.ButtonText(rect, buttonLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("WB_None".Translate(), delegate
                {
                    worldPresetName = null;
                }));

                foreach (WorldPreset preset in WorldPresetManager.GetAllPresets(true))
                {
                    options.Add(new FloatMenuOption(preset.Label, delegate
                    {
                        worldPresetName = preset.name;
                    }));
                }

                if (options.Count == 1)
                {
                    options.Add(new FloatMenuOption("WB_NoPresetsFound".Translate(), null));
                }

                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        public override string Summary(Scenario scen)
        {
            if (worldPresetName.NullOrEmpty())
            {
                return "WB_ScenPart_StartInWorld_NoPresetSelected".Translate();
            }
            return "WB_ScenPart_StartInWorld_Summary".Translate(worldPresetName);
        }
    }
}
