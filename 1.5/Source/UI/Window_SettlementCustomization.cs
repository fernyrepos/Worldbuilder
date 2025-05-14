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

        protected override void DrawAppearanceTab(Rect tabRect)
        {
            float buttonHeight = 32f;
            float tabY = tabRect.y;
            float tabWidth = 200f;
            float buttonGap = 5f;
            Rect factionButtonRect = new Rect(tabRect.x, tabY, tabWidth - 15, buttonHeight);
            if (Widgets.ButtonText(factionButtonRect, "WB_ColonyCustomizeFactionIconsLabel".Translate()))
            {
                showingFactionIcons = true;
            }
            Rect culturalButtonRect = new Rect(tabRect.x, factionButtonRect.yMax + buttonGap, tabWidth - 15, buttonHeight);
            if (Widgets.ButtonText(culturalButtonRect, "WB_ColonyCustomizeCulturalIconsLabel".Translate()))
            {
                showingFactionIcons = false;
            }
            Rect labelRect = new Rect(tabRect.x + tabWidth, tabY, tabRect.width - tabWidth, 24f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(labelRect, "WB_SetFactionIcon".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            float iconGridHeight = tabRect.height - 30f;
            Rect iconGridRect = new Rect(tabRect.x + tabWidth + 5, labelRect.yMax + 5f, tabRect.width - tabWidth, iconGridHeight);

            if (showingFactionIcons)
            {
                DrawFactionIconSelectorGrid(iconGridRect, availableFactionIcons, ref selectedFactionIconDef, ref factionIconScrollPosition);
            }
            else
            {
                DrawCulturalIconSelectorGrid(iconGridRect, availableCulturalIcons, ref selectedCulturalIconDef, ref culturalIconScrollPosition);
            }

            DrawColorSelector(
                culturalButtonRect.x,
                culturalButtonRect.yMax + 15,
                tabWidth - 15,
                selectedColor,
                newColor => selectedColor = newColor
            );
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

                Rect iconRect = new Rect(x + 5, y + 5, IconSize, IconSize);
                Widgets.DrawOptionBackground(iconRect, selectedDef == iconDefs[i]);

                Texture2D iconTex = ContentFinder<Texture2D>.Get(iconDefs[i].iconPath, false);
                if (iconTex != null)
                {
                    Color originalColor = GUI.color;
                    if (selectedColor.HasValue)
                    {
                        GUI.color = selectedColor.Value;
                    }

                    GUI.DrawTexture(iconRect.ContractedBy(4f), iconTex, ScaleMode.ScaleToFit);
                    GUI.color = originalColor;
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

                Rect iconRect = new Rect(x + 5, y + 5, IconSize, IconSize);
                Widgets.DrawOptionBackground(iconRect, selectedDef == iconDefs[i]);

                Texture2D iconTex = ContentFinder<Texture2D>.Get(iconDefs[i].factionIconPath, false);
                if (iconTex != null)
                {
                    Color originalColor = GUI.color;
                    if (selectedColor.HasValue)
                    {
                        GUI.color = selectedColor.Value;
                    }

                    GUI.DrawTexture(iconRect.ContractedBy(4f), iconTex, ScaleMode.ScaleToFit);
                    GUI.color = originalColor;
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
            currentFactionName = Widgets.TextField(factionFieldRect, currentFactionName);

            listing.End();
        }

        protected override void DrawBottomButtons(Rect inRect)
        {
            float buttonWidth = 150f;
            float buttonHeight = 32f;
            float buttonY = inRect.yMax - buttonHeight;

            if (!isPlayerColony)
            {
                float centerX = inRect.x + (inRect.width / 2) - (buttonWidth / 2);
                Rect saveButtonRect = new Rect(centerX, buttonY, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(saveButtonRect, "WB_CustomizeSave".Translate()))
                {
                    SaveChanges();
                }
                Rect worldButtonRect = new Rect(inRect.xMax - buttonWidth - 15f, buttonY, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(worldButtonRect, "WB_CustomizeWorld".Translate()))
                {
                    List<FloatMenuOption> worldOptions = new List<FloatMenuOption>();
                    foreach (WorldPreset preset in WorldPresetManager.GetAllPresets())
                    {
                        WorldPreset localPreset = preset;
                        worldOptions.Add(new FloatMenuOption("WB_CustomizeSaveToPreset".Translate(localPreset.name), () =>
                        {
                            ShowSaveConfirmationDialog(localPreset);
                        }));
                    }
                    worldOptions.Add(new FloatMenuOption("WB_SelectPresetCreateNewButton".Translate(), () =>
                    {
                        Find.WindowStack.Add(new Window_CreateWorld());
                    }));
                    var currentPreset = WorldPresetManager.CurrentlyLoadedPreset;
                    if (currentPreset != null)
                    {
                        worldOptions.Add(new FloatMenuOption("WB_CustomizeSaveToPreset".Translate(currentPreset.name ?? "Unknown"), () =>
                        {
                            SaveAppearanceToWorldPreset();
                        }));
                    }

                    Find.WindowStack.Add(new FloatMenu(worldOptions));
                }
            }
            else
            {
                float centerX = inRect.x + (inRect.width / 2) - (buttonWidth / 2);
                Rect saveButtonRect = new Rect(centerX, buttonY, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(saveButtonRect, "WB_CustomizeSave".Translate()))
                {
                    SaveChanges();
                }
            }
        }

        private void ShowSaveConfirmationDialog(WorldPreset targetPreset)
        {
            if (targetPreset == null)
            {
                Messages.Message("Cannot save to world preset: Invalid preset", MessageTypeDefOf.RejectInput);
                return;
            }

            string presetNameForMessage = targetPreset.name ?? "Unknown";

            if (settlement?.Faction == null)
            {
                Messages.Message("WB_FactionBaseCustomizeNoFactionError".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            string factionLabel = settlement.Faction.def?.label ?? "faction";

            Dialog_MessageBox confirmationDialog = Dialog_MessageBox.CreateConfirmation(
                "WB_CustomizeSaveToPresetConfirm".Translate(factionLabel, presetNameForMessage),
                () =>
                {
                    SaveAppearanceToWorldPreset(targetPreset);
                }
            );
            Find.WindowStack.Add(confirmationDialog);
        }

        private void SaveAppearanceToWorldPreset(WorldPreset preset = null)
        {
            var currentPreset = preset ?? WorldPresetManager.CurrentlyLoadedPreset;
            if (currentPreset == null)
            {
                Messages.Message("Cannot save to world preset: No preset loaded", MessageTypeDefOf.RejectInput);
                return;
            }

            if (settlement?.Faction == null)
            {
                Messages.Message("WB_FactionBaseCustomizeNoFactionError".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            if (currentPreset.factionSettlementCustomizationDefaults == null)
            {
                currentPreset.factionSettlementCustomizationDefaults = new Dictionary<FactionDef, SettlementCustomData>();
            }

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

            if (currentPreset.factionNameOverrides == null)
            {
                currentPreset.factionNameOverrides = new Dictionary<FactionDef, string>();
            }

            if (!string.IsNullOrEmpty(currentFactionName))
            {
                currentPreset.factionNameOverrides[settlement.Faction.def] = currentFactionName;

                if (settlement.Faction.Name != currentFactionName)
                {
                    settlement.Faction.Name = currentFactionName;
                }
            }

            bool savedSuccessfully = WorldPresetManager.SavePreset(currentPreset, null, null);

            if (savedSuccessfully)
            {
                Messages.Message("WB_FactionBaseCustomizePresetSaveSuccess".Translate(settlement.Faction.def.label, currentPreset.name), MessageTypeDefOf.PositiveEvent);
                Close();
            }
            else
            {
                Messages.Message("WB_FactionBaseCustomizePresetSaveFailed".Translate(currentPreset.name), MessageTypeDefOf.NegativeEvent);
            }
            presetDefaultData.ClearIconCache();
            Find.World.renderer.SetDirty<WorldLayer_WorldObjects>();
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
                    if (currentPreset != null && !string.IsNullOrEmpty(currentFactionName))
                    {
                        if (currentPreset.factionNameOverrides == null)
                        {
                            currentPreset.factionNameOverrides = new Dictionary<FactionDef, string>();
                        }
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
            settlementData.ClearIconCache();
            Find.World.renderer.SetDirty<WorldLayer_WorldObjects>();
            Close();
        }
    }
}
