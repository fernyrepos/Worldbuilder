using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace Worldbuilder
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    public class Page_SelectWorld : Page
    {
        public WorldPreset selectedPreset;
        private List<WorldPreset> availablePresets = new List<WorldPreset>();
        private Vector2 leftScrollPosition = Vector2.zero;
        private Vector2 descriptionScrollPosition = Vector2.zero;
        private float thumbSpacing = 8f;
        public static WorldPreset defaultPreset;
        private static readonly Texture2D DefaultPresetIcon = ContentFinder<Texture2D>.Get("Worldbuilder/UI/DefaultPresetIcon");
        private static readonly Texture2D DefaultPresetFlavourImage = ContentFinder<Texture2D>.Get("Worldbuilder/UI/DefaultPresetFlavour");
        private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
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
                    description = "WB_DefaultPresetDescription".Translate(),
                    sortPriority = 0
                };
            }
        }

        public override void PreOpen()
        {
            base.PreOpen();
            World_ExposeData_Patch.WorldPresetName = null;
            EnsureDefaultPreset();
            availablePresets.Clear();
            availablePresets.Add(defaultPreset);
            availablePresets.AddRange(WorldPresetManager.GetAllPresets(true));
            selectedPreset = defaultPreset;
            createWorldParamsPage.ResetFactionCounts();
        }

        public override void DoWindowContents(Rect rect)
        {
            DrawPageTitle(rect);
            var mainRect = GetMainRect(rect);
            float leftPanelWidth = mainRect.width * 0.15f;
            float rightPanelWidth = mainRect.width - leftPanelWidth - 10f;

            var leftPanelRect = new Rect(mainRect.x, mainRect.y, leftPanelWidth, mainRect.height);
            var rightPanelRect = new Rect(leftPanelRect.xMax + 10f, mainRect.y, rightPanelWidth, mainRect.height);

            DrawLeftPanel(leftPanelRect);
            DrawRightPanel(rightPanelRect);

            DoBottomButtonsModified(rect);
        }

        private void DrawLeftPanel(Rect rect)
        {
            var thumbSize = new Vector2(130f, 130f);
            float viewWidth = thumbSize.x + 16f;
            float margin = (rect.width - viewWidth) / 2f;
            if (margin < 5f) margin = 5f;

            float rowHeight = thumbSize.y + thumbSpacing;
            float totalContentHeight = availablePresets.Count * rowHeight;

            var viewRect = new Rect(0f, 0f, thumbSize.x, totalContentHeight);
            Widgets.BeginScrollView(rect, ref leftScrollPosition, viewRect);

            float currentY = 5f;
            float currentX = 5f;

            foreach (var preset in availablePresets.OrderBy(p => p.sortPriority))
            {
                Rect entryRect = new Rect(currentX, currentY, thumbSize.x, thumbSize.y);
                Texture2D thumb;
                if (preset == defaultPreset)
                {
                    thumb = DefaultPresetIcon;
                }
                else
                {
                    thumb = GetTexture(preset.ThumbnailPath) ?? ExpansionDefOf.Core.Icon;
                }

                DrawListEntry(entryRect, preset, thumb);
                if (Widgets.ButtonInvisible(entryRect))
                {
                    if (IsValid(preset, out string failReason) is false)
                    {
                        Messages.Message(failReason, MessageTypeDefOf.RejectInput);
                    }
                    else
                    {
                        if (ModsConfig.IsActive(ModCompatibilityHelper.WorldTechLevelPackageId))
                        {
                            ModCompatibilityHelper.TryGetWTLUnrestricted(out var wasUnrestricted);
                            if (selectedPreset.worldTechLevel != TechLevel.Undefined)
                            {
                                ModCompatibilityHelper.TrySetWTLUnrestricted(false);
                                ModCompatibilityHelper.TrySetWTL(selectedPreset.worldTechLevel);
                            }
                            else
                            {
                                ModCompatibilityHelper.TrySetWTL(TechLevel.Archotech);
                            }
                            createWorldParamsPage.ResetFactionCounts();
                            ModCompatibilityHelper.TrySetWTLUnrestricted(wasUnrestricted);
                        }
                        selectedPreset = preset;

                        if (preset.saveGenerationParameters && preset.generationData != null)
                        {
                            World_ExposeData_Patch.worldGenerationData = preset.generationData.MakeCopy();

                            if (preset.disableExtraBiomes)
                            {
                                foreach (var biomeDef in Utils.GetValidBiomes())
                                {
                                    if (!World_ExposeData_Patch.worldGenerationData.biomeCommonalities.ContainsKey(biomeDef.defName))
                                    {
                                        World_ExposeData_Patch.worldGenerationData.biomeCommonalities[biomeDef.defName] = 0;
                                    }
                                }
                            }
                        }
                        else
                        {
                            World_ExposeData_Patch.worldGenerationData = new WorldGenerationData();
                            World_ExposeData_Patch.worldGenerationData.Init();
                        }
                    }
                    
                    PlanetLayerSettingsDefOf.Surface.settings.subdivisions = preset.myLittlePlanetSubcount;
                    ModCompatibilityHelper.TrySetMLPSubcount(preset.myLittlePlanetSubcount);
                }
                currentY += rowHeight;
            }
            Widgets.EndScrollView();
        }

        public bool IsValid(WorldPreset preset, out string failReason)
        {
            failReason = null;
            var sb = new HashSet<string>();
            if (preset.customizationDefaults != null)
            {
                foreach (var kvp in preset.customizationDefaults)
                {
                    var def = kvp.Key.ToDef<ThingDef>();
                    if (def == null)
                    {
                        sb.Add("WB_ThingNotFound".Translate(kvp.Key));
                    }
                }
            }
            if (preset.factionSettlementCustomizationDefaults != null)
            {
                foreach (var kvp in preset.factionSettlementCustomizationDefaults)
                {
                    var def = kvp.Key.ToDef<FactionDef>();
                    if (def == null)
                    {
                        sb.Add("WB_FactionNotFound".Translate(kvp.Key));
                    }
                }
            }
            if (preset.savedFactionDefs != null)
            {
                foreach (var factionDefName in preset.savedFactionDefs)
                {
                    var def = factionDefName.ToDef<FactionDef>();
                    if (def == null)
                    {
                        sb.Add("WB_FactionNotFound".Translate(factionDefName));
                    }
                }
            }
            if (preset.factionNameOverrides != null)
            {
                foreach (var kvp in preset.factionNameOverrides)
                {
                    var def = kvp.Key.ToDef<FactionDef>();
                    if (def == null)
                    {
                        sb.Add("WB_FactionNotFound".Translate(kvp.Key));
                    }
                }
            }
            if (preset.factionDescriptionOverrides != null)
            {
                foreach (var kvp in preset.factionDescriptionOverrides)
                {
                    var def = kvp.Key.ToDef<FactionDef>();
                    if (def == null)
                    {
                        sb.Add("WB_FactionNotFound".Translate(kvp.Key));
                    }
                }
            }

            if (preset.biomes != null)
            {
                foreach (var biomeDefName in preset.biomes)
                {
                    var def = biomeDefName.ToDef<BiomeDef>();
                    if (def == null)
                    {
                        sb.Add("WB_BiomeNotFound".Translate(biomeDefName));
                    }
                }
            }
            if (preset.landmarks != null)
            {
                foreach (var landmarkDefName in preset.landmarks)
                {
                    var def = landmarkDefName.ToDef<LandmarkDef>();
                    if (def == null)
                    {
                        sb.Add("WB_LandmarkNotFound".Translate(landmarkDefName));
                    }
                }
            }
            if (preset.features != null)
            {
                foreach (var featureDefName in preset.features)
                {
                    var def = featureDefName.ToDef<TileMutatorDef>();
                    if (def == null)
                    {
                        sb.Add("WB_FeatureNotFound".Translate(featureDefName));
                    }
                }
            }
            if (preset.scenPartDefs != null)
            {
                foreach (var scenPartDefName in preset.scenPartDefs)
                {
                    var def = scenPartDefName.ToDef<ScenPartDef>();
                    if (def == null)
                    {
                        sb.Add("WB_ScenPartNotFound".Translate(scenPartDefName));
                    }
                }
            }
            if (sb.Any())
            {
                failReason = sb.ToLineList();
                return false;
            }
            return true;
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
                name = selectedPreset.Label;
                description = selectedPreset.description ?? "WB_CommonNoDescription".Translate();
                flavorImage = GetTexture(selectedPreset.FlavorImagePath) ?? ExpansionDefOf.Core.BackgroundImage;
            }
            else
            {
                name = "";
                description = "WB_SelectPresetSelectPrompt".Translate();
                flavorImage = ExpansionDefOf.Core.BackgroundImage;
            }
            Widgets.DrawMenuSection(rect);

            float padding = 17f;
            float spacing = 10f;
            var contentRect = rect.ContractedBy(padding);
            float headerHeight = 150f;
            var flavorImageHeight = CalculateFlavorImageHeight(contentRect.width, flavorImage);
            float factionInfoHeight = contentRect.height - headerHeight - flavorImageHeight - (spacing * 2);
            if (factionInfoHeight < 0) factionInfoHeight = 0;
            var headerRect = new Rect(contentRect.x, contentRect.y, contentRect.width, headerHeight);
            var factionInfoRect = new Rect(contentRect.x, headerRect.yMax + spacing, contentRect.width, factionInfoHeight);
            var flavorImageRect = new Rect(rect.x, factionInfoRect.yMax + spacing, rect.width, flavorImageHeight);
            DrawHeaderSection(headerRect, name, description);
            DrawFactionAndInfoSection(factionInfoRect);
            DrawFlavorImageSection(flavorImageRect, flavorImage);
        }
        private void DrawHeaderSection(Rect rect, string name, string description)
        {
            float nameHeight = 30f;
            var nameRect = new Rect(rect.x, rect.y, rect.width, nameHeight);
            Text.Font = GameFont.Medium;
            Widgets.Label(nameRect, name);
            Text.Font = GameFont.Small;

            var descriptionRect = new Rect(rect.x, nameRect.yMax, rect.width, rect.height - nameHeight);
            float viewWidth = descriptionRect.width - 16f;
            var viewHeight = Text.CalcHeight(description, viewWidth);
            var viewRect = new Rect(0f, 0f, viewWidth, viewHeight);

            Widgets.BeginScrollView(descriptionRect, ref descriptionScrollPosition, viewRect);
            Widgets.Label(viewRect, description);
            Widgets.EndScrollView();
        }
        private void DrawFlavorImageSection(Rect rect, Texture2D flavorImage)
        {
            if (flavorImage != null)
            {
                GUI.DrawTexture(rect, flavorImage, ScaleMode.ScaleToFit);
            }
        }
        private float CalculateFlavorImageHeight(float availableWidth, Texture2D image)
        {
            if (image == null) return 0f;

            float aspectRatio = (float)image.width / image.height;
            float calculatedHeight = availableWidth / aspectRatio;
            float maxHeight = 275f;
            return Mathf.Min(calculatedHeight, maxHeight);
        }

        private Vector2 factionListScrollPosition = Vector2.zero;
        private void DrawFactionAndInfoSection(Rect rect)
        {
            var factions = selectedPreset.savedFactionDefs?.ToDefs<FactionDef>() ?? createWorldParamsPage.factions;
            factions = factions.Where(f => f.displayInFactionSelection).ToList();
            var factionRect = new Rect(rect.x, rect.y, rect.width * 0.3f - 5f, rect.height);
            var infoRect = new Rect(factionRect.xMax + 10f, rect.y, rect.width - factionRect.width - 10f, rect.height);
            float factionLineHeight = 22f;
            var factionViewRect = new Rect(0, 0, factionRect.width - 16f, factions.Count * factionLineHeight);
            Widgets.BeginScrollView(factionRect, ref factionListScrollPosition, factionViewRect);
            for (int i = 0; i < factions.Count; i++)
            {
                FactionDef faction = factions[i];
                var entryRect = new Rect(0f, i * factionLineHeight, factionViewRect.width, factionLineHeight);
                var iconRect = new Rect(entryRect.x, entryRect.y, entryRect.height, entryRect.height);
                Widgets.DefIcon(iconRect, faction);

                var labelRect = new Rect(iconRect.xMax + 4f, entryRect.y, entryRect.width - iconRect.width - 4f, entryRect.height);
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, faction.LabelCap);
                Text.Anchor = TextAnchor.UpperLeft;
            }
            Widgets.EndScrollView();
            float infoLineHeight = 24f;
            float labelWidth = Text.CalcSize("WB_PlanetType".Translate() + ": ").x;
            float currentInfoY = infoRect.y;
            Widgets.Label(new Rect(infoRect.x, currentInfoY, labelWidth, infoLineHeight), "WB_PlanetType".Translate() + ":");
            var planetType = selectedPreset?.planetType;
            if (planetType.NullOrEmpty())
            {
                planetType = "WB_RimWorld".Translate();
            }
            Widgets.Label(new Rect(infoRect.x + labelWidth, currentInfoY, infoRect.width - labelWidth, infoLineHeight), planetType);
            currentInfoY += infoLineHeight;
            if (ModsConfig.IsActive(ModCompatibilityHelper.WorldTechLevelPackageId))
            {
                ModCompatibilityHelper.TryGetWTL(out var techLevel);
                if (selectedPreset.saveWorldTechLevel && techLevel != selectedPreset.worldTechLevel)
                {
                    ModCompatibilityHelper.TryGetWTLUnrestricted(out var wasUnrestricted);
                    ModCompatibilityHelper.TrySetWTLUnrestricted(false);
                    ModCompatibilityHelper.TrySetWTL(selectedPreset.worldTechLevel);
                    ModCompatibilityHelper.TryGetWTL(out techLevel);
                    ResearchUtility_InitialResearchLevelFor_Patch.preset = selectedPreset;
                    createWorldParamsPage.ResetFactionCounts();
                    ResearchUtility_InitialResearchLevelFor_Patch.preset = null;
                    ModCompatibilityHelper.TrySetWTLUnrestricted(wasUnrestricted);
                }
                else if (!selectedPreset.saveWorldTechLevel && techLevel != TechLevel.Archotech)
                {
                    ModCompatibilityHelper.TryGetWTLUnrestricted(out var wasUnrestricted);
                    ModCompatibilityHelper.TrySetWTL(TechLevel.Archotech);
                    ModCompatibilityHelper.TryGetWTL(out techLevel);
                    ResearchUtility_InitialResearchLevelFor_Patch.preset = selectedPreset;
                    createWorldParamsPage.ResetFactionCounts();
                    ResearchUtility_InitialResearchLevelFor_Patch.preset = null;
                    ModCompatibilityHelper.TrySetWTLUnrestricted(wasUnrestricted);
                }

                var techLevelLabel = techLevel.ToStringHuman();
                if (techLevelLabel == TechLevel.Undefined.ToStringHuman() || techLevelLabel == TechLevel.Archotech.ToStringHuman())
                {
                    techLevelLabel = "WB_Unrestricted".Translate();
                }
                techLevelLabel = techLevelLabel.CapitalizeFirst();
                Widgets.Label(new Rect(infoRect.x, currentInfoY, 200, infoLineHeight), "WB_TechLevel".Translate() + ": " + techLevelLabel);
                currentInfoY += infoLineHeight;
            }
            labelWidth = Text.CalcSize("Difficulty".Translate() + ": ").x;
            var difficultyRect = new Rect(infoRect.x, currentInfoY, labelWidth, infoLineHeight);
            Widgets.Label(difficultyRect, "Difficulty".Translate() + ": ");
            var starRect = new Rect(difficultyRect.xMax, currentInfoY, 20f, 20f);
            var difficulty = selectedPreset?.difficulty ?? 2;
            if (selectedPreset == defaultPreset) difficulty = 2;
            for (var i = 0; i < 5; i++)
            {
                GUI.DrawTexture(starRect, EmptyStarIcon);
                starRect.x += starRect.width;
            }
            starRect.x = difficultyRect.xMax;
            for (int i = 0; i < difficulty; i++)
            {
                GUI.DrawTexture(starRect, StarIcon);
                starRect.x += starRect.width;
            }
        }

        public static readonly Texture2D StarIcon = ContentFinder<Texture2D>.Get("Worldbuilder/UI/Star");
        public static readonly Texture2D EmptyStarIcon = ContentFinder<Texture2D>.Get("Worldbuilder/UI/NoStar");

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
            createWorldParamsPage.prev = this;
            base.DoNext();
            if (selectedPreset != null && selectedPreset.savedFactionDefs.NullOrEmpty() is false)
            {
                createWorldParamsPage.factions = selectedPreset.savedFactionDefs.ToDefs<FactionDef>();
            }
            next = oldNext;
        }

        private void SetSelectedPresetName()
        {
            if (selectedPreset == defaultPreset)
            {
                World_ExposeData_Patch.WorldPresetName = null;
            }
            else
            {
                World_ExposeData_Patch.WorldPresetName = selectedPreset?.name;
            }
        }
        private void GenerateWorldAndProceed()
        {
            WorldGeneratorUtility.GenerateWorldFromPreset(selectedPreset, delegate
            {
                Find.WindowStack.Add(this.createWorldParamsPage.next);
                Close();
            });
        }

        private Texture2D GetTexture(string path)
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
                var fileData = File.ReadAllBytes(path);
                var newTex = new Texture2D(2, 2);
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
            var backRect = new Rect(rect.x, y, buttonWidth, BottomButHeight);
            if (Widgets.ButtonText(backRect, "Back".Translate()) && CanDoBack())
            {
                DoBack();
            }
            var configureRect = new Rect(middleX - buttonWidth / 2f, y, buttonWidth, BottomButHeight);
            if (Widgets.ButtonText(configureRect, "WB_SelectPresetConfigurePlanetButton".Translate()))
            {
                DoConfigurePlanet();
            }
            var nextRect = new Rect(rect.xMax - buttonWidth, y, buttonWidth, BottomButHeight);
            if (Widgets.ButtonText(nextRect, "Next".Translate()))
            {
                if (selectedPreset == null || selectedPreset == defaultPreset || !selectedPreset.saveTerrain)
                {
                    DoConfigurePlanet();
                }
                else
                {
                    DoNextSkipConfigure();
                }
            }
        }
    }
}
