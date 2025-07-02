using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld.Planet;
using Verse.Profile;
namespace Worldbuilder
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    public class Page_SelectWorld : Page
    {
        private WorldPreset selectedPreset;
        private List<WorldPreset> availablePresets = new List<WorldPreset>();
        private Vector2 leftScrollPosition = Vector2.zero;
        private Vector2 descriptionScrollPosition = Vector2.zero;
        private float thumbSpacing = 8f;
        private static WorldPreset defaultPreset;
        private static readonly Texture2D DefaultPresetIcon = ContentFinder<Texture2D>.Get("Worldbuilder/UI/DefaultPresetIcon");
        private static readonly Texture2D DefaultPresetFlavourImage = ContentFinder<Texture2D>.Get("Worldbuilder/UI/DefaultPresetFlavour");
        private static readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
        public override string PageTitle => "WB_SelectPresetTitle".Translate();
        public Page_CreateWorldParams createWorldParamsPage;
        public Page_SelectWorld(Page_CreateWorldParams page_CreateWorldParams)
        {
            this.createWorldParamsPage = page_CreateWorldParams;
            this.next = page_CreateWorldParams;

        }

        private static void EnsureDefaultPreset()
        {
            if (defaultPreset == null)
            {
                defaultPreset = new WorldPreset
                {
                    name = "_DEFAULT_",
                    description = "WB_DefaultPresetDescription".Translate()
                };
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            EnsureDefaultPreset();
            availablePresets.Clear();
            availablePresets.Add(defaultPreset);
            availablePresets.AddRange(WorldPresetManager.GetAllPresets(true));
            selectedPreset = defaultPreset;
        }

        public override void DoWindowContents(Rect rect)
        {
            DrawPageTitle(rect);
            Rect mainRect = GetMainRect(rect);
            float leftPanelWidth = mainRect.width * 0.15f;
            float rightPanelWidth = mainRect.width - leftPanelWidth - 10f;

            Rect leftPanelRect = new Rect(mainRect.x, mainRect.y, leftPanelWidth, mainRect.height);
            Rect rightPanelRect = new Rect(leftPanelRect.xMax + 10f, mainRect.y, rightPanelWidth, mainRect.height);

            DrawLeftPanel(leftPanelRect);
            DrawRightPanel(rightPanelRect);

            DoBottomButtonsModified(rect);
        }

        private void DrawLeftPanel(Rect rect)
        {
            Vector2 thumbSize = new Vector2(130f, 130f);
            float viewWidth = thumbSize.x + 16f;
            float margin = (rect.width - viewWidth) / 2f;
            if (margin < 5f) margin = 5f;

            float rowHeight = thumbSize.y + thumbSpacing;
            float totalContentHeight = availablePresets.Count * rowHeight;

            Rect viewRect = new Rect(0f, 0f, thumbSize.x, totalContentHeight);
            Widgets.BeginScrollView(rect, ref leftScrollPosition, viewRect);

            float currentY = 5f;
            float currentX = 5f;

            foreach (var preset in availablePresets)
            {
                Rect entryRect = new Rect(currentX, currentY, thumbSize.x, thumbSize.y);
                Texture2D thumb;
                if (preset == defaultPreset)
                {
                    thumb = DefaultPresetIcon;
                }
                else
                {
                    thumb = GetTexture(WorldPresetManager.GetThumbnailPath(preset.name)) ?? ExpansionDefOf.Core.Icon;
                }

                DrawListEntry(entryRect, preset, thumb);
                if (Widgets.ButtonInvisible(entryRect))
                {
                    selectedPreset = preset;
                }
                currentY += rowHeight;
            }
            Widgets.EndScrollView();
        }

        private void DrawListEntry(Rect rect, WorldPreset preset, Texture2D thumbnail)
        {
            Widgets.DrawOptionBackground(rect, selectedPreset == preset);
            if (thumbnail != null)
            {
                GUI.DrawTexture(rect, thumbnail, ScaleMode.ScaleToFit);
            }
        }


        private void DrawRightPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            string name;
            string description;
            Texture2D flavorImage;

            if (selectedPreset == defaultPreset)
            {
                name = "WB_DefaultPresetName".Translate();
                description = defaultPreset.description;
                flavorImage = DefaultPresetFlavourImage;
            }
            else if (selectedPreset != null)
            {
                name = selectedPreset.name;
                description = selectedPreset.description ?? "WB_CommonNoDescription".Translate();
                flavorImage = GetTexture(WorldPresetManager.GetFlavorImagePath(selectedPreset.name)) ?? ExpansionDefOf.Core.BackgroundImage;
            }
            else
            {
                name = "";
                description = "WB_SelectPresetSelectPrompt".Translate();
                flavorImage = ExpansionDefOf.Core.BackgroundImage;
            }

            float padding = 10f;
            float spacing = 5f;
            float nameHeight = 30f;
            Rect contentRect = rect.ContractedBy(padding);
            Rect imageDrawRect = default;
            float imageFinalHeight = 0f;

            if (flavorImage != null)
            {
                float texAspect = (float)flavorImage.width / flavorImage.height;
                float finalW = contentRect.width;
                float finalH = finalW / texAspect;
                float maxImageHeight = contentRect.height * 0.6f;
                if (finalH > maxImageHeight)
                {
                    finalH = maxImageHeight;
                    finalW = finalH * texAspect;
                    if (finalW > contentRect.width)
                    {
                        finalW = contentRect.width;
                        finalH = finalW / texAspect;
                    }
                }

                imageFinalHeight = finalH;
                imageDrawRect = new Rect(contentRect.x, contentRect.yMax - finalH, finalW, finalH);
            }
            float textHeightAvailable = contentRect.height - imageFinalHeight - spacing;
            if (textHeightAvailable < 0) textHeightAvailable = 0;
            Rect textRect = new Rect(contentRect.x, contentRect.y, contentRect.width, textHeightAvailable);
            Rect nameRect = new Rect(textRect.x, textRect.y, textRect.width, nameHeight);
            Text.Font = GameFont.Medium;
            Widgets.Label(nameRect, name);
            Text.Font = GameFont.Small;
            float descScrollViewY = nameRect.yMax + spacing;
            float descScrollViewHeight = textRect.yMax - descScrollViewY;

            if (descScrollViewHeight > 20f)
            {
                Rect descScrollViewOuterRect = new Rect(textRect.x, descScrollViewY, textRect.width, descScrollViewHeight);
                float actualDescHeight = Text.CalcHeight(description, descScrollViewOuterRect.width - 16f);
                Rect descScrollViewViewRect = new Rect(0f, 0f, descScrollViewOuterRect.width - 16f, actualDescHeight);

                Widgets.BeginScrollView(descScrollViewOuterRect, ref descriptionScrollPosition, descScrollViewViewRect);
                Widgets.Label(new Rect(0f, 0f, descScrollViewViewRect.width, actualDescHeight), description);
                Widgets.EndScrollView();
            }
            if (flavorImage != null && imageDrawRect != default)
            {
                GUI.DrawTexture(imageDrawRect, flavorImage, ScaleMode.ScaleToFit);
            }
        }
        public override bool CanDoNext() => false;

        private void DoNextSkipConfigure()
        {
            SetSelectedPresetName();
            GenerateWorldAndProceed();
        }
        private void DoConfigurePlanet()
        {
            SetSelectedPresetName();
            if (createWorldParamsPage != null)
            {
                createWorldParamsPage.Reset();
            }
            var oldNext = next;
            next = createWorldParamsPage;
            base.DoNext();
            next = oldNext;
        }

        private void SetSelectedPresetName()
        {
            if (selectedPreset == defaultPreset)
            {
                World_ExposeData_Patch.worldPresetName = null;
            }
            else
            {
                World_ExposeData_Patch.worldPresetName = selectedPreset?.name;
            }
        }
        private void GenerateWorldAndProceed()
        {
            LongEventHandler.QueueLongEvent(delegate
            {
                Find.GameInitData.ResetWorldRelatedMapInitData();
                string seed = selectedPreset?.saveTerrain == true && !string.IsNullOrEmpty(selectedPreset.savedSeedString)
                                ? selectedPreset.savedSeedString
                                : GenText.RandomSeedString();

                float coverage = selectedPreset?.saveTerrain == true && selectedPreset.savedPlanetCoverage >= 0f
                                ? selectedPreset.savedPlanetCoverage
                                : ((!Prefs.DevMode || !UnityData.isEditor) ? 0.3f : 0.05f);
                OverallRainfall rain = selectedPreset?.saveTerrain == true
                                ? selectedPreset.rainfall
                                : OverallRainfall.Normal;

                OverallTemperature temp = selectedPreset?.saveTerrain == true
                                ? selectedPreset.temperature
                                : OverallTemperature.Normal;

                OverallPopulation pop = selectedPreset != null
                                ? selectedPreset.population
                                : OverallPopulation.Normal;

                float pollutionParam = selectedPreset?.saveTerrain == true && selectedPreset.savedPollution >= 0f
                                ? selectedPreset.savedPollution
                                : (ModsConfig.BiotechActive ? 0.05f : 0f);

                var landmarkDensity = selectedPreset != null ? selectedPreset.landmarkDensity
                    : LandmarkDensity.Normal;

                List <FactionDef> factionsToGenerate;
                if (selectedPreset?.saveFactions == true && selectedPreset.savedFactionDefs != null)
                {
                    factionsToGenerate = selectedPreset.savedFactionDefs;
                }
                else
                {
                    factionsToGenerate = new List<FactionDef>();
                    foreach (FactionDef configurableFaction in FactionGenerator.ConfigurableFactions)
                    {
                        if (configurableFaction.startingCountAtWorldCreation > 0)
                        {
                            for (int i = 0; i < configurableFaction.startingCountAtWorldCreation; i++)
                            {
                                factionsToGenerate.Add(configurableFaction);
                            }
                        }
                    }
                    foreach (FactionDef faction in FactionGenerator.ConfigurableFactions)
                    {
                        if (faction.replacesFaction != null)
                        {
                            factionsToGenerate.RemoveAll((FactionDef x) => x == faction.replacesFaction);
                        }
                    }
                }
                Current.Game.World = WorldGenerator.GenerateWorld(coverage, seed, rain, temp, pop, landmarkDensity, factionsToGenerate, pollutionParam);

                LongEventHandler.ExecuteWhenFinished(delegate
                {
                    Find.WindowStack.Add(this.createWorldParamsPage.next);
                    MemoryUtility.UnloadUnusedUnityAssets();
                    Find.World.renderer.RegenerateAllLayersNow();
                    Close();
                });
            }, "GeneratingWorld", doAsynchronously: true, null);
        }

        private static Texture2D GetTexture(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            if (textureCache.TryGetValue(path, out Texture2D cachedTex))
            {
                return cachedTex;
            }

            try
            {
                byte[] fileData = File.ReadAllBytes(path);
                Texture2D newTex = new Texture2D(2, 2);
                if (newTex.LoadImage(fileData))
                {
                    textureCache[path] = newTex;
                    return newTex;
                }
                return null;
            }
            catch (System.Exception ex)
            {
                Log.Error($"Worldbuilder: Exception loading texture from {path}: {ex.Message}");
                return null;
            }
        }
        private void DoBottomButtonsModified(Rect rect)
        {
            float y = rect.y + rect.height - BottomButHeight;
            float middleX = rect.x + rect.width / 2f;
            float buttonWidth = BottomButSize.x;
            Rect backRect = new Rect(rect.x, y, buttonWidth, BottomButHeight);
            if (Widgets.ButtonText(backRect, "Back".Translate()) && CanDoBack())
            {
                DoBack();
            }
            Rect configureRect = new Rect(middleX - buttonWidth / 2f, y, buttonWidth, BottomButHeight);
            if (Widgets.ButtonText(configureRect, "WB_SelectPresetConfigurePlanetButton".Translate()))
            {
                DoConfigurePlanet();
            }
            Rect nextRect = new Rect(rect.xMax - buttonWidth, y, buttonWidth, BottomButHeight);
            if (Widgets.ButtonText(nextRect, "Next".Translate()))
            {
                if (selectedPreset == defaultPreset)
                {
                    next = createWorldParamsPage;
                    createWorldParamsPage.prev = this;
                    DoNext();
                }
                else
                {
                    DoNextSkipConfigure();
                }
            }
        }
    }
}
