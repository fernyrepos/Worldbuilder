using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;


namespace Worldbuilder
{
    [HotSwappable]
    public class Window_SettlementCustomization : Window_WorldObjectCustomization
    {
        private Settlement settlement;
        private bool isPlayerColony;
        private string currentSettlementName = "";
        private string currentFactionName = "";
        private string currentFactionDescription = "";
        private string currentDescription = "";
        private SettlementCustomData settlementCustomData;

        public Window_SettlementCustomization(Settlement settlement)
            : base()
        {
            this.settlement = settlement;
            isPlayerColony = settlement.Faction == Faction.OfPlayer;
            currentSettlementName = settlement.Name;
            var data = SettlementCustomDataManager.GetData(settlement);
            settlementCustomData = new SettlementCustomData();
            settlementCustomData.narrativeText = data?.narrativeText ?? "";
            currentDescription = data?.description ?? settlement.def.description;
            currentFactionDescription = settlement.Faction?.def.GetPresetDescription();
            this.customizationData = new CustomizationData();
            this.customizationData.narrativeText = settlementCustomData.narrativeText;
            if (settlement.Faction != null)
            {
                currentFactionName = settlement.Faction.Name;
            }
            selectedFactionIconDef = data?.factionIconDef ?? settlement.Faction?.def;
            selectedCulturalIconDef = data?.iconDef;
            selectedColor = data?.color;
        }


        protected override void DrawDetailTab(Rect tabRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(tabRect);
            Text.Font = GameFont.Small;
            Rect factionLabelRect = listing.GetRect(24f);
            Widgets.Label(factionLabelRect, "WB_FactionNameLabel".Translate());

            Rect factionFieldRect = listing.GetRect(30f);
            currentFactionName = Widgets.TextField(factionFieldRect, currentFactionName);

            listing.Gap(12f);
            Rect factionDescriptionLabelRect = listing.GetRect(24f);
            Widgets.Label(factionDescriptionLabelRect, "WB_DescriptionLabel".Translate());

            Rect factionDescriptionFieldRect = listing.GetRect(100f);
            currentFactionDescription = Widgets.TextArea(factionDescriptionFieldRect, currentFactionDescription);

            listing.Gap(30f);
            Rect settlementLabelRect = listing.GetRect(24f);
            Widgets.Label(settlementLabelRect, "WB_ColonyCustomizeNameLabel".Translate());

            Rect settlementFieldRect = listing.GetRect(30f);
            currentSettlementName = Widgets.TextField(settlementFieldRect, currentSettlementName);

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

            if (!isPlayerColony)
            {
                float centerX = inRect.x + (inRect.width / 2) - (buttonWidth / 2);
                Rect saveButtonRect = new Rect(centerX, buttonY, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(saveButtonRect, "WB_CustomizeSave".Translate()))
                {
                    SaveIndividualChanges();
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
                    Find.WindowStack.Add(new FloatMenu(worldOptions));
                }
            }
            else
            {
                float centerX = inRect.x + (inRect.width / 2) - (buttonWidth / 2);
                Rect saveButtonRect = new Rect(centerX, buttonY, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(saveButtonRect, "WB_CustomizeSave".Translate()))
                {
                    SaveIndividualChanges();
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
            presetDefaultData.factionIconDef = selectedFactionIconDef;
            presetDefaultData.iconDef = selectedCulturalIconDef;
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

            if (currentPreset.factionDescriptionOverrides == null)
            {
                currentPreset.factionDescriptionOverrides = new Dictionary<FactionDef, string>();
            }

            if (!string.IsNullOrEmpty(currentFactionDescription))
            {
                currentPreset.factionDescriptionOverrides[settlement.Faction.def] = currentFactionDescription;
            }
            else
            {
                currentPreset.factionDescriptionOverrides.Remove(settlement.Faction.def);
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
            Find.World.renderer.SetDirty<WorldDrawLayer_WorldObjects>(settlement.Tile.Layer);
        }

        protected override void SaveIndividualChanges()
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
            }

            var settlementData = SettlementCustomDataManager.GetOrCreateData(settlement);
            settlementData.description = currentDescription;
            settlementData.narrativeText = customizationData.narrativeText;
            settlementData.factionIconDef = selectedFactionIconDef;
            settlementData.iconDef = selectedCulturalIconDef;
            settlementData.color = selectedColor;

            if (!isPlayerColony)
            {
                if (!string.IsNullOrEmpty(currentFactionDescription))
                {
                    World_ExposeData_Patch.individualFactionDescriptions[settlement.Faction.def] = currentFactionDescription;
                }
                else
                {
                    World_ExposeData_Patch.individualFactionDescriptions.Remove(settlement.Faction.def);
                }
                if (!string.IsNullOrEmpty(currentFactionName))
                {
                    World_ExposeData_Patch.individualFactionNames[settlement.Faction.def] = currentFactionName;
                }
                else
                {
                    World_ExposeData_Patch.individualFactionNames.Remove(settlement.Faction.def);
                }

                var currentPreset = WorldPresetManager.CurrentlyLoadedPreset;
                if (currentPreset != null)
                {
                    if (World_ExposeData_Patch.individualFactionNames.ContainsKey(settlement.Faction.def))
                    {
                        if (currentPreset.factionNameOverrides != null && currentPreset.factionNameOverrides.ContainsKey(settlement.Faction.def))
                        {
                            currentPreset.factionNameOverrides.Remove(settlement.Faction.def);
                        }
                    }

                    // Remove preset override if individual description is set
                    if (World_ExposeData_Patch.individualFactionDescriptions.ContainsKey(settlement.Faction.def))
                    {
                        if (currentPreset.factionDescriptionOverrides != null && currentPreset.factionDescriptionOverrides.ContainsKey(settlement.Faction.def))
                        {
                            currentPreset.factionDescriptionOverrides.Remove(settlement.Faction.def);
                        }
                    }
                }
            }

            if (isPlayerColony)
            {
                var playerSettlementData = new SettlementCustomData
                {
                    description = currentDescription,
                    narrativeText = customizationData.narrativeText,
                    factionIconDef = selectedFactionIconDef,
                    iconDef = selectedCulturalIconDef,
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
            Find.World.renderer.SetDirty<WorldDrawLayer_WorldObjects>(settlement.Tile.Layer);
            Close();
        }
    }
}
