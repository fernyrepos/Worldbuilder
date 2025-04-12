using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
namespace Worldbuilder
{
    [HotSwappable]
    public class Window_WorldSettings : Window
    {
        private WorldPreset preset;
        private const float CheckboxHeight = 24f;
        private const float SectionSpacing = 15f;
        private const float Indent = 15f;

        public override Vector2 InitialSize => new Vector2(450f, 350f);

        public Window_WorldSettings(WorldPreset presetToConfigure)
        {
            forcePause = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            draggable = true;
            preset = presetToConfigure;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            Rect contentRect = inRect;
            listing.Begin(contentRect);

            Text.Font = GameFont.Medium;
            listing.Label("WB_WorldSettingsTitle".Translate(preset.name ?? "New Preset"));
            Text.Font = GameFont.Small;
            listing.Gap(6f);
            Listing_Standard saveFlagsListing = listing;

            var prevSaveFactions = preset.saveFactions;
            var prevSaveIdeologies = preset.saveIdeologies;
            var prevSaveBases = preset.saveBases;

            saveFlagsListing.CheckboxLabeled("WB_WorldSettingsSaveFactions".Translate(), ref preset.saveFactions, "WB_WorldSettingsSaveFactionsTooltip".Translate());
            saveFlagsListing.Gap(10f);
            
            saveFlagsListing.CheckboxLabeled("WB_WorldSettingsSaveIdeologies".Translate(), ref preset.saveIdeologies, "WB_WorldSettingsSaveIdeologiesTooltip".Translate());
            saveFlagsListing.CheckboxLabeled("WB_WorldSettingsSaveBases".Translate(), ref preset.saveBases, "WB_WorldSettingsSaveBasesTooltip".Translate());
            saveFlagsListing.Gap(10f);


            saveFlagsListing.CheckboxLabeled("WB_WorldSettingsSaveTerrain".Translate(), ref preset.saveTerrain, "WB_WorldSettingsSaveTerrainTooltip".Translate());
            saveFlagsListing.CheckboxLabeled("WB_WorldSettingsSaveMapMarkers".Translate(), ref preset.saveMapMarkers, "WB_WorldSettingsSaveMapMarkersTooltip".Translate());
            saveFlagsListing.CheckboxLabeled("WB_WorldSettingsSaveMapText".Translate(), ref preset.saveWorldFeatures, "WB_WorldSettingsSaveMapTextTooltip".Translate());
            saveFlagsListing.Gap(10f);

            saveFlagsListing.CheckboxLabeled("WB_WorldSettingsSaveStorykeeperEntries".Translate(), ref preset.saveStorykeeperEntries, "WB_WorldSettingsSaveStorykeeperEntriesTooltip".Translate());

            if (!prevSaveFactions && preset.saveFactions && ModsConfig.IdeologyActive && !preset.saveIdeologies)
            {
                preset.saveIdeologies = true;
                Messages.Message("WB_WorldSettingsIdeologyRequiredMsg".Translate(), MessageTypeDefOf.NeutralEvent, false);
            }

            if (!prevSaveBases && preset.saveBases && !preset.saveFactions)
            {
                preset.saveFactions = true;
                Messages.Message("WB_WorldSettingsFactionsRequiredMsg".Translate(), MessageTypeDefOf.NeutralEvent, false);
            }

            listing.End();
            float buttonWidth = 120f;
            float buttonHeight = 35f;
            Rect closeButtonRect = new Rect(inRect.width / 2f - buttonWidth / 2f, inRect.height - buttonHeight, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(closeButtonRect, "Close".Translate()))
            {
                this.Close();
            }
        }
    }
}