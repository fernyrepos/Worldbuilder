using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Worldbuilder
{
    [HotSwappable]
    public class Window_WorldSettings : Window
    {
        private WorldPreset preset;
        private static WorldSettingsTab curTab = WorldSettingsTab.Data;
        private List<TabRecord> tabs = new List<TabRecord>();

        public override Vector2 InitialSize => new Vector2(650f, 550f);
        public Scenario scenario;
        public Window_WorldSettings(WorldPreset presetToConfigure)
        {
            forcePause = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            draggable = true;
            preset = presetToConfigure;
            scenario = CopyForEditing(Find.Scenario);
            if (preset.scenParts != null)
            {
                scenario.parts = new List<ScenPart>();
                foreach (var scenPart in preset.scenParts)
                {
                    scenario.parts.Add(scenPart.CopyForEditing());
                }
            }
        }

        public Scenario CopyForEditing(Scenario scenarioToCopy)
        {
            var scenario = new Scenario();
            scenario.name = scenarioToCopy.name;
            scenario.summary = scenarioToCopy.summary;
            scenario.description = scenarioToCopy.description;
            scenario.playerFaction = (ScenPart_PlayerFaction)scenarioToCopy.playerFaction.CopyForEditing();
            scenario.surfaceLayer = (ScenPart_PlanetLayer)scenarioToCopy.surfaceLayer.CopyForEditing();
            scenario.categoryInt = ScenarioCategory.CustomLocal;
            return scenario;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 30f), "WB_WorldSettingsTitle".Translate());
            Text.Font = GameFont.Small;

            tabs.Clear();
            tabs.Add(new TabRecord("WB_WorldSettingsDataTab".Translate(), () => curTab = WorldSettingsTab.Data, curTab == WorldSettingsTab.Data));
            tabs.Add(new TabRecord("WB_WorldSettingsModifiersTab".Translate(), () => curTab = WorldSettingsTab.Modifiers, curTab == WorldSettingsTab.Modifiers));

            var tabRect = new Rect(inRect.x, inRect.y + 60f, inRect.width, inRect.height - 100f);
            TabDrawer.DrawTabs(tabRect, tabs);

            Rect contentRect = tabRect;
            switch (curTab)
            {
                case WorldSettingsTab.Data:
                    DrawDataTab(contentRect);
                    break;
                case WorldSettingsTab.Modifiers:
                    DrawModifiersTab(contentRect);
                    break;
            }

            float buttonWidth = 120f;
            float buttonHeight = 35f;
            var closeButtonRect = new Rect(inRect.width / 2f - buttonWidth / 2f, inRect.height - buttonHeight, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(closeButtonRect, "Close".Translate()))
            {
                this.Close();
            }
        }

        private void DrawDataTab(Rect rect)
        {
            var listing = new Listing_Standard();
            listing.Begin(rect);

            Text.Font = GameFont.Medium;
            listing.Label("WB_WorldSettingsIncludedData".Translate());
            Text.Font = GameFont.Small;
            listing.Gap(6f);

            var prevSaveFactions = preset.saveFactions;
            var prevSaveBases = preset.saveBases;

            listing.CheckboxLabeled("WB_WorldSettingsSaveFactions".Translate(), ref preset.saveFactions, "WB_WorldSettingsSaveFactionsTooltip".Translate());
            listing.CheckboxLabeled("WB_WorldSettingsSaveIdeologies".Translate(), ref preset.saveIdeologies, "WB_WorldSettingsSaveIdeologiesTooltip".Translate());
            listing.CheckboxLabeled("WB_WorldSettingsSaveBases".Translate(), ref preset.saveBases, "WB_WorldSettingsSaveBasesTooltip".Translate());
            listing.Gap(10f);
            listing.CheckboxLabeled("WB_WorldSettingsSaveTerrain".Translate(), ref preset.saveTerrain, "WB_WorldSettingsSaveTerrainTooltip".Translate());
            listing.CheckboxLabeled("WB_WorldSettingsSaveMapMarkers".Translate(), ref preset.saveMapMarkers, "WB_WorldSettingsSaveMapMarkersTooltip".Translate());
            listing.CheckboxLabeled("WB_WorldSettingsSaveMapText".Translate(), ref preset.saveWorldFeatures, "WB_WorldSettingsSaveMapTextTooltip".Translate());
            listing.Gap(10f);
            listing.CheckboxLabeled("WB_WorldSettingsSaveStorykeeperEntries".Translate(), ref preset.saveStorykeeperEntries, "WB_WorldSettingsSaveStorykeeperEntriesTooltip".Translate());
            listing.CheckboxLabeled("WB_WorldSettingsSaveWorldTechLevel".Translate(), ref preset.saveWorldTechLevel, "WB_WorldSettingsSaveWorldTechLevelTooltip".Translate());

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
        }

        private Vector2 modifiersScrollPosition;
        private float modifiersViewHeight;

        private void DrawModifiersTab(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            if (preset == null)
            {
                return;
            }
            var headerRect = new Rect(rect.x, rect.y + 4f, rect.width, 30f);
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;
            var labelRect = new Rect(headerRect.x + 10f, headerRect.y, headerRect.width - 40f, headerRect.height);
            Widgets.Label(labelRect, "WB_WorldModifiers".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
            var addButtonRect = new Rect(headerRect.xMax - 35f, headerRect.y, 30f, 30f);
            if (Widgets.ButtonImage(addButtonRect, TexButton.Plus))
            {
                OpenAddScenPartMenu();
            }
            float scrollViewY = headerRect.yMax + 4f;
            var scrollViewContainer = new Rect(rect.x, scrollViewY, rect.width, rect.height - (scrollViewY - rect.y));
            var viewRect = new Rect(0f, 0f, scrollViewContainer.width - 16f, modifiersViewHeight);

            Widgets.BeginScrollView(scrollViewContainer, ref modifiersScrollPosition, viewRect);
            var contentRect = new Rect(0f, 0f, viewRect.width, 99999f);

            var listing_ScenEdit = new Listing_ScenEdit(scenario);
            listing_ScenEdit.ColumnWidth = contentRect.width;
            listing_ScenEdit.Begin(contentRect);

            if (scenario.parts != null)
            {
                foreach (var allPart in scenario.parts)
                {
                    allPart.DoEditInterface(listing_ScenEdit);
                }
                preset.scenParts = new List<ScenPart>(scenario.parts);
                preset.scenPartDefs = scenario.parts.Select(p => p.def.defName).Distinct().ToList();
            }
            listing_ScenEdit.End();
            modifiersViewHeight = listing_ScenEdit.CurHeight;

            Widgets.EndScrollView();
        }

        private void OpenAddScenPartMenu()
        {
            var floatMenuOptions = new List<FloatMenuOption>();
            foreach (var scenPartDef in ScenarioMaker.AddableParts(scenario).Where(p => p.PlayerAddRemovable).OrderBy(p => p.label))
            {
                var localDef = scenPartDef;
                floatMenuOptions.Add(new FloatMenuOption(localDef.LabelCap, () => AddScenPart(localDef)));
            }
            Find.WindowStack.Add(new FloatMenu(floatMenuOptions));
        }

        private void AddScenPart(ScenPartDef def)
        {
            var scenPart = ScenarioMaker.MakeScenPart(def);
            scenPart.Randomize();
            scenario.parts.Add(scenPart);
        }
    }

    public enum WorldSettingsTab
    {
        Data,
        Modifiers
    }
}
