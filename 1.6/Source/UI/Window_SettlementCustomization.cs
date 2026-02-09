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
            var data = settlement.GetCustomizationData();
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

            float centerX = inRect.x + (inRect.width / 2) - (buttonWidth / 2);
            Rect saveButtonRect = new Rect(centerX, buttonY, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(saveButtonRect, "WB_CustomizeSave".Translate()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("WB_CustomizeSaveToThisSettlement".Translate(), () => SaveIndividualChanges()));
                if (!isPlayerColony)
                {
                    options.Add(new FloatMenuOption("WB_CustomizeSaveToFaction".Translate(), () => SaveToFaction()));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            Rect deleteButtonRect = new Rect(inRect.x, buttonY, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(deleteButtonRect, "Delete".Translate()))
            {
                DeleteSettlement();
            }

            Rect relocateButtonRect = new Rect(inRect.xMax - buttonWidth, buttonY, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(relocateButtonRect, "WB_Relocate".Translate()))
            {
                RelocateSettlement();
            }
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

            var data = settlement.GetCustomizationData();
            if (data == null)
            {
                data = new SettlementCustomData();
                CustomizationDataCollections.settlementCustomizationData[settlement] = data;
            }
            data.description = currentDescription;
            data.narrativeText = customizationData.narrativeText;
            data.factionIconDef = selectedFactionIconDef;
            data.iconDef = selectedCulturalIconDef;
            data.color = selectedColor;

            if (!isPlayerColony)
            {
                if (!string.IsNullOrEmpty(currentFactionDescription))
                {
                    World_ExposeData_Patch.factionDescriptionsById[settlement.Faction.loadID] = currentFactionDescription;
                }
                else
                {
                    World_ExposeData_Patch.factionDescriptionsById.Remove(settlement.Faction.loadID);
                }
                if (!string.IsNullOrEmpty(currentFactionName))
                {
                    World_ExposeData_Patch.factionNamesById[settlement.Faction.loadID] = currentFactionName;
                }
                else
                {
                    World_ExposeData_Patch.factionNamesById.Remove(settlement.Faction.loadID);
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
            data.ClearIconCache();
            Find.World.renderer.SetDirty<WorldDrawLayer_WorldObjects>(settlement.Tile.Layer);
            Close();
        }

        private void SaveToFaction()
        {
            FactionDef targetFactionDef = settlement.Faction.def;
            if (selectedFactionIconDef != null)
            {
                World_ExposeData_Patch.individualFactionIcons[targetFactionDef] = selectedFactionIconDef.factionIconPath;
                World_ExposeData_Patch.individualFactionIdeoIcons.Remove(targetFactionDef);
                Utils.ShowFactionIconSharedWarning(targetFactionDef);
            }
            else if (selectedCulturalIconDef != null)
            {
                World_ExposeData_Patch.individualFactionIdeoIcons[targetFactionDef] = selectedCulturalIconDef;
                World_ExposeData_Patch.individualFactionIcons.Remove(targetFactionDef);
                Utils.ShowFactionIconSharedWarning(targetFactionDef);
            }
            if (selectedColor.HasValue)
            {
                settlement.Faction.color = selectedColor.Value;
            }

            foreach (var s in Utils.GetSurfaceWorldObjects<Settlement>())
            {
                if (s.Faction?.def == targetFactionDef)
                {
                    var data = s.GetCustomizationData();
                    if (data == null)
                    {
                        data = new SettlementCustomData();
                        CustomizationDataCollections.settlementCustomizationData[s] = data;
                    }
                    data.description = currentDescription;
                    data.narrativeText = customizationData.narrativeText;
                    data.factionIconDef = selectedFactionIconDef;
                    data.iconDef = selectedCulturalIconDef;
                    data.color = selectedColor;
                    data.ClearIconCache();
                    Find.World.renderer.SetDirty<WorldDrawLayer_WorldObjects>(s.Tile.Layer);
                }
            }
            if (settlement.Faction.Name != currentFactionName)
            {
                settlement.Faction.Name = currentFactionName;
            }
            World_ExposeData_Patch.factionDescriptionsById[settlement.Faction.loadID] = currentFactionDescription;
            World_ExposeData_Patch.factionNamesById[settlement.Faction.loadID] = currentFactionName;

            Messages.Message("WB_FactionBaseCustomizeAllSaveSuccess".Translate(targetFactionDef.label), MessageTypeDefOf.PositiveEvent);
            Close();
        }

        private void RelocateSettlement()
        {
            Close(true);
            Find.WorldTargeter.BeginTargeting(
                (GlobalTargetInfo target) =>
                {
                    settlement.Tile = target.Tile;
                    Messages.Message("WB_SettlementRelocated".Translate(settlement.Label), MessageTypeDefOf.NeutralEvent);
                    settlement.drawPosCacheTick = -1;
                    return true;
                }, true, null, false, null,
                (GlobalTargetInfo target) =>
                {
                    return CanRelocateTo(target);
                }, canSelectTarget: (GlobalTargetInfo target) => CanRelocateTo(target) == null
            );
        }

        private TaggedString CanRelocateTo(GlobalTargetInfo target)
        {
            if (Find.World.Impassable(target.Tile) || target.Tile.Tile.biome.impassable || target.Tile.Tile.hilliness == Hilliness.Impassable)
            {
                return "Impassable".Translate();
            }
            if (Find.WorldObjects.AnyMapParentAt(target.Tile))
            {
                return "WB_TileOccupied".Translate();
            }
            return null;
        }

        private void DeleteSettlement()
        {
            CustomizationDataCollections.settlementCustomizationData.Remove(settlement);
            Find.WorldObjects.Remove(settlement);
            Messages.Message(settlement.Label + " deleted.", MessageTypeDefOf.NeutralEvent);
            Close();
        }
    }
}
