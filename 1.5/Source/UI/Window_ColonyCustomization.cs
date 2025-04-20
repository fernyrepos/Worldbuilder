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
    public class Window_ColonyCustomization : Window
    {
        private Settlement settlement;
        private int currentTab = 0;
        private string currentSettlementName = "";
        private string currentNarrative = "";
        private Vector2 narrativeScrollPosition = Vector2.zero;
        private Vector2 factionIconScrollPosition = Vector2.zero;
        private Vector2 culturalIconScrollPosition = Vector2.zero;

        private List<FactionDef> availableFactionIcons;
        private FactionDef selectedFactionIconDef;

        private List<IdeoIconDef> availableCulturalIcons;
        private IdeoIconDef selectedCulturalIconDef;
        private const float IconSize = 64f;
        private const float IconPadding = 10f;
        private const float SectionLabelHeight = 25f;

        public override Vector2 InitialSize => new Vector2(800, 700);

        public Window_ColonyCustomization(Settlement settlement)
        {
            this.settlement = settlement;
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.preventCameraMotion = false;
            this.closeOnAccept = false;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;

            currentSettlementName = settlement.Name;
            var individualData = SettlementCustomDataManager.GetData(settlement);
            var presetData = World_ExposeData_Patch.GetPresetSettlementCustomizationData(settlement);
            var effectiveData = individualData ?? presetData;

            currentNarrative = effectiveData?.narrativeText ?? "";

            availableFactionIcons = DefDatabase<FactionDef>.AllDefsListForReading
                .Where(f => !f.factionIconPath.NullOrEmpty() && ContentFinder<Texture2D>.Get(f.factionIconPath, false) != null)
                .OrderBy(f => f.LabelCap)
                .ToList();
            availableCulturalIcons = DefDatabase<IdeoIconDef>.AllDefsListForReading
                .OrderBy(i => i.label)
                .ToList();
            selectedFactionIconDef = effectiveData?.SelectedFactionIconDef ?? settlement.Faction?.def;
            selectedCulturalIconDef = effectiveData?.SelectedCulturalIconDef;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            var titleRect = new Rect(inRect.x, inRect.y, inRect.width, 32f);
            Widgets.Label(titleRect, "WB_GizmoCustomizeColonyLabel".Translate());
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
            float halfWidth = (tabRect.width - IconPadding) / 2f;
            float gridHeight = tabRect.height - 40f;
            Rect factionSectionRect = new Rect(tabRect.x, tabRect.y, halfWidth, gridHeight);
            Rect factionLabelRect = new Rect(factionSectionRect.x, factionSectionRect.y, factionSectionRect.width, SectionLabelHeight);
            Widgets.Label(factionLabelRect, "WB_ColonyCustomizeFactionIconsLabel".Translate());
            Rect factionGridRect = new Rect(factionSectionRect.x, factionLabelRect.yMax, factionSectionRect.width, factionSectionRect.height - SectionLabelHeight);
            DrawFactionIconSelectorGrid(factionGridRect, availableFactionIcons, ref selectedFactionIconDef, ref factionIconScrollPosition);
            Rect culturalSectionRect = new Rect(factionSectionRect.xMax + IconPadding, tabRect.y, halfWidth, gridHeight);
            Rect culturalLabelRect = new Rect(culturalSectionRect.x, culturalSectionRect.y, culturalSectionRect.width, SectionLabelHeight);
            Widgets.Label(culturalLabelRect, "WB_ColonyCustomizeCulturalIconsLabel".Translate());
            Rect culturalGridRect = new Rect(culturalSectionRect.x, culturalLabelRect.yMax, culturalSectionRect.width, culturalSectionRect.height - SectionLabelHeight);
            DrawIdeoIconSelectorGrid(culturalGridRect, availableCulturalIcons, ref selectedCulturalIconDef, ref culturalIconScrollPosition);


            Rect setIconButtonRect = new Rect(tabRect.x, tabRect.yMax - 32f, 150f, 32f);
            if (Widgets.ButtonText(setIconButtonRect, "WB_ColonyCustomizeSetIconsButton".Translate()))
            {
                Find.World.renderer.SetDirty<WorldLayer_WorldObjects>();
            }
        }
        private void DrawFactionIconSelectorGrid(Rect rect, List<FactionDef> iconDefs, ref FactionDef selectedDef, ref Vector2 scrollPos)
        {
            int iconsPerRow = Mathf.FloorToInt(rect.width / (IconSize + IconPadding));
            if (iconsPerRow < 1) iconsPerRow = 1;

            float totalGridHeight = Mathf.Ceil((float)iconDefs.Count / iconsPerRow) * (IconSize + IconPadding);
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, totalGridHeight);

            Widgets.BeginScrollView(rect, ref scrollPos, viewRect);

            for (int i = 0; i < iconDefs.Count; i++)
            {
                int row = i / iconsPerRow;
                int col = i % iconsPerRow;

                float x = col * (IconSize + IconPadding);
                float y = row * (IconSize + IconPadding);

                Rect iconRect = new Rect(x, y, IconSize, IconSize);
                Widgets.DrawOptionBackground(iconRect, selectedDef == iconDefs[i]);

                Texture2D iconTex = ContentFinder<Texture2D>.Get(iconDefs[i].factionIconPath, false);
                if (iconTex != null)
                {
                    GUI.DrawTexture(iconRect.ContractedBy(4f), iconTex, ScaleMode.ScaleToFit);
                }

                if (Widgets.ButtonInvisible(iconRect))
                {
                    selectedDef = iconDefs[i];
                }
                TooltipHandler.TipRegion(iconRect, iconDefs[i].LabelCap);
            }

            Widgets.EndScrollView();
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
                int row = i / iconsPerRow;
                int col = i % iconsPerRow;

                float x = col * (IconSize + IconPadding);
                float y = row * (IconSize + IconPadding);

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
                }
                TooltipHandler.TipRegion(iconRect, iconDefs[i].LabelCap);
            }

            Widgets.EndScrollView();
        }


        private void DrawDetailTab(Rect tabRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(tabRect);

            listing.Label("WB_ColonyCustomizeNameLabel".Translate());
            currentSettlementName = listing.TextEntry(currentSettlementName);

            listing.End();
        }

        private void DrawNarrativeTab(Rect tabRect)
        {
            currentNarrative = DevGUI.TextAreaScrollable(tabRect, currentNarrative, ref narrativeScrollPosition);
        }

        private void DrawBottomButtons(Rect inRect)
        {
            float buttonWidth = 150f;
            float buttonHeight = 32f;
            float buttonY = inRect.yMax - buttonHeight - 10f;

            Rect saveButtonRect = new Rect(inRect.xMax - buttonWidth - 15f, buttonY, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(saveButtonRect, "Save".Translate()))
            {
                if (string.IsNullOrWhiteSpace(currentSettlementName))
                {
                    Messages.Message("WB_ColonyCustomizeNameEmptyError".Translate(), MessageTypeDefOf.RejectInput);
                }
                else
                {
                    settlement.Name = currentSettlementName;
                    var customData = SettlementCustomDataManager.GetOrCreateData(settlement);
                    customData.narrativeText = currentNarrative;
                    customData.selectedFactionIconDefName = selectedFactionIconDef?.defName;
                    customData.selectedCulturalIconDefName = selectedCulturalIconDef?.defName;

                    Messages.Message("WB_ColonyCustomizeSaveSuccess".Translate(), MessageTypeDefOf.PositiveEvent);
                    Close();
                }
            }

            Rect cancelButtonRect = new Rect(saveButtonRect.x - buttonWidth - 10f, buttonY, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(cancelButtonRect, "Cancel".Translate()))
            {
                Close();
            }
        }
    }
}
