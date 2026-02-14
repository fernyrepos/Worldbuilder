using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public class Window_MapMarkerCustomization : Window_WorldObjectCustomization
    {
        private WorldObject_MapMarker marker;
        private string currentMarkerName = "";
        private string currentDescription = "";
        private MarkerData markerCustomData;
        public Window_MapMarkerCustomization(WorldObject_MapMarker marker)
            : base()
        {
            this.marker = marker;
            var data = MarkerDataManager.GetData(marker);
            markerCustomData = new MarkerData();
            markerCustomData.narrativeText = data?.narrativeText ?? "";
            currentMarkerName = data?.name ?? marker.Label;
            currentDescription = data?.description ?? marker.def.description;
            this.customizationData = new CustomizationData();
            this.customizationData.narrativeText = markerCustomData.narrativeText;
            this.customizationData.syncedFilePath = data?.syncedFilePath;
            this.customizationData.syncToExternalFile = data?.syncToExternalFile ?? false;
            this.selectedCulturalIconDef = data?.iconDef;
            this.selectedFactionIconDef = data?.factionIconDef;
            this.selectedColor = data?.color;
        }

        protected override void DrawDetailTab(Rect tabRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(tabRect);
            Text.Font = GameFont.Small;

            Rect nameLabelRect = listing.GetRect(24f);
            Widgets.Label(nameLabelRect, "WB_MapMarkerNameLabel".Translate());

            Rect nameFieldRect = listing.GetRect(30f);
            currentMarkerName = Widgets.TextField(nameFieldRect, currentMarkerName);

            listing.Gap(12f);
            Rect descriptionLabelRect = listing.GetRect(24f);
            Widgets.Label(descriptionLabelRect, "WB_DescriptionLabel".Translate());

            Rect descriptionFieldRect = listing.GetRect(100f);
            currentDescription = Widgets.TextArea(descriptionFieldRect, currentDescription);

            listing.End();
        }
        
        protected override void DrawBottomButtons(Rect inRect)
        {
            float buttonWidth = 150f;
            float buttonHeight = 32f;
            float buttonY = inRect.yMax - buttonHeight;

            float centerX = inRect.x + (inRect.width / 2) - (buttonWidth / 2);
            Rect saveButtonRect = new Rect(centerX, buttonY, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(saveButtonRect, "WB_CustomizeSave".Translate()))
            {
                SaveIndividualChanges();
            }
        }

        protected override void SaveIndividualChanges()
        {
            if (string.IsNullOrWhiteSpace(currentMarkerName))
            {
                Messages.Message("WB_MapMarkerNameEmptyError".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            var markerData = MarkerDataManager.GetData(marker);
            markerData.name = currentMarkerName;
            markerData.description = currentDescription;
            markerData.narrativeText = customizationData.narrativeText;
            markerData.iconDef = this.selectedCulturalIconDef;
            markerData.factionIconDef = this.selectedFactionIconDef;
            markerData.color = this.selectedColor.GetValueOrDefault();
            markerData.syncedFilePath = customizationData.syncedFilePath;
            markerData.syncToExternalFile = customizationData.syncToExternalFile;

            base.SaveIndividualChanges();

            Messages.Message("WB_MapMarkerSaveSuccess".Translate(), MessageTypeDefOf.PositiveEvent);
            markerData.ClearIconCache();
            Find.World.renderer.SetDirty<WorldDrawLayer_WorldObjects>(marker.Tile.Layer);
            Close();
        }
    }
}
