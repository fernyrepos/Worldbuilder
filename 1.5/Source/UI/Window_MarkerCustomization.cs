using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using LudeonTK;
using System.Linq;

namespace Worldbuilder
{
    [HotSwappable]
    public class Window_MarkerCustomization : Window
    {
        private WorldObject marker;
        private MarkerData markerData;

        private int currentTab = 0;
        private Vector2 narrativeScrollPosition = Vector2.zero;
        private Vector2 iconScrollPosition = Vector2.zero;

        private List<IdeoIconDef> availableIcons;
        private IdeoIconDef selectedIconDef;
        private Color selectedColor;
        private const float IconSize = 64f;
        private const float IconPadding = 10f;
        private const float ColorBoxSize = 20f;
        private const float ColorBoxSpacing = 6f;

        public override Vector2 InitialSize => new Vector2(700, 600);

        public Window_MarkerCustomization(WorldObject marker)
        {
            this.marker = marker;
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.preventCameraMotion = false;
            this.closeOnAccept = false;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            var originalData = MarkerDataManager.GetOrCreateData(marker);
            markerData = new MarkerData
            {
                name = originalData.name ?? marker.def.label,
                description = originalData.description,
                narrativeText = originalData.narrativeText,
                iconDefName = originalData.iconDefName,
                color = originalData.color
            };

            selectedIconDef = markerData.IconDef;
            selectedColor = markerData.color;
            availableIcons = DefDatabase<IdeoIconDef>.AllDefsListForReading
                .OrderBy(i => i.label)
                .ToList();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            var titleRect = new Rect(inRect.x, inRect.y, inRect.width, 32f);
            Widgets.Label(titleRect, "WB_MarkerCustomizeTitle".Translate());
            Text.Font = GameFont.Small;

            var tabAreaRect = new Rect(inRect.x, titleRect.yMax + 5f, inRect.width, 32f);
            var contentRect = new Rect(inRect.x, tabAreaRect.yMax, inRect.width, inRect.height - tabAreaRect.height - titleRect.height - 70f);

            List<TabRecord> tabsList = new List<TabRecord>();
            tabsList.Add(new TabRecord("WB_CustomizeAppearance".Translate(), delegate { currentTab = 0; }, currentTab == 0));
            tabsList.Add(new TabRecord("WB_CustomizeDetail".Translate(), delegate { currentTab = 1; }, currentTab == 1));
            tabsList.Add(new TabRecord("WB_CustomizeNarrative".Translate(), delegate { currentTab = 2; }, currentTab == 2));
            TabDrawer.DrawTabs(tabAreaRect, tabsList, maxTabWidth: 300);

            Widgets.DrawMenuSection(contentRect);
            var innerContentRect = contentRect.ContractedBy(15f);

            switch (currentTab)
            {
                case 0:
                    DrawAppearanceTab(innerContentRect);
                    break;
                case 1:
                    DrawDetailTab(innerContentRect);
                    break;
                case 2:
                    DrawNarrativeTab(innerContentRect);
                    break;
            }

            DrawBottomButtons(inRect);
        }

        private void DrawAppearanceTab(Rect tabRect)
        {
            float iconGridHeight = tabRect.height - 40f;
            Rect iconGridRect = new Rect(tabRect.x, tabRect.y, tabRect.width, iconGridHeight);
            DrawIdeoIconSelectorGrid(iconGridRect, availableIcons, ref selectedIconDef, ref iconScrollPosition);
            Rect colorPickerRect = new Rect(tabRect.x, tabRect.yMax - 30f, tabRect.width, 30f);
            Widgets.Label(colorPickerRect.LeftPartPixels(100f), "WB_MarkerCustomizeColorLabel".Translate());
            Rect colorButtonRect = new Rect(colorPickerRect.x + 110f, colorPickerRect.y, 150f, colorPickerRect.height);
            if (Widgets.ButtonText(colorButtonRect, "WB_MarkerCustomizeSelectColorButton".Translate()))
            {
                Find.WindowStack.Add(new Dialog_ChooseColor(
                    "WB_MarkerCustomizeSelectColorTitle".Translate(),
                    selectedColor,
                    null,
                    (Color c) => { selectedColor = c; markerData.color = c; Find.World.renderer.SetDirty<WorldLayer_WorldObjects>(); }
                ));
            }
            Rect colorPreviewRect = new Rect(colorButtonRect.xMax + 10f, colorPickerRect.y + (colorPickerRect.height - 20f) / 2f, 20f, 20f);
            Widgets.DrawBoxSolid(colorPreviewRect, selectedColor);
        }

        private void DrawIdeoIconSelectorGrid(Rect rect, List<IdeoIconDef> iconDefs, ref IdeoIconDef selectedDef, ref Vector2 scrollPos)
        {
            int iconsPerRow = Mathf.FloorToInt(rect.width / (IconSize + IconPadding));
            if (iconsPerRow < 1) iconsPerRow = 1;
            float totalGridHeight = Mathf.Ceil((float)iconDefs.Count / iconsPerRow) * (IconSize + IconPadding);
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, totalGridHeight);
            Widgets.BeginScrollView(rect, ref scrollPos, viewRect);
            for (int i = 0; i < iconDefs.Count; i++)
            {
                int row = i / iconsPerRow; int col = i % iconsPerRow;
                float x = col * (IconSize + IconPadding); float y = row * (IconSize + IconPadding);
                Rect iconRect = new Rect(x, y, IconSize, IconSize);
                Widgets.DrawOptionBackground(iconRect, selectedDef == iconDefs[i]);
                Texture2D iconTex = iconDefs[i].Icon;
                if (iconTex != null)
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(iconRect.ContractedBy(4f), iconTex, ScaleMode.ScaleToFit);
                    GUI.color = Color.white;
                }
                if (Widgets.ButtonInvisible(iconRect))
                {
                    selectedDef = iconDefs[i];
                    markerData.iconDefName = selectedDef?.defName;
                    Find.World.renderer.SetDirty<WorldLayer_WorldObjects>();
                }
                TooltipHandler.TipRegion(iconRect, iconDefs[i].LabelCap);
            }
            Widgets.EndScrollView();
        }

        private void DrawDetailTab(Rect tabRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(tabRect);

            listing.Label("WB_MarkerCustomizeNameLabel".Translate());
            markerData.name = listing.TextEntry(markerData.name ?? "");

            listing.Gap(12f);

            listing.Label("WB_MarkerCustomizeDescLabel".Translate());
            markerData.description = listing.TextEntry(markerData.description ?? "", 3);

            listing.End();
        }

        private void DrawNarrativeTab(Rect tabRect)
        {
            markerData.narrativeText = DevGUI.TextAreaScrollable(tabRect, markerData.narrativeText ?? "", ref narrativeScrollPosition);
        }

        private void DrawBottomButtons(Rect inRect)
        {
            float buttonWidth = 150f;
            float buttonHeight = 32f;
            float buttonY = inRect.yMax - buttonHeight - 10f;

            Rect saveButtonRect = new Rect(inRect.xMax - buttonWidth - 15f, buttonY, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(saveButtonRect, "Save".Translate()))
            {
                var originalData = MarkerDataManager.GetOrCreateData(marker);
                originalData.name = markerData.name;
                originalData.description = markerData.description;
                originalData.narrativeText = markerData.narrativeText;
                originalData.iconDefName = markerData.iconDefName;
                originalData.color = markerData.color;

                Messages.Message("WB_MarkerCustomizeSaveSuccess".Translate(), MessageTypeDefOf.PositiveEvent);
                Find.World.renderer.SetDirty<WorldLayer_WorldObjects>();
                Close();
            }

            Rect cancelButtonRect = new Rect(saveButtonRect.x - buttonWidth - 10f, buttonY, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(cancelButtonRect, "Cancel".Translate()))
            {
                Close();
            }
        }
    }
}