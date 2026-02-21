using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Noise;
using Verse.Profile;
using Verse.Sound;

namespace Worldbuilder
{
    [HotSwappable]
    [HarmonyPatch(typeof(Page_CreateWorldParams), nameof(Page_CreateWorldParams.DoWindowContents))]
    [StaticConstructorOnStartup]
    public static class Page_CreateWorldParams_DoWindowContents_Patch
    {
        private const int WorldCameraHeight = 315;
        private const int WorldCameraWidth = 315;
        private const int MinScale = 5;
        private const int MaxScale = 11;
        private static Vector2 scrollPosition;
        public static bool dirty;
        public static string curPlanetName = "";
        private static Texture2D worldPreview;
        public static World threadedWorld;
        public static Thread thread;
        private static float texSpinAngle;
        public static bool startFresh;
        public static volatile bool shouldCancelGeneration;
        private static volatile int previewStepsDone;
        private static volatile int previewStepsTotal;
        private static HashSet<WorldGenStepDef> _worldGenStepDefs;
        private static HashSet<WorldGenStepDef> worldGenStepDefs
        {
            get
            {
                if (_worldGenStepDefs == null)
                {
                    _worldGenStepDefs = new HashSet<WorldGenStepDef>
                    {
                        DefsOf.Tiles,
                        DefsOf.Terrain,
                        DefsOf.Lakes,
                        DefsOf.Rivers,
                        DefsOf.AncientSites,
                        DefsOf.AncientRoads,
                        DefsOf.Roads
                    };
                }
                return _worldGenStepDefs;
            }
        }

        public static bool generatingWorld;
        private static List<GameSetupStepDef> cachedGenSteps;
        private static int updatePreviewCounter = -1;
        private static WorldGenerationData lastGenerationData;
        private static int lastSubdivisions = -1;
        private static List<GameSetupStepDef> GameSetupStepsInOrder => cachedGenSteps ?? (cachedGenSteps =
            (from x in DefDatabase<GameSetupStepDef>.AllDefs
             orderby x.order, x.index
             select x).ToList());

        private static Page_CreateWorldParams trackedInstance;
        private static string initialSeedForInstance;
        private static bool warningShownForInstance;
        private static int currentTab = 0;

        public static bool Prefix(Page_CreateWorldParams __instance, Rect rect)
        {
            if (!WorldbuilderMod.settings.enablePlanetGenOverhaul)
            {
                return true;
            }

            if (__instance != trackedInstance)
            {
                trackedInstance = __instance;
                warningShownForInstance = false;
                initialSeedForInstance = null;
                var preset = WorldPresetManager.CurrentlyLoadedPreset;
                if (preset != null && preset.saveTerrain && !string.IsNullOrEmpty(preset.worldInfo?.seedString))
                {
                    initialSeedForInstance = __instance.seedString;
                }
            }

            if (World_ExposeData_Patch.worldGenerationData == null)
            {
                var preset = WorldPresetManager.CurrentlyLoadedPreset;
                if (preset != null && preset.generationData != null)
                {
                    World_ExposeData_Patch.worldGenerationData = preset.generationData.MakeCopy();
                }
                else
                {
                    World_ExposeData_Patch.worldGenerationData = new WorldGenerationData();
                    World_ExposeData_Patch.worldGenerationData.Init();
                }
            }

            if (initialSeedForInstance != null && !warningShownForInstance)
            {
                if (__instance.seedString != initialSeedForInstance)
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                        "WB_CreateWorldParamsConfirmUnlockSeedMessage".Translate(), () => { }, destructive: true,
                        title: "WB_CreateWorldParamsConfirmUnlockSeedTitle".Translate()));
                    warningShownForInstance = true;
                }
            }

            if (startFresh)
            {
                startFresh = false;
                dirty = true;
            }

            const float padding = 17f;
            float leftColWidth = rect.width * 0.25f;
            float centerColWidth = rect.width * 0.5f - padding * 2;
            float rightColWidth = rect.width * 0.25f;
            Rect leftColRect = new Rect(rect.x, rect.y, leftColWidth, rect.height);
            Rect centerColRect = new Rect(leftColRect.xMax + padding, rect.y, centerColWidth, rect.height);
            Rect rightColRect = new Rect(centerColRect.xMax + padding, rect.y, rightColWidth, rect.height);
            DrawLeftColumn(__instance, leftColRect);
            DrawCenterColumn(__instance, centerColRect);
            DrawRightColumn(__instance, rightColRect);
            HandlePreviewAutoGeneration(__instance);
            return false;
        }

        private static void DrawLeftColumn(Page_CreateWorldParams __instance, Rect rect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 30f), "WB_PlanetEditor".Translate());
            Text.Font = GameFont.Small;

            float curY = rect.y + 40f;
            float buttonWidth = rect.width;
            float buttonHeight = 35f;

            if (Widgets.ButtonText(new Rect(rect.x, curY, buttonWidth, buttonHeight), "WB_BiomesTab".Translate())) currentTab = 0;
            curY += buttonHeight + 5f;

            if (Widgets.ButtonText(new Rect(rect.x, curY, buttonWidth, buttonHeight), "WB_TerrainTab".Translate())) currentTab = 1;
            curY += buttonHeight + 5f;

            if (Widgets.ButtonText(new Rect(rect.x, curY, buttonWidth, buttonHeight), "WB_ClimateTab".Translate())) currentTab = 2;
            curY += buttonHeight + 5f;

            if (Widgets.ButtonText(new Rect(rect.x, curY, buttonWidth, buttonHeight), "WB_MoreTab".Translate())) currentTab = 3;
            curY += buttonHeight + 10f;

            float resetButtonHeight = 0f;
            if (currentTab == 0 &&
                (World_ExposeData_Patch.worldGenerationData.biomeCommonalities.Any(x => x.Value != 10) ||
                 World_ExposeData_Patch.worldGenerationData.biomeScoreOffsets.Any(y => y.Value != 0)))
            {
                resetButtonHeight = 35f;
            }

            Rect greySpaceRect = new Rect(rect.x, curY, rect.width, rect.height - curY - Page.BottomButSize.y - 10f - resetButtonHeight);
            Widgets.DrawMenuSection(greySpaceRect);

            float scrollViewHeight = GetTabContentHeight();
            Rect scrollViewContentRect = new Rect(0, 0, greySpaceRect.width - 16f, scrollViewHeight);
            Widgets.BeginScrollView(greySpaceRect, ref scrollPosition, scrollViewContentRect);

            Rect contentRect = new Rect(10f, 10f, scrollViewContentRect.width - 10f, scrollViewHeight);
            switch (currentTab)
            {
                case 0: DrawBiomesTab(contentRect); break;
                case 1: DrawTerrainTab(__instance, contentRect); break;
                case 2: DrawClimateTab(__instance, contentRect); break;
                case 3: DrawMoreTab(contentRect); break;
            }
            Widgets.EndScrollView();

            curY = greySpaceRect.yMax + 5f;
            if (currentTab == 0 &&
                (World_ExposeData_Patch.worldGenerationData.biomeCommonalities.Any(x => x.Value != 10) ||
                 World_ExposeData_Patch.worldGenerationData.biomeScoreOffsets.Any(y => y.Value != 0)))
            {
                if (Widgets.ButtonText(new Rect(rect.x, curY, buttonWidth, 30f), "WB_ResetBiomesToDefault".Translate()))
                {
                    World_ExposeData_Patch.worldGenerationData.ResetBiomeCommonalities();
                    World_ExposeData_Patch.worldGenerationData.ResetBiomeScoreOffsets();
                }
                curY += 35f;
            }

            var canDoBackMethod = AccessTools.Method(typeof(Page_CreateWorldParams), nameof(Page_CreateWorldParams.CanDoBack));
            var doBackMethod = AccessTools.Method(typeof(Page_CreateWorldParams), nameof(Page_CreateWorldParams.DoBack));
            Rect backRect = new Rect(rect.x, rect.yMax - Page.BottomButSize.y, Page.BottomButSize.x, Page.BottomButSize.y);
            if ((Widgets.ButtonText(backRect, "Back".Translate()) || KeyBindingDefOf.Cancel.KeyDownEvent) && (bool)canDoBackMethod.Invoke(__instance, []))
            {
                doBackMethod.Invoke(__instance, []);
            }
        }

        private static void DrawCenterColumn(Page_CreateWorldParams __instance, Rect rect)
        {
            float currentY = rect.y;
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            var preset = WorldPresetManager.CurrentlyLoadedPreset;
            var planetType = preset?.planetType;
            if (planetType.NullOrEmpty())
            {
                planetType = "WB_DefaultPresetName".Translate();
            }
            Widgets.Label(new Rect(rect.x, currentY, rect.width, 30f), "WB_WorldType".Translate(planetType));
            Text.Anchor = TextAnchor.UpperLeft;
            currentY += 35f;
            Text.Font = GameFont.Small;

            float previewSize = rect.width;
            Rect previewRect = new Rect(rect.x, currentY, previewSize, previewSize);
            doWorldPreviewArea(__instance, previewRect);

            const float labelWidth = 120f;
            const float randomizeButtonSize = 30f;
            const float randomizeButtonSpacing = 5f;
            float fieldWidth = rect.width - labelWidth - 5f;
            const float controlHeight = 30f;
            const float controlSpacing = 40f;

            int controlCount = 6;
            if (ModsConfig.IsActive(ModCompatibilityHelper.WorldTechLevelPackageId)) controlCount++;
            if (ModsConfig.OdysseyActive) controlCount++;
            if (ModsConfig.BiotechActive) controlCount++;

            float controlsHeight = controlCount * controlHeight;
            float controlsBottomY = rect.yMax - 10f;
            float currentControlY = controlsBottomY - controlsHeight;

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x, currentControlY, labelWidth, controlHeight), "WB_PlanetName".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            curPlanetName = Widgets.TextField(new Rect(rect.x + labelWidth + 5f, currentControlY, fieldWidth - randomizeButtonSize - randomizeButtonSpacing, controlHeight), curPlanetName);
            Rect randomizePlanetNameRect = new Rect(rect.x + labelWidth + 5f + fieldWidth - randomizeButtonSize, currentControlY, randomizeButtonSize, randomizeButtonSize);
            if (Widgets.ButtonImage(randomizePlanetNameRect, Resources.GeneratePreview))
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                curPlanetName = NameGenerator.GenerateName(RulePackDefOf.NamerWorld);
            }
            TooltipHandler.TipRegion(randomizePlanetNameRect, "Randomize".Translate());
            currentControlY += controlSpacing;

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x, currentControlY, labelWidth, controlHeight), "WB_Seed".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            __instance.seedString = Widgets.TextField(new Rect(rect.x + labelWidth + 5f, currentControlY, fieldWidth - randomizeButtonSize - randomizeButtonSpacing, controlHeight), __instance.seedString);
            Rect randomizeSeedRect = new Rect(rect.x + labelWidth + 5f + fieldWidth - randomizeButtonSize, currentControlY, randomizeButtonSize, randomizeButtonSize);
            if (Widgets.ButtonImage(randomizeSeedRect, Resources.GeneratePreview))
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                __instance.seedString = GenText.RandomSeedString();
            }
            TooltipHandler.TipRegion(randomizeSeedRect, "RandomizeSeed".Translate());
            currentControlY += controlSpacing;

            if (ModsConfig.IsActive(ModCompatibilityHelper.WorldTechLevelPackageId))
            {
                Widgets.Label(new Rect(rect.x, currentControlY, labelWidth, controlHeight), "WB_TechLimit".Translate());
                if (ModCompatibilityHelper.TryGetWTL(out TechLevel currentTL))
                {
                    float iconX = rect.x + labelWidth + 5f;
                    const float techIconSize = 26f;
                    var techLevelIcons = new List<(TechLevel level, Texture2D icon, string tooltip)>
                    {
                        (TechLevel.Undefined, Resources.TechLevel_Unrestricted, "WB_Unrestricted".Translate()),
                        (TechLevel.Animal, Resources.TechLevel_Animal, TechLevel.Animal.ToStringHuman().CapitalizeFirst()),
                        (TechLevel.Neolithic, Resources.TechLevel_Neolithic, TechLevel.Neolithic.ToStringHuman().CapitalizeFirst()),
                        (TechLevel.Medieval, Resources.TechLevel_Medieval, TechLevel.Medieval.ToStringHuman().CapitalizeFirst()),
                        (TechLevel.Industrial, Resources.TechLevel_Industrial, TechLevel.Industrial.ToStringHuman().CapitalizeFirst()),
                        (TechLevel.Spacer, Resources.TechLevel_Spacer, TechLevel.Spacer.ToStringHuman().CapitalizeFirst()),
                        (TechLevel.Ultra, Resources.TechLevel_Ultra, TechLevel.Ultra.ToStringHuman().CapitalizeFirst())
                    };
                    foreach (var (level, icon, tooltip) in techLevelIcons)
                    {
                        Rect iconRect = new Rect(iconX, currentControlY + 2f, techIconSize, techIconSize);
                        if (currentTL == level) Widgets.DrawHighlightSelected(iconRect);
                        if (Widgets.ButtonImage(iconRect, icon))
                        {
                            TechLevel toSet = level;
                            __instance.ResetFactionCounts();
                            ModCompatibilityHelper.TrySetWTL(toSet);
                            ModCompatibilityHelper.ApplyWTLChanges(__instance);
                            SoundDefOf.Click.PlayOneShotOnCamera();
                        }
                        TooltipHandler.TipRegion(iconRect, tooltip);
                        iconX += techIconSize + 2f;
                    }
                }
                currentControlY += controlSpacing;
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x, currentControlY, labelWidth, controlHeight), "WB_Scale".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            int subdivs = PlanetLayerSettingsDefOf.Surface.settings.subdivisions;
            float fSub = subdivs;
            Rect scaleSliderRect = new Rect(rect.x + labelWidth + 5f, currentControlY, fieldWidth, controlHeight);
            fSub = Widgets.HorizontalSlider(scaleSliderRect, fSub, MinScale, MaxScale, true, fSub.ToString(), MinScale.ToString(), MaxScale.ToString(), 1);
            if (Mathf.RoundToInt(fSub) != subdivs)
            {
                int newSub = Mathf.RoundToInt(fSub);
                PlanetLayerSettingsDefOf.Surface.settings.subdivisions = newSub;
                ModCompatibilityHelper.TrySetMLPSubcount(newSub);
            }
            currentControlY += controlSpacing;

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x, currentControlY, labelWidth, controlHeight), "WB_Coverage".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Rect covSliderRect = new Rect(rect.x + labelWidth + 5f, currentControlY, fieldWidth, controlHeight);
            __instance.planetCoverage = Widgets.HorizontalSlider(covSliderRect, __instance.planetCoverage, 0.05f, 1, true, $"{__instance.planetCoverage:P0}", "WB_Small".Translate(), "WB_Large".Translate());
            currentControlY += controlSpacing;

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rect.x, currentControlY, labelWidth, controlHeight), "PlanetPopulation".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Rect populationSlider = new Rect(rect.x + labelWidth + 5f, currentControlY, fieldWidth, controlHeight);
            __instance.population = (OverallPopulation)Mathf.RoundToInt(Widgets.HorizontalSlider(populationSlider,
                (float)__instance.population, 0f, OverallPopulationUtility.EnumValuesCount - 1, middleAlignment: true,
                "PlanetPopulation_Normal".Translate(), "PlanetPopulation_Low".Translate(),
                "PlanetPopulation_High".Translate(), 1f));

            if (ModsConfig.IsActive(ModCompatibilityHelper.VFEInsectoids2PackageId))
            {
                currentControlY += controlSpacing;
                ModCompatibilityHelper.TryAddVFE2InsectTerritoryScaleSlider(rect.x, currentControlY, labelWidth, fieldWidth, controlHeight);
            }

            World_ExposeData_Patch.worldGenerationData.planetCoverage = __instance.planetCoverage;
            World_ExposeData_Patch.worldGenerationData.seedString = __instance.seedString;
            World_ExposeData_Patch.worldGenerationData.population = __instance.population;
        }

        private static void DrawRightColumn(Page_CreateWorldParams __instance, Rect rect)
        {
            Text.Font = GameFont.Medium;
            var title = "WB_Population".Translate() + ":";
            var size = Text.CalcSize(title).x;
            Widgets.Label(new Rect(rect.xMax - size, rect.y, size, 30f), title);
            Text.Font = GameFont.Small;

            const float buttonHeight = 30f;
            const float bottomAreaHeight = buttonHeight * 2 + 5f;

            Rect factionListRect = new Rect(rect.x - 18f, rect.y, rect.width + 35, rect.height - bottomAreaHeight - 5f);
            WorldFactionsUIUtility.DoWindowContents(factionListRect, __instance.factions, true);
            GUI.color = Widgets.WindowBGFillColor;
            var hideFactionsRect = new Rect(factionListRect.x, factionListRect.y, 100, 30);
            GUI.DrawTexture(hideFactionsRect, BaseContent.WhiteTex);
            GUI.color = Color.white;

            Rect gameplayRect = new Rect(rect.x, rect.yMax - bottomAreaHeight, rect.width, buttonHeight);
            if (Widgets.ButtonText(gameplayRect, "WB_GameplaySettings".Translate()))
            {
                Find.WindowStack.Add(new Dialog_AdvancedGameConfig());
            }
            Rect generateRect = new Rect(gameplayRect.x, gameplayRect.yMax + 5f, gameplayRect.width, buttonHeight);
            if ((Widgets.ButtonText(generateRect, "WB_Generate".Translate()) || KeyBindingDefOf.Accept.KeyDownEvent) && __instance.CanDoNext())
            {
                startFresh = true;
                UpdateCurPreset(__instance);
                __instance.DoNext();
            }
            UIHighlighter.HighlightOpportunity(generateRect, "NextPage");
        }

        private static void HandlePreviewAutoGeneration(Page_CreateWorldParams __instance)
        {
            if (!WorldbuilderMod.settings.showPreview)
            {
                return;
            }

            int currentSubdivs = PlanetLayerSettingsDefOf.Surface.settings.subdivisions;
            if (lastSubdivisions != currentSubdivs)
            {
                lastSubdivisions = currentSubdivs;
                updatePreviewCounter = 60;
                if (thread is { IsAlive: true })
                {
                    shouldCancelGeneration = true;
                }
            }

            if (lastGenerationData == null)
            {
                lastGenerationData = World_ExposeData_Patch.worldGenerationData.MakeCopy();
                updatePreviewCounter = 60;
            }
            else if (World_ExposeData_Patch.worldGenerationData.IsDifferentFrom(lastGenerationData))
            {
                lastGenerationData = World_ExposeData_Patch.worldGenerationData.MakeCopy();
                updatePreviewCounter = 60;
                if (thread is { IsAlive: true })
                {
                    shouldCancelGeneration = true;
                }
            }

            if (thread is null)
            {
                if (updatePreviewCounter == 0)
                {
                    startRefreshWorldPreview(__instance);
                }
            }
            else if (thread != null && !thread.IsAlive && shouldCancelGeneration)
            {
                thread = null;
                shouldCancelGeneration = false;
                updatePreviewCounter = 0;
                startRefreshWorldPreview(__instance);
            }

            if (updatePreviewCounter > -2)
            {
                updatePreviewCounter--;
            }
        }

        private static float GetTabContentHeight()
        {
            return currentTab switch
            {
                0 => Utils.GetValidBiomes().Count() * 90 + 10,
                1 => CalculateTerrainTabHeight(),
                2 => CalculateClimateTabHeight(),
                _ => 0
            };
        }

        private static void DoFloatSlider(ref float y, Rect rect, string labelKey, ref float value, float min, float max, bool middleAlignment = false, string format = "0.00", float roundTo = 0f)
        {
            Widgets.Label(new Rect(rect.x, y, rect.width, 30f), labelKey.Translate());
            y += 30f;
            value = Widgets.HorizontalSlider(
                new Rect(rect.x, y, rect.width, 30f),
                value, min, max, middleAlignment: true,
                "PlanetRainfall_Normal".Translate(), "None".Translate(), "PlanetRainfall_High".Translate(), 0.1f);
            y += 30f;
        }

        private static void DoEnumSlider<T>(ref float y, Rect rect, string labelKey, ref T value, float min, float max, string middleLabelKey, string leftLabelKey, string rightLabelKey, float roundTo = 1f) where T : struct
        {
            Widgets.Label(new Rect(rect.x, y, rect.width, 30f), labelKey.Translate());
            y += 30f;
            value = (T)Enum.ToObject(typeof(T), Mathf.RoundToInt(Widgets.HorizontalSlider(
                new Rect(rect.x, y, rect.width, 30f),
                Convert.ToSingle(value), min, max, middleAlignment: true,
                middleLabelKey.Translate(), leftLabelKey.Translate(), rightLabelKey.Translate(), roundTo)));
            y += 30f;
        }

        private static float CalculateTerrainTabHeight()
        {
            float height = 30f * 4;
            if (ModsConfig.BiotechActive) height += 30f;
            if (ModsConfig.OdysseyActive) height += 30f;
            return height;
        }

        private static float CalculateClimateTabHeight()
        {
            return 30f * 4;
        }

        private static void DrawBiomesTab(Rect rect)
        {
            float num = rect.y;

            foreach (var biomeDef in Utils.GetValidBiomes().OrderBy(x => x.label ?? x.defName))
            {
                doBiomeSliders(biomeDef, rect.x, ref num, biomeDef.label?.CapitalizeFirst() ?? biomeDef.defName, rect.width);
            }
        }

        private static void DrawTerrainTab(Page_CreateWorldParams __instance, Rect rect)
        {
            float y = rect.y;

            DoFloatSlider(ref y, rect, "WB_MountainDensity", ref World_ExposeData_Patch.worldGenerationData.mountainDensity, 0f, 2f, true);
            DoFloatSlider(ref y, rect, "WB_SeaLevel", ref World_ExposeData_Patch.worldGenerationData.seaLevel, 0f, 2f, true);
            DoFloatSlider(ref y, rect, "WB_AncientRoadDensity", ref World_ExposeData_Patch.worldGenerationData.ancientRoadDensity, 0f, 2f, true);
            DoFloatSlider(ref y, rect, "WB_FactionRoadDensity", ref World_ExposeData_Patch.worldGenerationData.factionRoadDensity, 0f, 2f, true);

            if (ModsConfig.BiotechActive)
            {
                DoFloatSlider(ref y, rect, "PlanetPollution", ref __instance.pollution, 0f, 1f, true, "P0", 0.05f);
                World_ExposeData_Patch.worldGenerationData.pollution = __instance.pollution;
            }

            if (ModsConfig.OdysseyActive)
            {
                DoEnumSlider(ref y, rect, "PlanetLandmarkDensity", ref __instance.landmarkDensity, 0f, LandmarkDensityUtility.EnumValuesCount - 1,
                    "PlanetLandmarkDensity_Normal", "PlanetLandmarkDensity_Low", "PlanetLandmarkDensity_High", 1f);
                World_ExposeData_Patch.worldGenerationData.landmarkDensity = __instance.landmarkDensity;
            }
        }

        private static void DrawClimateTab(Page_CreateWorldParams __instance, Rect rect)
        {
            float y = rect.y;

            DoFloatSlider(ref y, rect, "WB_RiverDensity", ref World_ExposeData_Patch.worldGenerationData.riverDensity, 0f, 2f, true);

            if (!ModCompat.MyLittlePlanetActive)
            {
                DoEnumSlider(ref y, rect, "WB_AxialTilt", ref World_ExposeData_Patch.worldGenerationData.axialTilt, 0f, AxialTiltUtility.EnumValuesCount - 1,
                    "PlanetRainfall_Normal", "PlanetRainfall_Low", "PlanetRainfall_High", 1f);
            }

            DoEnumSlider(ref y, rect, "PlanetRainfall", ref __instance.rainfall, 0f, OverallRainfallUtility.EnumValuesCount - 1,
                "PlanetRainfall_Normal", "PlanetRainfall_Low", "PlanetRainfall_High", 1f);
            DoEnumSlider(ref y, rect, "PlanetTemperature", ref __instance.temperature, 0f, OverallTemperatureUtility.EnumValuesCount - 1,
                "PlanetTemperature_Normal", "PlanetTemperature_Low", "PlanetTemperature_High", 1f);
                
            World_ExposeData_Patch.worldGenerationData.rainfall = __instance.rainfall;
            World_ExposeData_Patch.worldGenerationData.temperature = __instance.temperature;
        }

        public static bool ShouldShowMoreTabMessage()
        {
            return true;
        }

        public static Vector3 moreTabPos;
        public static void DrawMoreTab(Rect rect)
        {
            if (ShouldShowMoreTabMessage())
            {
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.UpperLeft;
                Text.WordWrap = true;
                Widgets.Label(rect, "WB_MoreTabNoSettings".Translate());
                Text.WordWrap = false;
            }
        }

        private static void doBiomeSliders(BiomeDef biomeDef, float x, ref float num, string label, float width)
        {
            if (World_ExposeData_Patch.worldGenerationData is null || World_ExposeData_Patch.worldGenerationData.biomeCommonalities is null ||
                World_ExposeData_Patch.worldGenerationData.biomeScoreOffsets is null)
            {
                return;
            }

            var labelRect = new Rect(x, num - 10, 200f, 30f);
            Widgets.Label(labelRect, label);
            num += 10;

            World_ExposeData_Patch.worldGenerationData.biomeCommonalities.TryAdd(biomeDef.defName, 10);
            World_ExposeData_Patch.worldGenerationData.biomeScoreOffsets.TryAdd(biomeDef.defName, 0);

            var biomeCommonalityLabel = new Rect(labelRect.x, num + 5, 70, 30);
            var value = World_ExposeData_Patch.worldGenerationData.biomeCommonalities[biomeDef.defName];
            if (value < 10f) GUI.color = Color.red;
            else if (value > 10f) GUI.color = Color.green;

            Widgets.Label(biomeCommonalityLabel, "WB_Commonality".Translate());
            float sliderWidth = width - biomeCommonalityLabel.width - 10f;
            var biomeCommonalitySlider = new Rect(biomeCommonalityLabel.xMax + 5, num, sliderWidth, 30f);
            World_ExposeData_Patch.worldGenerationData.biomeCommonalities[biomeDef.defName] = (int)Widgets.HorizontalSlider(biomeCommonalitySlider, value, 0, 20, false, $"{value * 10}%");
            GUI.color = Color.white;
            num += 30f;

            var biomeOffsetLabel = new Rect(labelRect.x, num + 5, 70, 30);
            var value2 = World_ExposeData_Patch.worldGenerationData.biomeScoreOffsets[biomeDef.defName];
            if (value2 < 0f) GUI.color = Color.red;
            else if (value2 > 0f) GUI.color = Color.green;

            Widgets.Label(biomeOffsetLabel, "WB_ScoreOffset".Translate());
            var scoreOffsetSlider = new Rect(biomeOffsetLabel.xMax + 5, num, sliderWidth, 30f);
            World_ExposeData_Patch.worldGenerationData.biomeScoreOffsets[biomeDef.defName] = (int)Widgets.HorizontalSlider(scoreOffsetSlider, value2, -99, 99, false, value2.ToString());
            GUI.color = Color.white;
            num += 50f;
        }

        private static void doSlider(float x, ref float num, string label, ref float field, string leftLabel, float width)
        {
            var labelRect = new Rect(x, num, 120f, 40f);
            Widgets.Label(labelRect, label);
            var slider = new Rect(labelRect.xMax + 5f, num, width - labelRect.width - 15f, 30f);
            field = Widgets.HorizontalSlider(slider, field, 0, 2f, middleAlignment: true,
                "PlanetRainfall_Normal".Translate(), leftLabel, "PlanetRainfall_High".Translate(), 1f);
            num += 40f;

        }

        private static void doWorldPreviewArea(Page_CreateWorldParams window, Rect previewRect)
        {
            var generateButtonRect = new Rect(previewRect.xMax - 80, previewRect.y, 35, 35);
            var hideButtonRect = new Rect(generateButtonRect.x + generateButtonRect.width * 1.1f, previewRect.y, 35, 35);

            drawHidePreviewButton(window, hideButtonRect);

            if (WorldbuilderMod.settings.showPreview)
            {
                drawGeneratePreviewButton(window, generateButtonRect);

                if (thread is null && Find.World != null && Find.World.info.name != "DefaultWorldName" || worldPreview != null)
                {
                    if (dirty)
                    {
                        var numAttempt = 0;
                        while (numAttempt < 5)
                        {
                            worldPreview = getWorldCameraPreview((int)previewRect.height, (int)previewRect.width);
                            if (worldPreview == null || isBlack(worldPreview))
                            {
                                numAttempt++;
                            }
                            else
                            {
                                dirty = false;
                                break;
                            }
                        }
                    }

                    if (worldPreview != null)
                    {
                        previewRect.y -= 30;
                        GUI.DrawTexture(previewRect, worldPreview);
                    }
                }

                drawGeneratingText(previewRect);
            }
        }

        public static void UpdateCurPreset(Page_CreateWorldParams window)
        {
            World_ExposeData_Patch.worldGenerationData.rainfall = window.rainfall;
            World_ExposeData_Patch.worldGenerationData.population = window.population;
            World_ExposeData_Patch.worldGenerationData.planetCoverage = window.planetCoverage;
            World_ExposeData_Patch.worldGenerationData.seedString = window.seedString;
            World_ExposeData_Patch.worldGenerationData.temperature = window.temperature;
            if (ModsConfig.BiotechActive)
            {
                World_ExposeData_Patch.worldGenerationData.pollution = window.pollution;
            }
            if (ModsConfig.OdysseyActive)
            {
                World_ExposeData_Patch.worldGenerationData.landmarkDensity = window.landmarkDensity;
            }
        }

        public static void ApplyChanges(Page_CreateWorldParams window)
        {
            window.rainfall = World_ExposeData_Patch.worldGenerationData.rainfall;
            window.population = World_ExposeData_Patch.worldGenerationData.population;
            window.planetCoverage = World_ExposeData_Patch.worldGenerationData.planetCoverage;
            if (World_ExposeData_Patch.worldGenerationData.seedString != null)
            {
                window.seedString = World_ExposeData_Patch.worldGenerationData.seedString;
            }
            window.temperature = World_ExposeData_Patch.worldGenerationData.temperature;
            if (ModsConfig.BiotechActive)
            {
                window.pollution = World_ExposeData_Patch.worldGenerationData.pollution;
            }
            if (ModsConfig.OdysseyActive)
            {
                window.landmarkDensity = World_ExposeData_Patch.worldGenerationData.landmarkDensity;
            }
        }

        private static bool isBlack(Texture2D texture)
        {
            var pixel = texture.GetPixel(texture.width / 2, texture.height / 2);
            return pixel.r <= 0 && pixel is { g: <= 0, b: <= 0 };
        }

        private static void startRefreshWorldPreview(Page_CreateWorldParams window)
        {
            dirty = false;
            if (thread is { IsAlive: true })
            {
                shouldCancelGeneration = true;
                return;
            }

            if (!WorldbuilderMod.settings.showPreview)
            {
                return;
            }

            previewStepsDone = 0;
            previewStepsTotal = 0;

            shouldCancelGeneration = false;
            thread = new Thread(delegate () { generateWorld(window); });
            thread.Start();
        }

        private static void drawHidePreviewButton(Page_CreateWorldParams window, Rect hideButtonRect)
        {
            var buttonTexture = Resources.Visible;
            if (!WorldbuilderMod.settings.showPreview)
            {
                buttonTexture = Resources.InVisible;
            }

            if (Widgets.ButtonImageFitted(hideButtonRect, buttonTexture))
            {
                WorldbuilderMod.settings.showPreview =
                    !WorldbuilderMod.settings.showPreview;
                WorldbuilderMod.settings.Write();
                if (WorldbuilderMod.settings.showPreview)
                {
                    startRefreshWorldPreview(window);
                }
            }

            Widgets.DrawHighlightIfMouseover(hideButtonRect);
            TooltipHandler.TipRegion(hideButtonRect, "WB_HidePreview".Translate());
        }

        private static void drawGeneratePreviewButton(Page_CreateWorldParams window, Rect generateButtonRect)
        {
            if (thread != null)
            {
                if (texSpinAngle > 360f)
                {
                    texSpinAngle -= 360f;
                }

                texSpinAngle += 3;
            }

            if (thread != null)
            {
                var pct = 0f;
                var total = previewStepsTotal;
                if (total > 0)
                {
                    pct = Mathf.Clamp01((float)previewStepsDone / total);
                }

                Widgets.FillableBar(generateButtonRect, pct);
                TooltipHandler.TipRegion(generateButtonRect, total > 0 ? previewStepsDone + "/" + previewStepsTotal : "");
            }

            if (Prefs.UIScale != 1f)
            {
                GUI.DrawTexture(generateButtonRect, Resources.GeneratePreview);
            }
            else
            {
                Widgets.DrawTextureRotated(generateButtonRect, Resources.GeneratePreview, texSpinAngle);
            }

            if (Mouse.IsOver(generateButtonRect))
            {
                Widgets.DrawHighlightIfMouseover(generateButtonRect);
                if (Event.current.type == EventType.MouseDown)
                {
                    if (Event.current.button == 0)
                    {
                        startRefreshWorldPreview(window);
                        Event.current.Use();
                    }
                }
            }

            if (thread == null || thread.IsAlive || threadedWorld == null)
            {
                return;
            }

            if (initializeWorld())
            {
                dirty = true;
            }
            threadedWorld = null;
            thread = null;
            generatingWorld = false;
        }

        private static void drawGeneratingText(Rect previewRect)
        {
            if (thread != null)
            {
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.MiddleCenter;
                GUI.color = Color.white;
                Widgets.Label(previewRect, "WB_GeneratingPreview".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }
        }

        private static bool initializeWorld()
        {
            if (Find.World == null || Find.WorldGrid == null || Find.World.components == null || Find.World.renderer == null)
            {
                return false;
            }

            var layers = Find.World.renderer.AllDrawLayers;
            foreach (var layer in layers)
            {
                if (layer is WorldDrawLayer_Hills or WorldDrawLayer_Rivers or WorldDrawLayer_Roads
                    or WorldDrawLayer_Terrain)
                {
                    layer.RegenerateNow();
                }
            }

            var comps = Find.World.components.Where(x => x.GetType().Name == "TacticalGroups");
            foreach (var comp in comps)
            {
                try
                {
                    comp.FinalizeInit(false);
                }
                catch (Exception ex)
                {
                    Log.Warning($"[RG] initializeWorld: Error finalizing component {comp.GetType().Name}: {ex.Message}");
                }
            }
            return true;
        }

        private static void generateWorld(Page_CreateWorldParams page)
        {
            generatingWorld = true;

            var prevProgramState = Current.ProgramState;
            var prevFaction = Find.World?.factionManager?.OfPlayer;

            Rand.PushState();

            try
            {
                var seed = Rand.Seed = GenText.StableStringHash(page.seedString);
                Current.ProgramState = ProgramState.Entry;

                if (Current.Game.World != null)
                {
                    Current.Game.World = null;
                }

                if (prevFaction is null)
                {
                    Find.GameInitData.ResetWorldRelatedMapInitData();
                }

                Current.CreatingWorld = new World
                {
                    renderer = new WorldRenderer(),
                    UI = new WorldInterface(),
                    factionManager = new FactionManager(),
                    info =
                    {
                        seedString = page.seedString,
                        planetCoverage = page.planetCoverage,
                        overallRainfall = page.rainfall,
                        overallTemperature = page.temperature,
                        overallPopulation = page.population,
                        pollution = ModsConfig.BiotechActive ? page.pollution : 0f,
                        name = curPlanetName
                    }
                };

                if (Current.CreatingWorld == null)
                {
                    return;
                }

                Current.CreatingWorld.factionManager.ofPlayer = prevFaction;
                Current.CreatingWorld.dynamicDrawManager = new WorldDynamicDrawManager();
                Current.CreatingWorld.ticksAbsCache = new ConfiguredTicksAbsAtGameStartCache();
                Current.Game.InitData.playerFaction = prevFaction;

                previewStepsDone = 0;
                previewStepsTotal = GameSetupStepsInOrder.Count;
                foreach (var item in GameSetupStepsInOrder)
                {
                    if (shouldCancelGeneration || !Find.WindowStack.IsOpen<Page_CreateWorldParams>())
                    {
                        return;
                    }

                    Rand.Seed = Gen.HashCombineInt(seed, item.setupStep.SeedPart);
                    item.setupStep.GenerateFresh();
                    previewStepsDone++;
                }

                var tmpGenSteps = new List<WorldGenStepDef>();

                var activeGrid = Find.WorldGrid ?? Current.CreatingWorld.grid;

                try
                {
                    var totalWorldGenSteps = 0;
                    foreach (var layerKvp in activeGrid.PlanetLayers)
                    {
                        totalWorldGenSteps += layerKvp.Value.Def.GenStepsInOrder.Count(s => worldGenStepDefs.Contains(s));
                    }

                    previewStepsTotal += totalWorldGenSteps;
                }
                catch
                {
                }

                foreach (var planetLayer in activeGrid.PlanetLayers)
                {
                    if (shouldCancelGeneration || !Find.WindowStack.IsOpen<Page_CreateWorldParams>())
                    {
                        return;
                    }

                    tmpGenSteps.Clear();
                    tmpGenSteps.AddRange(planetLayer.Value.Def.GenStepsInOrder);

                    for (var i = 0; i < tmpGenSteps.Count; i++)
                    {
                        if (shouldCancelGeneration || !Find.WindowStack.IsOpen<Page_CreateWorldParams>())
                        {
                            return;
                        }

                        try
                        {
                            Rand.Seed = Gen.HashCombineInt(seed, getSeedPart(tmpGenSteps, i));

                            if (!worldGenStepDefs.Contains(tmpGenSteps[i]))
                            {
                                continue;
                            }

                            tmpGenSteps[i].worldGenStep.GenerateFresh(page.seedString, planetLayer.Value);

                            if (tmpGenSteps[i].defName == "Tiles" && prevFaction != null)
                            {
                                Current.CreatingWorld.factionManager.ofPlayer = prevFaction;
                            }

                            previewStepsDone++;
                        }
                        catch (Exception ex)
                        {
                            if (ex is ThreadAbortException)
                            {
                                Rand.PopState();
                                Current.CreatingWorld = null;
                                generatingWorld = false;
                                Current.ProgramState = prevProgramState;
                                return;
                            }
                            else
                            {
                                Log.Error($"[RG] generateWorld: Error in WorldGenStep {tmpGenSteps[i].defName}: {ex}");
                            }
                        }
                    }
                }

                if (shouldCancelGeneration || !Find.WindowStack.IsOpen<Page_CreateWorldParams>())
                {
                    return;
                }

                Rand.Seed = seed;
                activeGrid.StandardizeTileData();

                threadedWorld = Current.CreatingWorld;
                Current.Game.World = threadedWorld;

                if (Current.Game.World != null)
                {
                    Current.Game.World.features = new WorldFeatures();
                }

                MemoryUtility.UnloadUnusedUnityAssets();
                previewStepsDone = previewStepsTotal;
            }
            catch (Exception ex)
            {
                if (ex is ThreadAbortException)
                {
                    var stateStack = Rand.stateStack;
                    if (stateStack?.Any() == true)
                    {
                        Rand.PopState();
                    }

                    generatingWorld = false;
                    Current.ProgramState = prevProgramState;
                    Current.CreatingWorld = null;
                }
            }
            finally
            {
                var stateStack = Rand.stateStack;
                if (stateStack?.Any() == true)
                {
                    Rand.PopState();
                }

                generatingWorld = false;
                Current.CreatingWorld = null;
                Current.ProgramState = prevProgramState;
            }
        }

        private static int getSeedPart(List<WorldGenStepDef> genSteps, int index)
        {
            var seedPart = genSteps[index].worldGenStep.SeedPart;
            var num = 0;
            for (var i = 0; i < index; i++)
            {
                if (genSteps[i].worldGenStep.SeedPart == seedPart)
                {
                    num++;
                }
            }

            return seedPart + num;
        }

        private static Texture2D getWorldCameraPreview(int width, int height)
        {

            if (Find.World == null || Find.World.renderer == null || Find.WorldCamera == null || Find.World.UI == null || Find.GameInitData == null)
            {
                return null;
            }

            Find.World.renderer.wantedMode = WorldRenderMode.Planet;
            Find.WorldCamera.gameObject.SetActive(true);
            Find.World.UI.Reset();
            Find.WorldCameraDriver.RotateSoNorthIsUp(false);
            AccessTools.Field(typeof(WorldCameraDriver), "rotationAnimation_lerpFactor").SetValue(Find.WorldCameraDriver, 1f);
            AccessTools.Field(typeof(WorldCameraDriver), nameof(WorldCameraDriver.desiredAltitude))
                .SetValue(Find.WorldCameraDriver, 800);
            Find.WorldCameraDriver.altitude = 800;
            AccessTools.Method(typeof(WorldCameraDriver), nameof(WorldCameraDriver.ApplyPositionToGameObject))
                .Invoke(Find.WorldCameraDriver, []);

            var rect = new Rect(0, 0, width, height);
            var renderTexture = new RenderTexture(width, height, 24);
            var screenShot = new Texture2D(width, height, TextureFormat.RGBA32, false);

            Find.WorldCamera.targetTexture = renderTexture;
            Find.WorldCamera.Render();

            ExpandableWorldObjectsUtility.ExpandableWorldObjectsUpdate();

            try
            {
                foreach (var layer in Find.World.renderer.AllDrawLayers)
                {
                    if (layer is WorldDrawLayer_Clouds)
                    {
                        continue;
                    }

                    try
                    {
                        layer.Render();
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"[RG] Error rendering layer {layer.GetType().Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RG] Error during world layer rendering: {ex.Message}");
            }

            try
            {
                if (Find.World?.dynamicDrawManager != null)
                {
                    Find.World.dynamicDrawManager.DrawDynamicWorldObjects();
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RG] Error drawing dynamic world objects: {ex.Message}");
            }

            try
            {
                if (Find.World?.features != null)
                {
                    Find.World.features.UpdateFeatures();
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RG] Error updating world features: {ex.Message}");
            }

            NoiseDebugUI.RenderPlanetNoise();

            RenderTexture.active = renderTexture;
            screenShot.ReadPixels(rect, 0, 0);
            screenShot.Apply();
            Find.WorldCamera.targetTexture = null;
            RenderTexture.active = null;

            Find.WorldCamera.gameObject.SetActive(false);
            if (Find.World != null)
            {
                Find.World.renderer.wantedMode = WorldRenderMode.None;
            }
            return screenShot;
        }
    }
}
