using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public class Window_SettlementCustomization : Window_BaseCustomization
    {
        private Settlement settlement;
        private bool isPlayerColony;
        private string currentSettlementName = "";
        private string currentFactionName = "";
        private string currentDescription = "";
        private Vector2 factionIconScrollPosition = Vector2.zero;
        private Vector2 culturalIconScrollPosition = Vector2.zero;
        private SettlementCustomData settlementCustomData;
        private Color? selectedColor;

        private readonly List<FactionDef> availableFactionIcons;
        private FactionDef selectedFactionIconDef;
        private IdeoIconDef selectedCulturalIconDef;
        private readonly List<IdeoIconDef> availableCulturalIcons;
        private bool showingFactionIcons = true;
        private const float IconSize = 64f;
        private const float IconPadding = 10f;

        public Window_SettlementCustomization(Settlement settlement)
            : base()
        {
            this.settlement = settlement;
            isPlayerColony = settlement.Faction == Faction.OfPlayer;

            currentSettlementName = settlement.Name;
            var individualData = SettlementCustomDataManager.GetData(settlement);
            var presetData = World_ExposeData_Patch.GetPresetSettlementCustomizationData(settlement);
            var effectiveData = individualData ?? presetData;

            settlementCustomData = new SettlementCustomData();
            settlementCustomData.narrativeText = effectiveData?.narrativeText ?? "";
            currentDescription = effectiveData?.description ?? "";
            this.customizationData = new CustomizationData();
            this.customizationData.narrativeText = settlementCustomData.narrativeText;
            if (settlement.Faction != null)
            {
                currentFactionName = settlement.Faction.Name;
            }

            availableFactionIcons = DefDatabase<FactionDef>.AllDefsListForReading
                .Where(f => !f.factionIconPath.NullOrEmpty() && ContentFinder<Texture2D>.Get(f.factionIconPath, false) != null)
                .OrderBy(f => f.defName)
                .ToList();

            availableCulturalIcons = DefDatabase<IdeoIconDef>.AllDefsListForReading
                .OrderBy(i => i.defName)
                .ToList();

            selectedFactionIconDef = effectiveData?.SelectedFactionIconDef ?? settlement.Faction?.def;
            selectedCulturalIconDef = effectiveData?.SelectedCulturalIconDef;
            selectedColor = effectiveData?.color;
        }

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);
        }

        protected override void DrawAppearanceTab(Rect tabRect)
        {
            float buttonHeight = 30f;
            Rect factionButtonRect = new Rect(tabRect.x, tabRect.y, 120f, buttonHeight);
            Widgets.DrawOptionBackground(factionButtonRect, showingFactionIcons);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(factionButtonRect, "WB_ColonyCustomizeFactionIconsLabel".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            if (Widgets.ButtonInvisible(factionButtonRect))
            {
                showingFactionIcons = true;
            }

            Rect culturalButtonRect = new Rect(factionButtonRect.xMax + 10f, tabRect.y, 120f, buttonHeight);
            Widgets.DrawOptionBackground(culturalButtonRect, !showingFactionIcons);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(culturalButtonRect, "WB_ColonyCustomizeCulturalIconsLabel".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            if (Widgets.ButtonInvisible(culturalButtonRect))
            {
                showingFactionIcons = false;
            }
            Rect labelRect = new Rect(tabRect.x, factionButtonRect.yMax + 10f, tabRect.width, 30f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, showingFactionIcons ? "WB_SetFactionIcon".Translate() : "WB_SetIdeologicalIcon".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            DrawColorSelector(
                tabRect.x,
                labelRect.yMax + 10f,
                tabRect.width,
                selectedColor,
                newColor => selectedColor = newColor
            );
            Rect iconGridRect = new Rect(tabRect.x, labelRect.yMax + 10f + 30f + 30f, tabRect.width, tabRect.height - (labelRect.yMax + 10f + 30f) - 70f);

            if (showingFactionIcons)
            {
                DrawFactionIconSelectorGrid(iconGridRect, availableFactionIcons, ref selectedFactionIconDef, ref factionIconScrollPosition);
            }
            else
            {
                DrawCulturalIconSelectorGrid(iconGridRect, availableCulturalIcons, ref selectedCulturalIconDef, ref culturalIconScrollPosition);
            }
        }

        private void DrawCulturalIconSelectorGrid(Rect rect, List<IdeoIconDef> iconDefs, ref IdeoIconDef selectedDef, ref Vector2 scrollPos)
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

                Texture2D iconTex = ContentFinder<Texture2D>.Get(iconDefs[i].iconPath, false);
                if (iconTex != null)
                {
                    GUI.DrawTexture(iconRect.ContractedBy(4f), iconTex, ScaleMode.ScaleToFit);
                }

                if (Widgets.ButtonInvisible(iconRect))
                {
                    selectedDef = iconDefs[i];
                }
                TooltipHandler.TipRegion(iconRect, iconDefs[i].defName);
            }

            Widgets.EndScrollView();
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



        protected override void DrawDetailTab(Rect tabRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(tabRect);
            Text.Font = GameFont.Small;
            Rect settlementLabelRect = listing.GetRect(24f);
            Widgets.Label(settlementLabelRect, "WB_ColonyCustomizeNameLabel".Translate());

            Rect settlementFieldRect = listing.GetRect(30f);
            currentSettlementName = Widgets.TextField(settlementFieldRect, currentSettlementName);

            listing.Gap(12f);
            Rect factionLabelRect = listing.GetRect(24f);
            Widgets.Label(factionLabelRect, "WB_FactionNameLabel".Translate());

            Rect factionFieldRect = listing.GetRect(30f);
            GUI.enabled = !isPlayerColony;
            currentFactionName = Widgets.TextField(factionFieldRect, currentFactionName);
            GUI.enabled = true;

            listing.End();
        }

        protected override void DrawBottomButtons(Rect inRect)
        {
            float buttonWidth = 150f;
            float buttonHeight = 32f;
            float buttonY = inRect.yMax - buttonHeight - 15f;

            if (!isPlayerColony)
            {
                Rect saveToWorldButtonRect = new Rect(inRect.xMax - (buttonWidth * 2) - 25f, buttonY, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(saveToWorldButtonRect, "WB_FactionBaseCustomizeSaveToWorld".Translate()))
                {
                    SaveAppearanceToWorldPreset();
                }

                Rect saveButtonRect = new Rect(inRect.xMax - buttonWidth - 15f, buttonY, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(saveButtonRect, "Save".Translate()))
                {
                    SaveChanges();
                }
            }
            else
            {
                Rect saveButtonRect = new Rect(inRect.xMax - buttonWidth - 15f, buttonY, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(saveButtonRect, "Save".Translate()))
                {
                    SaveChanges();
                }
            }
        }

        private void SaveAppearanceToWorldPreset()
        {
            var currentPreset = WorldPresetManager.CurrentlyLoadedPreset;
            if (settlement.Faction == null)
            {
                Messages.Message("WB_FactionBaseCustomizeNoFactionError".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            currentPreset.factionSettlementCustomizationDefaults ??= new Dictionary<FactionDef, SettlementCustomData>();

            if (!currentPreset.factionSettlementCustomizationDefaults.TryGetValue(settlement.Faction.def, out var presetDefaultData))
            {
                presetDefaultData = new SettlementCustomData();
                currentPreset.factionSettlementCustomizationDefaults[settlement.Faction.def] = presetDefaultData;
            }

            presetDefaultData.description = currentDescription;
            presetDefaultData.narrativeText = customizationData.narrativeText;
            presetDefaultData.selectedFactionIconDefName = selectedFactionIconDef?.defName;
            presetDefaultData.selectedCulturalIconDefName = selectedCulturalIconDef?.defName;
            presetDefaultData.color = selectedColor;
            currentPreset.factionNameOverrides ??= new Dictionary<FactionDef, string>();
            currentPreset.factionNameOverrides[settlement.Faction.def] = currentFactionName;
            if (settlement.Faction != null && settlement.Faction.Name != currentFactionName)
            {
                settlement.Faction.Name = currentFactionName;
            }

            Messages.Message("WB_FactionBaseCustomizeSaveToWorldSuccess".Translate(), MessageTypeDefOf.PositiveEvent);
            Close();
        }

        protected override void SaveChanges()
        {
            if (string.IsNullOrWhiteSpace(currentSettlementName))
            {
                Messages.Message("WB_ColonyCustomizeNameEmptyError".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            if (settlement.Name != currentSettlementName)
            {
                settlement.Name = currentSettlementName;
            }
            if (settlement.Faction != null && settlement.Faction.Name != currentFactionName)
            {
                settlement.Faction.Name = currentFactionName;
                if (isPlayerColony)
                {
                    World_ExposeData_Patch.playerFactionName = currentFactionName;
                }
                if (!isPlayerColony)
                {
                    var currentPreset = WorldPresetManager.CurrentlyLoadedPreset;
                    if (currentPreset != null)
                    {
                        currentPreset.factionNameOverrides ??= new Dictionary<FactionDef, string>();
                        currentPreset.factionNameOverrides[settlement.Faction.def] = currentFactionName;
                    }
                }
            }

            var settlementData = SettlementCustomDataManager.GetOrCreateData(settlement);
            settlementData.description = currentDescription;
            settlementData.narrativeText = customizationData.narrativeText;
            settlementData.selectedFactionIconDefName = selectedFactionIconDef?.defName;
            settlementData.selectedCulturalIconDefName = selectedCulturalIconDef?.defName;
            settlementData.color = selectedColor;

            if (isPlayerColony)
            {
                var playerSettlementData = new SettlementCustomData
                {
                    description = currentDescription,
                    narrativeText = customizationData.narrativeText,
                    selectedFactionIconDefName = selectedFactionIconDef?.defName,
                    selectedCulturalIconDefName = selectedCulturalIconDef?.defName,
                    color = selectedColor
                };

                CustomizationDataCollections.settlementCustomizationData[settlement] = playerSettlementData;
                Messages.Message("WB_ColonyCustomizeSaveSuccess".Translate(), MessageTypeDefOf.PositiveEvent);
            }
            else
            {
                Messages.Message("WB_FactionBaseCustomizeIndividualSaveSuccess".Translate(), MessageTypeDefOf.PositiveEvent);
            }

            Find.World.renderer.SetDirty<WorldLayer_WorldObjects>();
            Close();
        }
    }
}
