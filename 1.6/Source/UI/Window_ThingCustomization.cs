using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using UnityEngine;
using VEF.Buildings;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    public class Window_ThingCustomization : Window_BaseCustomization
    {
        private List<Thing> things;
        private ThingDef thingDef;
        private List<ThingStyleDef> availableStyles;
        private CompProperties_RandomBuildingGraphic graphicProps;
        private int currentAppearanceTab = 0;
        public override Vector2 InitialSize => new Vector2(825, 675);
        private Vector2 scrollPosition = Vector2.zero;
        private const float buttonWidth = 150f;
        private const float buttonHeight = 32f;
        private static Texture2D circleTexture;
        private static Texture2D smoothLineTexture;

        private bool hasUnsavedChanges = false;
        private CustomizationData originalData;

        public Window_ThingCustomization(List<Thing> things, CustomizationData customizationData)
            : base()
        {
            this.draggable = true;
            this.closeOnClickedOutside = false;
            this.things = things;
            this.thingDef = things.First().def;
            var thing = things.First();

            this.availableStyles = GetAvailableStylesForThing();

            if (customizationData is null)
            {
                this.customizationData = CreateCustomization(thing);
                this.originalData = this.customizationData.Copy();
            }
            else
            {
                this.customizationData = customizationData.Copy();
                this.originalData = customizationData.Copy();
            }

            graphicProps = thingDef.GetCompProperties<CompProperties_RandomBuildingGraphic>();
            if (graphicProps is null)
            {
                currentAppearanceTab = 1;
            }
            if (availableStyles.Any(x => x is not null) is false && currentAppearanceTab == 1)
            {
                currentAppearanceTab = 2;
            }
        }

        private CustomizationData CreateCustomization(Thing thing)
        {
            var customizationData = new CustomizationData();
            customizationData.originalStyleDef = thing.StyleDef;
            customizationData.color = null;
            customizationData.styleDef = thing.StyleDef;
            customizationData.labelOverride = thing.def.label;
            customizationData.descriptionOverride = thing.DescriptionFlavor;
            customizationData.narrativeText = "";
            if (thing.def.MadeFromStuff)
            {
                customizationData.labelOverride += " (" + thing.Stuff.label + ")";
            }
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

        private void ApplyVisualChanges()
        {
            hasUnsavedChanges = true;
            foreach (var thing in things)
            {
                thing.StyleDef = null;
                thing.graphicInt = null;
                thing.styleGraphicInt = null;
                thing.graphicInt = customizationData.GetGraphic(thing);
                thing.styleGraphicInt = thing.graphicInt;
                if (thing.Spawned)
                {
                    thing.DirtyMapMesh(thing.Map);
                }
            }
        }

        public override void Close(bool doCloseSound = true)
        {
            if (hasUnsavedChanges)
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "WB_UnsavedChangesWarning".Translate(),
                    () => {
                        RestoreOriginalState();
                        base.Close(doCloseSound);
                    },
                    destructive: true
                ));
            }
            else
            {
                base.Close(doCloseSound);
            }
        }

        private void RestoreOriginalState()
        {
            foreach (var thing in things)
            {
                thing.StyleDef = originalData.originalStyleDef;
                thing.graphicInt = null;
                thing.styleGraphicInt = null;
                thing.UpdateGraphic();
                if (thing.Spawned)
                {
                    thing.DirtyMapMesh(thing.Map);
                }
            }
        }

        protected override void DrawBottomButtons(Rect inRect)
        {
            int numButtons = 5;
            float totalButtonWidth = buttonWidth * numButtons;
            float totalSpacing = inRect.width - totalButtonWidth;
            float buttonSpacing = totalSpacing / (numButtons - 1);
            float currentButtonX = inRect.x;

            var factionButtonRect = new Rect(currentButtonX, inRect.yMax - buttonHeight, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(factionButtonRect, "WB_CustomizeFaction".Translate()))
            {
                var factionOptions = new List<FloatMenuOption>();
                factionOptions.Add(new FloatMenuOption("WB_CustomizeSaveFactionDefault".Translate(), () =>
                        {
                            var confirmationDialog = Dialog_MessageBox.CreateConfirmation(
                        "WB_ThingCustomizeSaveFactionDefaultConfirm".Translate(thingDef.label),
                        () =>
                        {
                            CustomizationDataCollections.playerDefaultCustomizationData[thingDef] = customizationData;
                            WorldbuilderMod.ApplyCustomizationsToExistingThings();
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
                        Messages.Message("WB_ThingCustomizeFactionDefaultReset".Translate(thingDef.label), MessageTypeDefOf.PositiveEvent);
                    }
                    else
                    {
                        Messages.Message("WB_ThingCustomizeNoFactionDefault".Translate(thingDef.label), MessageTypeDefOf.NeutralEvent);
                    }
                }));
                Find.WindowStack.Add(new FloatMenu(factionOptions));
            }
            currentButtonX += buttonWidth + buttonSpacing;

            var worldButtonRect = new Rect(currentButtonX, inRect.yMax - buttonHeight, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(worldButtonRect, "WB_World".Translate()))
            {
                var worldOptions = new List<FloatMenuOption>();
                foreach (var preset in WorldPresetManager.GetAllPresets())
                {
                    WorldPreset localPreset = preset;
                    worldOptions.Add(new FloatMenuOption("WB_CustomizeSaveToPreset".Translate(localPreset.Label), () =>
                                    {
                                        ShowSaveConfirmationDialog(localPreset);
                                    }));
                }
                worldOptions.Add(new FloatMenuOption("WB_SelectPresetCreateNewButton".Translate(), () =>
                {
                    Find.WindowStack.Add(new Window_CreateOrEditWorld(customizationData: (thingDef, customizationData)));
                }));
                Find.WindowStack.Add(new FloatMenu(worldOptions));
            }
            currentButtonX += buttonWidth + buttonSpacing;

            var saveThingButtonRect = new Rect(currentButtonX, inRect.yMax - buttonHeight, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(saveThingButtonRect, "WB_CustomizeSave".Translate()))
            {
                SaveIndividualChanges();
            }
            currentButtonX += buttonWidth + buttonSpacing;

            var mapButtonRect = new Rect(currentButtonX, inRect.yMax - buttonHeight, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(mapButtonRect, "WB_CustomizeMap".Translate()))
            {
                var mapOptions = new List<FloatMenuOption>();
                mapOptions.Add(new FloatMenuOption("WB_CustomizeSaveMapAll".Translate(thingDef.label), () =>
                {
                    var confirmationDialog = Dialog_MessageBox.CreateConfirmation(
                        "WB_ThingCustomizeSaveMapAllConfirm".Translate(thingDef.label),
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

            var resetButtonRect = new Rect(currentButtonX, inRect.yMax - buttonHeight, buttonWidth, buttonHeight);
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

        protected override void DrawAppearanceTab(Rect tabRect)
        {

            DisplayThingPreview(tabRect, out var tabWidth, out var previewImageRect, out var currentY);

            var tabsRect = new Rect(tabRect.x, currentY, tabWidth, 32);
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

            currentY = tabsRect.yMax + 15;
            DrawColorSelector(
                tabsRect.x,
                currentY,
                tabsRect.width,
                customizationData.color,
                newColor => {
                    customizationData.color = newColor;
                    ApplyVisualChanges();
                }
            );

            currentY += 55f;
            currentY = DrawRenderTransformControls(tabRect.x, currentY, tabWidth);

            float gridStartX = tabsRect.xMax + 10f;
            float thumbnailSize = 120;
            float spacing = 15f;
            float ySpacing = 30;
            int thumbnailsPerRow = 4;
            float extraPadding = 0f;
            var gridRect = new Rect(gridStartX, previewImageRect.y, tabRect.width - tabWidth - 10f, tabRect.height - 20);

            bool hasVariations = graphicProps != null && graphicProps.randomGraphics != null && graphicProps.randomGraphics.Count > 0;
            var hasStyles = availableStyles.Any(s => s != null);
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
        }

        private float DrawRenderTransformControls(float x, float y, float width)
        {
            float lineHeight = 22f;
            y += 12;
            Widgets.Label(new Rect(x, y, width, lineHeight), "WB_CustomizeRenderOffset".Translate());
            y += lineHeight;

            var dPadRect = new Rect(x, y - 10, 80f, 80f);

            Vector2 tempOffset = customizationData.drawOffset;
            DrawOffsetDPad(dPadRect, ref tempOffset);
            customizationData.drawOffset = tempOffset;

            float dPadCenterY = y + 40f;
            var resetRect = new Rect(x, dPadCenterY + 24, 80f, 24f);

            if (Widgets.ButtonText(resetRect, "Reset".Translate()))
            {
                customizationData.drawOffset = Vector2.zero;
                customizationData.rotation = 0f;
                ApplyVisualChanges();
            }
            y -= 12;

            float halfWidth = width / 2f;

            float controlSize = 100f;

            float tempRot = customizationData.rotation;
            DrawRotationWheel(new Rect(x + halfWidth, y, controlSize, controlSize), ref tempRot);
            customizationData.rotation = tempRot;
            if (tempRot != customizationData.rotation)
            {
                ApplyVisualChanges();
            }

            y += controlSize + 10f;

            var layerLabelRect = new Rect(x, y, 90f, 24f);
            var layerButtonRect = new Rect(x + 95f, y, width - 95f, 24f);

            Widgets.Label(layerLabelRect, "WB_CustomizeRenderLayer".Translate());

            string layerName = customizationData.altitudeLayer.HasValue ? customizationData.altitudeLayer.Value.ToString() : "Default".Translate();
            if (Widgets.ButtonText(layerButtonRect, layerName))
            {
                var layerOptions = new List<FloatMenuOption>();
                layerOptions.Add(new FloatMenuOption("Default".Translate(), () => customizationData.altitudeLayer = null));
                foreach (var layer in (AltitudeLayer[])Enum.GetValues(typeof(AltitudeLayer)))
                {
                    AltitudeLayer local = layer;
                    layerOptions.Add(new FloatMenuOption(layer.ToString(), () => customizationData.altitudeLayer = local));
                }
                Find.WindowStack.Add(new FloatMenu(layerOptions));
            }

            return y + 80f;
        }

        private void DrawRotationWheel(Rect rect, ref float rotation)
        {
            float lineWidth = 4f;

            if (circleTexture == null)
            {
                circleTexture = CreateCircleTexture(128);
            }

            if (smoothLineTexture == null)
            {
                smoothLineTexture = CreateSmoothLineTexture((int)lineWidth);
            }

            var size = Mathf.Min(rect.width, rect.height);
            var squareRect = new Rect(
                rect.x + (rect.width - size) / 2f,
                rect.y + (rect.height - size) / 2f,
                size,
                size
            );

            GUI.color = new Color(0.3f, 0.3f, 0.3f);
            GUI.DrawTexture(squareRect, circleTexture);
            GUI.color = Color.white;

            Vector2 center = squareRect.center;
            float radius = (squareRect.width / 2f) - 2f;

            float lineLength = radius;

            var lineRect = new Rect(
                center.x - lineWidth / 2f,
                center.y - lineLength,
                lineWidth,
                lineLength
            );

            Matrix4x4 m = Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, rotation), Vector3.one) * Matrix4x4.TRS(-center, Quaternion.identity, Vector3.one);

            GL.PushMatrix();
            GL.MultMatrix(m);
            GUI.DrawTexture(lineRect, smoothLineTexture);
            GL.PopMatrix();

            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && Mouse.IsOver(squareRect))
            {
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && Input.GetMouseButton(0) && Mouse.IsOver(squareRect))
            {
                Vector2 dir = e.mousePosition - center;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                float res = angle + 90f;
                if (res < 0) res += 360f;
                if (res >= 360f) res -= 360f;

                rotation = res;
                ApplyVisualChanges();
                e.Use();
            }
        }

        private static Texture2D CreateSmoothLineTexture(int width)
        {
            var texture = new Texture2D(width, 1, TextureFormat.ARGB32, false);
            texture.filterMode = FilterMode.Bilinear;

            float center = width / 2f;

            for (int x = 0; x < width; x++)
            {
                var distance = Mathf.Abs(x - center);
                float normalizedDistance = distance / (width / 2f);

                float alpha = 1f - Mathf.Clamp01(normalizedDistance * 2f - 0.5f);

                texture.SetPixel(x, 0, new Color(1, 1, 1, alpha));
            }

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateCircleTexture(int resolution)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);
            texture.filterMode = FilterMode.Bilinear;

            var center = new Vector2(resolution / 2f, resolution / 2f);
            float radius = resolution / 2f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    var pos = new Vector2(x, y);
                    var distance = Vector2.Distance(pos, center);

                    if (distance <= radius - 1f)
                    {
                        texture.SetPixel(x, y, Color.white);
                    }
                    else if (distance <= radius)
                    {
                        float alpha = 1f - (distance - (radius - 1f));
                        texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            return texture;
        }

        private void DrawOffsetDPad(Rect rect, ref Vector2 offset)
        {
            float btnSize = 24f;

            float centerX = rect.x + (rect.width - btnSize) / 2f;
            float centerY = rect.y + (rect.height - btnSize) / 2f;

            var upRect = new Rect(centerX, rect.y + 10, btnSize, btnSize);
            var leftRect = new Rect(rect.x, centerY, btnSize, btnSize);
            var rightRect = new Rect(rect.xMax - btnSize, centerY, btnSize, btnSize);
            var downRect = new Rect(centerX, rect.yMax - btnSize - 10, btnSize, btnSize);

            float step = 0.05f;

            if (Widgets.ButtonText(upRect, "▲")) { offset.y += step; ApplyVisualChanges(); }
            if (Widgets.ButtonText(leftRect, "◀")) { offset.x -= step; ApplyVisualChanges(); }
            if (Widgets.ButtonText(rightRect, "▶")) { offset.x += step; ApplyVisualChanges(); }
            if (Widgets.ButtonText(downRect, "▼")) { offset.y -= step; ApplyVisualChanges(); }
        }

        private class StyleGridItem
        {
            public enum ItemType { Style, Variation }

            public ItemType type;
            public ThingStyleDef styleDef;
            public int variationIndex = -1;
            public string label;
        }
        private void DrawStyles(float thumbnailSize, float spacing, float ySpacing, int thumbnailsPerRow, float extraPadding, Rect gridRect)
        {
            var itemsToDraw = new List<StyleGridItem>();
            foreach (var styleDef in availableStyles)
            {
                var graphicRandom = (styleDef?.graphicData?.Graphic ?? thingDef.graphic) as Graphic_Random;
                if (graphicRandom != null)
                {
                    for (int i = 0; i < graphicRandom.subGraphics.Length; i++)
                    {
                        var item = new StyleGridItem
                        {
                            type = StyleGridItem.ItemType.Variation,
                            styleDef = styleDef,
                            variationIndex = i,
                            label = $"{styleDef?.Category?.label ?? "Default".Translate().ToString()}"
                        };
                        itemsToDraw.Add(item);
                    }
                }
                else
                {
                    itemsToDraw.Add(new StyleGridItem
                    {
                        type = StyleGridItem.ItemType.Style,
                        styleDef = styleDef,
                        label = styleDef != null ? styleDef?.Category?.label?.CapitalizeFirst() ?? "Default".Translate().ToString() : "Default".Translate().ToString()
                    });
                }
            }

            if (itemsToDraw.Count == 0) return;
            int totalItemCount = itemsToDraw.Count;
            var numberOfRows = Mathf.CeilToInt((float)totalItemCount / thumbnailsPerRow);
            float rowHeight = thumbnailSize + 24f;
            float totalGridHeight = rowHeight * numberOfRows + ySpacing * (numberOfRows > 0 ? numberOfRows - 1 : 0) + extraPadding * 2;

            var viewRect = new Rect(0, 0, gridRect.width - 16, totalGridHeight);
            Widgets.BeginScrollView(gridRect, ref scrollPosition, viewRect);
            float thumbnailStartY = extraPadding;

            for (int i = 0; i < totalItemCount; i++)
            {
                var item = itemsToDraw[i];
                int row = i / thumbnailsPerRow;
                int col = i % thumbnailsPerRow;

                var thumbnailRect = new Rect(
                    spacing + col * (thumbnailSize + spacing),
                    thumbnailStartY + row * (rowHeight + ySpacing),
                    thumbnailSize,
                    thumbnailSize
                );

                Widgets.DrawMenuSection(thumbnailRect);
                if (item.type == StyleGridItem.ItemType.Style)
                {
                    var graphic = item.styleDef?.graphicData?.Graphic ?? thingDef.graphic;
                    var thing = things.FirstOrDefault();
                    var color = customizationData.color ?? (thingDef.MadeFromStuff && thing != null ? thingDef.GetColorForStuff(thing.Stuff) : thingDef.uiIconColor);

                    var previewData = new CustomizationData
                    {
                        styleDef = item.styleDef,
                        color = customizationData.color
                    };

                    CustomizationGraphicUtility.DrawCustomizedGraphic(thumbnailRect.ContractedBy(5), graphic, thingDef, previewData, color, Rot4.South);

                    if (Widgets.ButtonInvisible(thumbnailRect))
                    {
                        customizationData.styleDef = item.styleDef;
                        customizationData.randomIndexOverride?.Remove(customizationData.RandomIndexKey);
                        ApplyVisualChanges();
                    }
                    if (customizationData.styleDef == item.styleDef)
                    {
                        Widgets.DrawHighlight(thumbnailRect);
                    }
                }
                else
                {
                    var parentGraphic = (item.styleDef?.graphicData?.Graphic ?? thingDef.graphic) as Graphic_Random;
                    var variationGraphic = parentGraphic.subGraphics[item.variationIndex];

                    var thing = things.FirstOrDefault();
                    var color = customizationData.color ?? (thingDef.MadeFromStuff && thing != null ? thingDef.GetColorForStuff(thing.Stuff) : thingDef.uiIconColor);

                    var previewData = new CustomizationData
                    {
                        styleDef = item.styleDef,
                        color = customizationData.color,
                        randomIndexOverride = new Dictionary<string, int> { { thingDef.defName, item.variationIndex } }
                    };

                    CustomizationGraphicUtility.DrawCustomizedGraphic(thumbnailRect.ContractedBy(5), variationGraphic, thingDef, previewData, color, Rot4.South);

                    if (Widgets.ButtonInvisible(thumbnailRect))
                    {
                        customizationData.styleDef = item.styleDef;
                        customizationData.randomIndexOverride ??= new Dictionary<string, int>();
                        customizationData.randomIndexOverride.Clear();
                        customizationData.randomIndexOverride[customizationData.RandomIndexKey] = item.variationIndex;
                        ApplyVisualChanges();
                    }
                    if (customizationData.styleDef == item.styleDef && customizationData.randomIndexOverride != null && customizationData.randomIndexOverride.TryGetValue(customizationData.RandomIndexKey, out int curIndex) && curIndex == item.variationIndex)
                    {
                        Widgets.DrawHighlight(thumbnailRect);
                    }
                }
                Text.Font = GameFont.Small;
                var labelBox = new Rect(thumbnailRect.x, thumbnailRect.yMax, thumbnailRect.width, 24);
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.DrawBoxSolid(labelBox, new ColorInt(94, 93, 93).ToColor);
                Widgets.Label(labelBox, item.label);
                Text.Anchor = TextAnchor.UpperLeft;
            }

            Widgets.EndScrollView();
        }

        private void DrawDefaultResetThumbnail(float thumbnailSize, float spacing, float extraPadding, Rect gridRect)
        {
            var viewRect = new Rect(0, 0, gridRect.width - 16, thumbnailSize + 24 + extraPadding * 2);
            Widgets.BeginScrollView(gridRect, ref scrollPosition, viewRect);

            var thing = things.First();
            float currentX = spacing;
            float currentGridY = extraPadding;

            var defaultThumbnailRect = new Rect(currentX, currentGridY, thumbnailSize, thumbnailSize);
            Widgets.DrawMenuSection(defaultThumbnailRect);
            if (thingDef.graphic is Graphic_Linked || thing is Building_Door && customizationData.styleDef != null)
            {
                Widgets.DefIcon(defaultThumbnailRect.ContractedBy(5), thingDef, null, thingStyleDef: customizationData.styleDef,
                    color: customizationData.color, scale: 0.7f);
            }
            else
            {
                var defaultGraphic = customizationData.DefaultGraphic(thing);
                if (defaultGraphic is Graphic_Random random)
                {
                    defaultGraphic = random.subGraphics.First();
                }
                var color = customizationData.color ?? (thingDef.MadeFromStuff ? thingDef.GetColorForStuff(thing.Stuff) : thingDef.uiIconColor);
                CustomizationGraphicUtility.DrawCustomizedGraphic(defaultThumbnailRect.ContractedBy(5), defaultGraphic, thingDef, customizationData, color, Rot4.South);
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
                ApplyVisualChanges();
            }

            Widgets.DrawHighlight(defaultThumbnailRect);
            DrawLabelBelowThumbnail(defaultThumbnailRect, "WB_CustomizeDefault".Translate());
            Widgets.EndScrollView();
        }

        private void DrawVariations(float thumbnailSize, float spacing, float ySpacing, int thumbnailsPerRow, float extraPadding, Rect gridRect)
        {
            if (graphicProps == null || graphicProps.randomGraphics == null || graphicProps.randomGraphics.Count == 0)
            {
                Widgets.Label(gridRect, "WB_ThingCustomizeNoVariations".Translate());
                return;
            }

            var numberOfRows = Mathf.CeilToInt((float)graphicProps.randomGraphics.Count / thumbnailsPerRow);
            float totalGridHeight = thumbnailSize * numberOfRows + ySpacing * (numberOfRows - 1) + extraPadding * 2;
            totalGridHeight += 24;
            var viewRect = new Rect(0, 0, gridRect.width - 16, totalGridHeight);
            Widgets.BeginScrollView(gridRect, ref scrollPosition, viewRect);
            int variationIndex = 0;
            float thumbnailStartY = extraPadding;

            for (int row = 0; row < numberOfRows; row++)
            {
                for (int col = 0; col < thumbnailsPerRow; col++)
                {
                    if (variationIndex < graphicProps.randomGraphics.Count)
                    {
                        var thumbnailRect = new Rect(
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
                        var color = customizationData.color ?? (thingDef.MadeFromStuff ? thingDef.GetColorForStuff(things.First().Stuff) : thingDef.uiIconColor);
                        CustomizationGraphicUtility.DrawCustomizedGraphic(thumbnailRect.ContractedBy(5), variationGraphic, thingDef, customizationData, color, Rot4.South);
                        if (Widgets.ButtonInvisible(thumbnailRect))
                        {
                            customizationData.variationIndex = variationIndex;
                            customizationData.styleDef = null;
                            customizationData.selectedImagePath = null;
                            ApplyVisualChanges();
                        }
                        if (customizationData.variationIndex == variationIndex)
                        {
                            Widgets.DrawHighlight(thumbnailRect);
                        }
                        var variationName = "WB_ThingCustomizeVariationLabel".Translate() + (variationIndex + 1);
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

        private void DisplayThingPreview(Rect tabRect, out float previewWidth, out Rect previewImageRect, out float currentY)
        {
            previewWidth = 200f;
            previewImageRect = new Rect(tabRect.x, tabRect.y + 15, previewWidth, previewWidth);
            Widgets.DrawMenuSection(previewImageRect);
            var previewThingRect = previewImageRect.ContractedBy(previewImageRect.width * 0.05f);
            var graphic = CustomizationGraphicUtility.GetGraphic(things.First().def, things.First().Stuff, customizationData);
            var color = customizationData.color ?? (things.First().def.MadeFromStuff ? things.First().def.GetColorForStuff(things.First().Stuff) : things.First().def.uiIconColor);
            CustomizationGraphicUtility.DrawCustomizedGraphic(
                previewThingRect,
                graphic,
                things.First().def,
                customizationData,
                color,
                things.First().Rotation,
                0f,
                1f,
                applyCustomizationTransforms: true
            );
            currentY = previewImageRect.yMax + 15;
        }

        protected override void DrawDetailTab(Rect tabRect)
        {
            DisplayThingPreview(tabRect, out var tabWidth, out var previewImageRect, out var currentY);
            var explanationTextBox = new Rect(tabRect.x, currentY, tabWidth, 100f);
            Widgets.Label(explanationTextBox, "WB_CustomizeDetailTabExplanation".Translate());
            currentY = tabRect.y + 15;
            float lineHeight = 30f;
            tabRect.xMin += tabWidth + 15;
            Widgets.Label(new Rect(tabRect.x, currentY, 100f, lineHeight), "WB_CustomizeLabel".Translate());
            var labelEditRect = new Rect(tabRect.x + 100f, currentY, tabRect.width - 100f - 70f, lineHeight);
            customizationData.labelOverride = Widgets.TextField(labelEditRect, customizationData.labelOverride);

            var resetLabelButtonRect = new Rect(labelEditRect.xMax + 5f, currentY, 65f, lineHeight);
            if (Widgets.ButtonText(resetLabelButtonRect, "Reset".Translate()))
            {
                customizationData.labelOverride = thingDef.label;
            }
            currentY += lineHeight + 10f;
            Widgets.CheckboxLabeled(new Rect(tabRect.x, currentY, tabRect.width - 100f - 70f - 65f - 10f, lineHeight), "WB_CustomizeIncludeAdditionalDetails".Translate(), ref customizationData.includeAdditionalDetails);
            currentY += lineHeight + 10f;
            Widgets.CheckboxLabeled(new Rect(tabRect.x, currentY, tabRect.width - 100f - 70f - 65f - 10f, lineHeight), "WB_CustomizeIncludeMaterialInLabel".Translate(), ref customizationData.includeMaterialInLabel);
            currentY += lineHeight + 10f;
            Widgets.Label(new Rect(tabRect.x, currentY, 100f, lineHeight), "WB_CustomizeDescription".Translate());
            var descriptionEditRect = new Rect(tabRect.x + 100f, currentY, tabRect.width - 100f - 70f, 100f);
            customizationData.descriptionOverride = Widgets.TextArea(descriptionEditRect, customizationData.descriptionOverride);

            var resetDescriptionButtonRect = new Rect(descriptionEditRect.xMax + 5f, currentY, 65f, lineHeight);
            if (Widgets.ButtonText(resetDescriptionButtonRect, "Reset".Translate()))
            {
                customizationData.descriptionOverride = null;
                customizationData.descriptionOverride = things.First().DescriptionFlavor;
            }
        }

        protected override void DrawNarrativeTab(Rect tabRect)
        {
            DisplayThingPreview(tabRect, out var tabWidth, out var previewImageRect, out var currentY);
            var explanationTextBox = new Rect(tabRect.x, currentY, tabWidth, 100f);
            Widgets.Label(explanationTextBox, "WB_CustomizeNarrativeTabExplanation".Translate());
            var narrativeEditRect = new Rect(tabRect.x + tabWidth + 15, tabRect.y + 15, tabRect.width - tabWidth - 15, tabRect.height - 60);
            customizationData.narrativeText = DevGUI.TextAreaScrollable(narrativeEditRect, customizationData.narrativeText, ref narrativeScrollPosition);
        }

        private List<ThingStyleDef> GetAvailableStylesForThing()
        {
            var styles = new List<ThingStyleDef>();
            styles.Add(null);
            foreach (var categoryDef in DefDatabase<StyleCategoryDef>.AllDefs)
            {
                ThingStyleDef style = categoryDef.GetStyleForThingDef(thingDef);
                if (style != null)
                {
                    styles.Add(style);
                }
            }
            if (!thingDef.randomStyle.NullOrEmpty())
            {
                foreach (var styleChance in thingDef.randomStyle)
                {
                    if (styleChance.StyleDef.graphicData != null)
                    {
                        styles.Add(styleChance.StyleDef);
                    }
                }
            }
            return styles.Distinct().OrderBy(styleDef =>
            {
                if (styleDef == null) return 0;
                if (styleDef?.Category == null) return 1;
                return 2;
            }).ToList();
        }

        protected override void SaveIndividualChanges()
        {
            foreach (var thing in things)
            {
                CustomizationDataCollections.thingCustomizationData[thing] = customizationData.Copy();
                customizationData.SetGraphic(thing);
            }
            hasUnsavedChanges = false;
            originalData = customizationData.Copy();
            Messages.Message("WB_ThingCustomizeSaveSuccess".Translate(thingDef.label), MessageTypeDefOf.PositiveEvent);
        }

        private void ApplyCustomizationsToMaps()
        {
            foreach (var map in Find.Maps)
            {
                foreach (Thing thing in map.listerThings.AllThings.Where(t => t.def == thingDef))
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
                if (thing.def == thingDef)
                {
                    thing.UpdateGraphic();
                }
            }
        }

        private void ShowSaveConfirmationDialog(WorldPreset targetPreset)
        {
            string presetNameForMessage = targetPreset.Label;
            var confirmationDialog = Dialog_MessageBox.CreateConfirmation(
               "WB_CustomizeSaveToPresetConfirm".Translate(thingDef.label, presetNameForMessage),
               () =>
               {
                   targetPreset.customizationDefaults ??= new Dictionary<string, CustomizationData>();
                   targetPreset.customizationDefaults[thingDef.defName] = customizationData;
                   bool savedSuccessfully = WorldPresetManager.SavePreset(targetPreset, null, null);
                   if (savedSuccessfully)
                   {
                       Messages.Message("WB_CustomizePresetSaveSuccess".Translate(thingDef.label, presetNameForMessage), MessageTypeDefOf.PositiveEvent);
                       WorldbuilderMod.ApplyCustomizationsToExistingThings();
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
