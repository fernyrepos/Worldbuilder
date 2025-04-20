using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using UnityEngine;
using VanillaFurnitureExpanded;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public class Window_ThingCustomization : Window
    {
        private List<Thing> things;
        private ThingDef thingDef;
        private CustomizationData customizationData;
        private List<ThingStyleDef> availableStyles;
        private CompProperties_RandomBuildingGraphic graphicProps;
        private int currentTab = 0;
        private int currentAppearanceTab = 0;
        private List<Color> cachedColors = new List<Color>();
        public override Vector2 InitialSize => new Vector2(825, 675);
        private Vector2 scrollPosition = Vector2.zero;
        private float buttonWidth = 150f;
        private float buttonHeight = 32f;
        public Window_ThingCustomization(List<Thing> things, CustomizationData customizationData)
        {
            this.things = things;
            this.thingDef = things.First().def;
            var thing = things.First();

            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.preventCameraMotion = false;
            this.availableStyles = GetAvailableStylesForThing();
            this.closeOnAccept = false;
            if (customizationData is null)
            {
                customizationData = CreateCustomization(thing);
            }
            else
            {
                this.customizationData = customizationData.Copy();
            }

            cachedColors = DefDatabase<ColorDef>.AllDefsListForReading.Select((ColorDef c) => c.color).ToList();
            cachedColors.AddRange(Find.FactionManager.AllFactionsVisible.Select((Faction f) => f.Color));
            cachedColors.SortByColor((Color c) => c);
            graphicProps = thingDef.GetCompProperties<CompProperties_RandomBuildingGraphic>();
            if (graphicProps is null)
            {
                this.currentAppearanceTab = 1;
            }
            if (availableStyles.Any(x => x is not null) is false && this.currentAppearanceTab == 1)
            {
                this.currentAppearanceTab = 2;
            }
        }

        private CustomizationData CreateCustomization(Thing thing)
        {
            CustomizationData customizationData = new CustomizationData();
            customizationData.originalStyleDef = thing.StyleDef;
            customizationData.color = null;
            customizationData.styleDef = thing.StyleDef;
            customizationData.labelOverride = thing.def.label;
            customizationData.descriptionOverride = thing.DescriptionFlavor;
            customizationData.narrativeText = "";
            var comp = thing.TryGetComp<CompRandomBuildingGraphic>();
            if (comp != null)
            {
                Graphic graphic = ((Graphic)comp.newGraphic ?? comp.newGraphicSingle);
                if (graphic != null)
                {
                    customizationData.variationIndex = comp.Props.randomGraphics.IndexOf(graphic.path);
                }
            }
            this.customizationData = customizationData;
            return customizationData;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            var offset = 32f;
            Rect tabAreaRect = new Rect(inRect.x, inRect.y + offset, inRect.width, offset);
            Rect contentRect = new Rect(inRect.x, inRect.y + offset, inRect.width, inRect.height - offset);

            List<TabRecord> tabsList = new List<TabRecord>();
            tabsList.Add(new TabRecord("WB_CustomizeAppearance".Translate(), delegate { currentTab = 0; }, currentTab == 0));
            tabsList.Add(new TabRecord("WB_CustomizeDetail".Translate(), delegate { currentTab = 1; }, currentTab == 1));
            tabsList.Add(new TabRecord("WB_CustomizeNarrative".Translate(), delegate { currentTab = 2; }, currentTab == 2));
            TabDrawer.DrawTabs(tabAreaRect, tabsList, maxTabWidth: 300);
            switch (currentTab)
            {
                case 0:
                    DrawAppearanceTab(contentRect);
                    break;
                case 1:
                    DrawDetailTab(contentRect);
                    break;
                case 2:
                    DrawNarrativeTab(contentRect);
                    break;
            }

            int numButtons = 5;
            float totalButtonWidth = buttonWidth * numButtons;
            float totalSpacing = inRect.width - totalButtonWidth;
            float buttonSpacing = totalSpacing / (numButtons - 1);
            float currentButtonX = inRect.x;

            Rect factionButtonRect = new Rect(currentButtonX, inRect.yMax - buttonHeight, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(factionButtonRect, "WB_CustomizeFaction".Translate()))
            {
                List<FloatMenuOption> factionOptions = new List<FloatMenuOption>();
                factionOptions.Add(new FloatMenuOption("WB_CustomizeSaveFactionDefault".Translate(), () =>
                {
                    Dialog_MessageBox confirmationDialog = Dialog_MessageBox.CreateConfirmation(
                        $"Are you sure you want to save as player faction default? This will apply these customizations to all future player-owned {thingDef.label} and all existing player-owned {thingDef.label}.",
                        () =>
                        {
                            CustomizationDataCollections.playerDefaultCustomizationData[thingDef] = customizationData;
                            Close();
                        }
                    );
                    Find.WindowStack.Add(confirmationDialog);
                }));
                factionOptions.Add(new FloatMenuOption("WB_CustomizeResetFactionDefault".Translate(thingDef.label), () =>
                {
                    if (CustomizationDataCollections.playerDefaultCustomizationData.ContainsKey(thingDef))
                    {
                        CustomizationDataCollections.playerDefaultCustomizationData.Remove(thingDef);
                        Messages.Message($"Player faction default for {thingDef.label} reset.", MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Messages.Message($"No player faction default set for {thingDef.label}.", MessageTypeDefOf.NeutralEvent);
                    }
                }));
                Find.WindowStack.Add(new FloatMenu(factionOptions));
            }
            currentButtonX += buttonWidth + buttonSpacing;

            Rect worldButtonRect = new Rect(currentButtonX, inRect.yMax - buttonHeight, buttonWidth, buttonHeight);
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
            currentButtonX += buttonWidth + buttonSpacing;

            Rect saveThingButtonRect = new Rect(currentButtonX, inRect.yMax - buttonHeight, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(saveThingButtonRect, "WB_CustomizeSave".Translate()))
            {
                foreach (var thing in things)
                {
                    CustomizationDataCollections.thingCustomizationData[thing] = customizationData.Copy();
                    customizationData.SetGraphic(thing);
                }
                Messages.Message($"Customization saved for selected {thingDef.label}(s).", MessageTypeDefOf.PositiveEvent);
            }
            currentButtonX += buttonWidth + buttonSpacing;

            Rect mapButtonRect = new Rect(currentButtonX, inRect.yMax - buttonHeight, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(mapButtonRect, "WB_CustomizeMap".Translate()))
            {
                List<FloatMenuOption> mapOptions = new List<FloatMenuOption>();
                mapOptions.Add(new FloatMenuOption("WB_CustomizeSaveMapAll".Translate(thingDef.label), () =>
                {
                    Dialog_MessageBox confirmationDialog = Dialog_MessageBox.CreateConfirmation(
                        $"Are you sure you want to save this customization to all instances of {thingDef.label} on the current map?",
                        () =>
                        {
                            foreach (var thing in Find.CurrentMap.listerThings.ThingsOfDef(thingDef))
                            {
                                CustomizationDataCollections.thingCustomizationData[thing] = customizationData.Copy();
                            }
                            Close();
                            ApplyCustomizationsToMaps();
                        }
                    );
                    Find.WindowStack.Add(confirmationDialog);
                }));
                Find.WindowStack.Add(new FloatMenu(mapOptions));
            }
            currentButtonX += buttonWidth + buttonSpacing;

            Rect resetButtonRect = new Rect(currentButtonX, inRect.yMax - buttonHeight, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(resetButtonRect, "WB_CustomizeResetThing".Translate()))
            {
                foreach (var thing in things)
                {
                    CustomizationDataCollections.thingCustomizationData.Remove(thing);
                    thing.StyleDef = customizationData.originalStyleDef;
                    thing.graphicInt = null;
                    thing.UpdateGraphic();
                }
                customizationData = CreateCustomization(things.First());
                Messages.Message("WB_CustomizeResetThingSuccess".Translate(thingDef.label), MessageTypeDefOf.PositiveEvent);
            }
        }

        private void DrawAppearanceTab(Rect tabRect)
        {
            DisplayPreview(tabRect, out var tabWidth, out var previewImageRect, out var currentY);
            Rect tabsRect = new Rect(tabRect.x, currentY, tabWidth, 32);
            if (graphicProps != null)
            {
                if (Widgets.ButtonText(tabsRect, "WB_CustomizeVariations".Translate()))
                {
                    currentAppearanceTab = 0;
                }
                tabsRect.y += tabsRect.height;
            }
            if (availableStyles.Count(x => x is not null) > 0)
            {
                if (Widgets.ButtonText(tabsRect, "WB_CustomizeCulturalStyles".Translate()))
                {
                    currentAppearanceTab = 1;
                }
                tabsRect.y += tabsRect.height;
            }

            if (Widgets.ButtonText(tabsRect, "WB_CustomizeUploadImage".Translate()))
            {
                var fileSelector = new Dialog_FileSelector();
                fileSelector.onSelectAction = (string filePath) =>
                {
                    customizationData.selectedImagePath = filePath;
                    customizationData.color = Color.white;
                    customizationData.styleDef = null;
                    customizationData.variationIndex = null;
                };
                Find.WindowStack.Add(fileSelector);
            }

            float gridStartX = tabsRect.xMax + 10f;
            currentY = previewImageRect.y;
            float thumbnailSize = 120;
            float spacing = 15f;
            float ySpacing = 30;
            int thumbnailsPerRow = 4;
            float extraPadding = 0f;
            Rect gridRect = new Rect(gridStartX, previewImageRect.y, tabRect.width - tabWidth - 10f, tabRect.height - previewImageRect.y - 40f);

            bool hasVariations = graphicProps != null && graphicProps.randomGraphics != null && graphicProps.randomGraphics.Count > 0;
            bool hasStyles = availableStyles.Any(s => s != null);
            bool hasCustomImage = !string.IsNullOrEmpty(customizationData.selectedImagePath);

            bool showOnlyDefaultReset = hasCustomImage && !hasVariations && !hasStyles;

            if (showOnlyDefaultReset)
            {
                DrawDefaultResetThumbnail(thumbnailSize, spacing, extraPadding, gridRect);
            }
            else if (currentAppearanceTab == 0)
            {
                DrawVariations(thumbnailSize, spacing, ySpacing, thumbnailsPerRow, extraPadding, gridRect);
            }
            else if (currentAppearanceTab == 1)
            {
                DrawStyles(thumbnailSize, spacing, ySpacing, thumbnailsPerRow, extraPadding, gridRect);
            }
            currentY = tabsRect.yMax + 15;
            Rect colorSectionRect = new Rect(tabsRect.x, currentY, tabsRect.width, 30f);
            var colorBlock = new Rect(colorSectionRect.x, currentY, 50, 50);
            bool enableColoring = customizationData.color.HasValue;
            Widgets.DrawBoxSolid(colorBlock, enableColoring ? customizationData.color.Value : Color.clear);
            currentY -= 5f;
            Widgets.CheckboxLabeled(new Rect(colorBlock.xMax + 5, currentY, colorSectionRect.width - colorBlock.width, 30f), "WB_CustomizeEnableColoring".Translate(), ref enableColoring);
            currentY += 30f;
            if (enableColoring && customizationData.color.HasValue is false)
            {
                customizationData.color = Color.white;
            }
            else if (enableColoring is false && customizationData.color.HasValue)
            {
                customizationData.color = null;
            }

            if (Widgets.ButtonText(new Rect(colorBlock.xMax + 5, currentY, colorSectionRect.width - colorBlock.width - 5, 30f), "WB_CustomizeSetColor".Translate()))
            {
                Find.WindowStack.Add(new Dialog_ChooseColor(
                    "WB_CustomizeChooseColor".Translate(),
                    Color.white,
                    cachedColors,
                    delegate (Color color)
                    {
                        customizationData.color = color;
                    }));
            }
            if (Prefs.DevMode)
            {
                currentY += 35f;
                Text.Font = GameFont.Tiny;
                var currentGraphic = customizationData.GetGraphic(things.First());
                string graphicClass = currentGraphic?.GetType().Name ?? "null";
                string debugText = $"Color: {(customizationData.color.HasValue ? customizationData.color.Value.ToString() : "null")}\n" +
                                   $"Style: {(customizationData.styleDef?.defName ?? "null")}\n" +
                                   $"Variation: {(customizationData.variationIndex.HasValue ? customizationData.variationIndex.Value.ToString() : "null")}\n" +
                                   $"Image Path: {(customizationData.selectedImagePath ?? "null")}\n" +
                                   $"Graphic Class: {graphicClass}";
                Rect debugRect = new Rect(tabsRect.x, currentY, tabsRect.width, 100f);
                Widgets.Label(debugRect, debugText);
            }
            Text.Font = GameFont.Small;
        }

        private void DrawStyles(float thumbnailSize, float spacing, float ySpacing, int thumbnailsPerRow, float extraPadding, Rect gridRect)
        {
            int numberOfRows = Mathf.CeilToInt((float)availableStyles.Count / thumbnailsPerRow);
            float totalGridHeight = thumbnailSize * numberOfRows + ySpacing * (numberOfRows - 1) + extraPadding * 2;
            totalGridHeight += 24;
            Rect viewRect = new Rect(0, 0, gridRect.width - 16, totalGridHeight);
            Widgets.BeginScrollView(gridRect, ref scrollPosition, viewRect);

            int styleIndex = 0;
            float thumbnailStartY = extraPadding;

            for (int row = 0; row < numberOfRows; row++)
            {
                for (int col = 0; col < thumbnailsPerRow; col++)
                {
                    if (styleIndex < availableStyles.Count)
                    {
                        ThingStyleDef styleDef = availableStyles[styleIndex];
                        Rect thumbnailRect = new Rect(
                            spacing + col * (thumbnailSize + spacing),
                            thumbnailStartY + row * (thumbnailSize + ySpacing),
                            thumbnailSize,
                            thumbnailSize
                        );
                        Widgets.DrawMenuSection(thumbnailRect);
                        Widgets.DefIcon(thumbnailRect.ContractedBy(5), thingDef, null, thingStyleDef: styleDef,
                        color: customizationData.color, scale: 0.7f);
                        if (Widgets.ButtonInvisible(thumbnailRect))
                        {
                            customizationData.styleDef = styleDef;
                        }
                        if (customizationData.styleDef == styleDef)
                        {
                            Widgets.DrawHighlight(thumbnailRect);
                        }
                        var styleName = styleDef != null ? DefDatabase<StyleCategoryDef>.AllDefs
                            .First(x => x.thingDefStyles.Any(y => y.styleDef == styleDef)).label.CapitalizeFirst() : "Default";
                        Text.Font = GameFont.Small;
                        var labelBox = new Rect(thumbnailRect.x, thumbnailRect.yMax, thumbnailRect.width, 24);
                        Text.Anchor = TextAnchor.MiddleCenter;
                        Widgets.DrawBoxSolid(labelBox, new ColorInt(94, 93, 93).ToColor);
                        Widgets.Label(labelBox, styleName);
                        Text.Anchor = TextAnchor.UpperLeft;
                        Text.Font = GameFont.Small;
                    }
                    styleIndex++;
                }
            }
            Widgets.EndScrollView();
        }

        private void DrawDefaultResetThumbnail(float thumbnailSize, float spacing, float extraPadding, Rect gridRect)
        {
            Rect viewRect = new Rect(0, 0, gridRect.width - 16, thumbnailSize + 24 + extraPadding * 2);
            Widgets.BeginScrollView(gridRect, ref scrollPosition, viewRect);

            var thing = things.First();
            float currentX = spacing;
            float currentGridY = extraPadding;

            Rect defaultThumbnailRect = new Rect(currentX, currentGridY, thumbnailSize, thumbnailSize);
            Widgets.DrawMenuSection(defaultThumbnailRect);
            if (thingDef.graphic is Graphic_Linked || thing is Building_Door && customizationData.styleDef != null)
            {
                Widgets.DefIcon(defaultThumbnailRect.ContractedBy(5), thingDef, null, thingStyleDef: customizationData.styleDef,
                    color: customizationData.color, scale: 0.7f);
            }
            else
            {
                var defaultGraphic = customizationData.DefaultGraphic(thing);
                Texture textureToDraw = GetStableTextureForGraphic(defaultGraphic, thing);
                Widgets.ThingIconWorker(defaultThumbnailRect.ContractedBy(5), thingDef, textureToDraw, 0);
            }

            if (Widgets.ButtonInvisible(defaultThumbnailRect))
            {
                customizationData.selectedImagePath = null;
                customizationData.styleDef = null;
                customizationData.variationIndex = null;
                customizationData.color = null;
                currentAppearanceTab = -1;
                foreach (var thing2 in things)
                {
                    thing2.graphicInt = null;
                }
            }

            Widgets.DrawHighlight(defaultThumbnailRect);
            DrawLabelBelowThumbnail(defaultThumbnailRect, "WB_CustomizeDefault".Translate());
            Widgets.EndScrollView();
        }

        private void DrawLabelBelowThumbnail(Rect thumbnailRect, string label)
        {
            Text.Font = GameFont.Small;
            var labelBox = new Rect(thumbnailRect.x, thumbnailRect.yMax + 2, thumbnailRect.width, 22);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(labelBox, label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private Texture GetStableTextureForGraphic(Graphic graphic, Thing thing)
        {
            Texture textureToDraw = null;
            if (graphic != null)
            {
                if (graphic is Graphic_Random graphicRandom)
                {
                    textureToDraw = graphicRandom.SubGraphicFor(thing)?.MatSouth?.mainTexture;
                }
                else
                {
                    textureToDraw = graphic.MatSouth?.mainTexture;
                }
            }
            if (textureToDraw == null)
            {
                textureToDraw = BaseContent.BadTex;
            }
            return textureToDraw;
        }


        private void DrawVariations(float thumbnailSize, float spacing, float ySpacing, int thumbnailsPerRow, float extraPadding, Rect gridRect)
        {
            if (graphicProps == null || graphicProps.randomGraphics == null || graphicProps.randomGraphics.Count == 0)
            {
                Widgets.Label(gridRect, "No variations available.");
                return;
            }

            int numberOfRows = Mathf.CeilToInt((float)graphicProps.randomGraphics.Count / thumbnailsPerRow);
            float totalGridHeight = thumbnailSize * numberOfRows + ySpacing * (numberOfRows - 1) + extraPadding * 2;
            totalGridHeight += 24;
            Rect viewRect = new Rect(0, 0, gridRect.width - 16, totalGridHeight);
            Widgets.BeginScrollView(gridRect, ref scrollPosition, viewRect);
            int variationIndex = 0;
            float thumbnailStartY = extraPadding;

            for (int row = 0; row < numberOfRows; row++)
            {
                for (int col = 0; col < thumbnailsPerRow; col++)
                {
                    if (variationIndex < graphicProps.randomGraphics.Count)
                    {
                        Rect thumbnailRect = new Rect(
                            spacing + col * (thumbnailSize + spacing),
                            thumbnailStartY + row * (thumbnailSize + ySpacing),
                            thumbnailSize,
                            thumbnailSize
                        );
                        Widgets.DrawMenuSection(thumbnailRect);
                        string graphicPath = graphicProps.randomGraphics[variationIndex];
                        var thing = things.First();
                        Graphic variationGraphic;
                        if (thingDef.graphicData.graphicClass == typeof(Graphic_Multi))
                        {
                            variationGraphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>(graphicPath, ShaderTypeDefOf.Cutout.Shader, thing.Graphic.drawSize, thing.Graphic.color);
                        }
                        else
                        {
                            variationGraphic = (Graphic_Single)GraphicDatabase.Get<Graphic_Single>(graphicPath, ShaderTypeDefOf.Cutout.Shader, thing.Graphic.drawSize, thing.Graphic.color);
                        }
                        GUI.color = customizationData.color ?? Color.white;
                        Widgets.ThingIconWorker(thumbnailRect.ContractedBy(5), thingDef, GetStableTextureForGraphic(variationGraphic, thing), 0);
                        GUI.color = Color.white;
                        if (Widgets.ButtonInvisible(thumbnailRect))
                        {
                            customizationData.variationIndex = variationIndex;
                            customizationData.styleDef = null;
                            customizationData.selectedImagePath = null;
                        }
                        if (customizationData.variationIndex == variationIndex)
                        {
                            Widgets.DrawHighlight(thumbnailRect);
                        }
                        var variationName = $"Variation {variationIndex + 1}";
                        Text.Font = GameFont.Small;
                        var labelBox = new Rect(thumbnailRect.x, thumbnailRect.yMax, thumbnailRect.width, 24);
                        Text.Anchor = TextAnchor.MiddleCenter;
                        Widgets.DrawBoxSolid(labelBox, new ColorInt(94, 93, 93).ToColor);
                        Widgets.Label(labelBox, variationName);
                        Text.Anchor = TextAnchor.UpperLeft;
                        Text.Font = GameFont.Small;
                    }
                    variationIndex++;
                }
            }
            Widgets.EndScrollView();
        }

        private void DisplayPreview(Rect tabRect, out float previewWidth, out Rect previewImageRect, out float currentY)
        {
            previewWidth = 200f;
            previewImageRect = new Rect(tabRect.x, tabRect.y + 15, previewWidth, previewWidth);
            Widgets.DrawMenuSection(previewImageRect);
            var previewThingRect = previewImageRect.ContractedBy(previewImageRect.width * 0.05f);
            var thing = things.First();
            var old = thing.graphicInt;
            var oldStyle = thing.StyleDef;
            thing.StyleDef = customizationData.styleDef;
            thing.graphicInt = customizationData.GetGraphic(thing);
            GUI.color = customizationData.color ?? Color.white;
            Texture textureToDraw = GetStableTextureForGraphic(thing.graphicInt, thing);

            if (customizationData.selectedImagePath.NullOrEmpty() is false)
            {
                var oldScale = thing.def.uiIconScale;
                thing.def.uiIconScale = 1f;
                var oldPath = thing.def.uiIconPath;
                var oldFlags = thing.def.graphicData.linkFlags;
                thing.def.graphicData.linkFlags = LinkFlags.None;
                thing.def.uiIconPath = null;
                Widgets.ThingIconWorker(previewThingRect, thing.def, textureToDraw, 0f);
                thing.def.uiIconScale = oldScale;
                thing.def.uiIconPath = oldPath;
                thing.def.graphicData.linkFlags = oldFlags;
            }
            else if (thingDef.graphic is Graphic_Linked || thing is Building_Door && customizationData.styleDef != null)
            {
                Widgets.DefIcon(previewThingRect, thingDef, null, thingStyleDef: customizationData.styleDef,
                    color: customizationData.color, scale: 0.7f);
            }
            else
            {
                Widgets.ThingIconWorker(previewThingRect, thing.def, textureToDraw, 0f);
            }
            GUI.color = Color.white;
            thing.graphicInt = old;
            thing.StyleDef = oldStyle;
            currentY = previewImageRect.yMax + 15;
        }

        private void DrawDetailTab(Rect tabRect)
        {
            DisplayPreview(tabRect, out var tabWidth, out var previewImageRect, out var currentY);
            var explanationTextBox = new Rect(tabRect.x, currentY, tabWidth, 100f);
            Widgets.Label(explanationTextBox, "WB_CustomizeDetailTabExplanation".Translate());
            currentY = tabRect.y + 15;
            float lineHeight = 30f;
            tabRect.xMin += tabWidth + 15;
            Widgets.Label(new Rect(tabRect.x, currentY, 100f, lineHeight), "WB_CustomizeLabel".Translate());
            Rect labelEditRect = new Rect(tabRect.x + 100f, currentY, tabRect.width - 100f - 70f, lineHeight);
            customizationData.labelOverride = Widgets.TextField(labelEditRect, customizationData.labelOverride);

            Rect resetLabelButtonRect = new Rect(labelEditRect.xMax + 5f, currentY, 65f, lineHeight);
            if (Widgets.ButtonText(resetLabelButtonRect, "Reset".Translate()))
            {
                customizationData.labelOverride = thingDef.label;
            }
            currentY += lineHeight + 10f;
            Widgets.CheckboxLabeled(new Rect(tabRect.x, currentY, tabRect.width - 100f - 70f - 65f - 10f, lineHeight), "WB_CustomizeIncludeAdditionalDetails".Translate(), ref customizationData.includeAdditionalDetails);
            currentY += lineHeight + 10f;
            Widgets.Label(new Rect(tabRect.x, currentY, 100f, lineHeight), "WB_CustomizeDescription".Translate());
            Rect descriptionEditRect = new Rect(tabRect.x + 100f, currentY, tabRect.width - 100f - 70f, 100f);
            customizationData.descriptionOverride = Widgets.TextArea(descriptionEditRect, customizationData.descriptionOverride);

            Rect resetDescriptionButtonRect = new Rect(descriptionEditRect.xMax + 5f, currentY, 65f, lineHeight);
            if (Widgets.ButtonText(resetDescriptionButtonRect, "Reset".Translate()))
            {
                customizationData.descriptionOverride = null;
                customizationData.descriptionOverride = things.First().DescriptionFlavor;
            }
        }

        private Vector2 narrativeScrollPosition = Vector2.zero;
        private void DrawNarrativeTab(Rect tabRect)
        {
            DisplayPreview(tabRect, out var tabWidth, out var previewImageRect, out var currentY);
            var explanationTextBox = new Rect(tabRect.x, currentY, tabWidth, 100f);
            Widgets.Label(explanationTextBox, "WB_CustomizeNarrativeTabExplanation".Translate());

            Rect narrativeEditRect = new Rect(tabRect.x + tabWidth + 15, tabRect.y + 15, tabRect.width - tabWidth - 15, tabRect.height - 60);
            customizationData.narrativeText = DevGUI.TextAreaScrollable(narrativeEditRect, customizationData.narrativeText, ref narrativeScrollPosition);
        }

        private List<ThingStyleDef> GetAvailableStylesForThing()
        {
            List<ThingStyleDef> styles = new List<ThingStyleDef>();
            styles.Add(null);
            foreach (StyleCategoryDef categoryDef in DefDatabase<StyleCategoryDef>.AllDefs)
            {
                ThingStyleDef style = categoryDef.GetStyleForThingDef(thingDef);
                if (style != null)
                {
                    styles.Add(style);
                }
            }
            if (!thingDef.randomStyle.NullOrEmpty())
            {
                foreach (ThingStyleChance styleChance in thingDef.randomStyle)
                {
                    if (styleChance.StyleDef.graphicData != null)
                    {
                        styles.Add(styleChance.StyleDef);
                    }
                }
            }
            return styles.Distinct().ToList();
        }

        private void ApplyCustomizationsToMaps()
        {
            foreach (Map map in Find.Maps)
            {
                foreach (Thing thing in map.listerThings.AllThings.Where(t => t.def == this.thingDef))
                {
                    var customizationData = thing.GetCustomizationData();
                    if (customizationData != null)
                    {
                        customizationData.SetGraphic(thing);
                    }
                }
            }
            foreach (var thing in CustomizationDataCollections.explicitlyCustomizedThings)
            {
                if (thing.def == this.thingDef)
                {
                    thing.UpdateGraphic();
                }
            }
        }
        
        private void ShowSaveConfirmationDialog(WorldPreset targetPreset)
        {
            string presetNameForMessage = targetPreset.name;
            Dialog_MessageBox confirmationDialog = Dialog_MessageBox.CreateConfirmation(
               "WB_CustomizeSaveToPresetConfirm".Translate(thingDef.label, presetNameForMessage),
               () =>
               {
                   if (targetPreset.customizationDefaults == null)
                   {
                       targetPreset.customizationDefaults = new Dictionary<ThingDef, CustomizationData>();
                   }
                   targetPreset.customizationDefaults[thingDef] = customizationData;
                   bool savedSuccessfully;
                   savedSuccessfully = WorldPresetManager.SavePreset(targetPreset, null, null);

                   if (savedSuccessfully)
                   {
                       Messages.Message("WB_CustomizePresetSaveSuccess".Translate(thingDef.label, presetNameForMessage), MessageTypeDefOf.PositiveEvent);
                       Close();
                   }
                   else
                   {
                       Messages.Message("WB_CustomizePresetSaveFailed".Translate(thingDef.label, presetNameForMessage), MessageTypeDefOf.NegativeEvent);
                   }
               }
           );
            Find.WindowStack.Add(confirmationDialog);
        }
    }
}
