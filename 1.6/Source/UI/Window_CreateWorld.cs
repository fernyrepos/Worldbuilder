using System;
using Verse;
using RimWorld;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using HarmonyLib;

namespace Worldbuilder
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    public class Window_CreateWorld : Window
    {
        private WorldPreset presetInProgress;
        private string presetName = "";
        private string presetDescription = "";
        private string thumbnailPath = "";
        private string flavorImagePath = "";
        private static readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
        private static readonly Texture2D MissingTexture = SolidColorMaterials.NewSolidColorTexture(Color.magenta);
        private float ButtonHeight => 35f;
        private float InputFieldHeight => 30f;
        private float Spacing => 10f;
        private float LabelWidth => 100f;
        private float ThumbnailSize => 292f;
        private float FlavorImageWidth => 498f;
        private float FlavorImageHeight => 249f;
        private float UploadButtonWidth => 100f;

        public override Vector2 InitialSize => new Vector2(850f, 900f);

        public Window_CreateWorld(WorldPreset existingPreset = null)
        {
            forcePause = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            draggable = true;

            if (existingPreset != null)
            {
                presetInProgress = existingPreset;
                presetName = presetInProgress.name;
                presetDescription = presetInProgress.description;
                thumbnailPath = WorldPresetManager.GetThumbnailPath(presetInProgress.name);
                flavorImagePath = WorldPresetManager.GetFlavorImagePath(presetInProgress.name);
            }
            else
            {
                presetInProgress = new WorldPreset();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            float currentY = inRect.y;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(inRect.x, currentY, inRect.width, 30f), "WB_CreatePresetTitle".Translate());
            currentY += 50f;
            Text.Font = GameFont.Small;

            float inputAreaWidth = inRect.width - LabelWidth - Spacing;

            Widgets.Label(new Rect(inRect.x, currentY, LabelWidth, InputFieldHeight), "WB_CreatePresetNameLabel".Translate());
            presetName = Widgets.TextField(new Rect(inRect.x + LabelWidth + Spacing, currentY, inputAreaWidth, InputFieldHeight), presetName);
            currentY += InputFieldHeight + Spacing;

            float descriptionHeight = 370f;
            Widgets.Label(new Rect(inRect.x, currentY, LabelWidth, descriptionHeight), "WB_CreatePresetDescLabel".Translate());
            presetDescription = Widgets.TextArea(new Rect(inRect.x + LabelWidth + Spacing, currentY, inputAreaWidth, descriptionHeight), presetDescription);
            currentY += descriptionHeight + Spacing * 2;

            float imageSectionY = currentY;
            float totalImageBlockWidth = ThumbnailSize + Spacing + FlavorImageWidth;
            float imageBlockStartX = inRect.x + (inRect.width - totalImageBlockWidth) / 2f;

            float thumbnailSectionHeight = InputFieldHeight + Spacing + ThumbnailSize;
            float flavorSectionHeight = InputFieldHeight + Spacing + FlavorImageHeight;

            Rect thumbnailSectionRect = new Rect(imageBlockStartX, imageSectionY, ThumbnailSize, thumbnailSectionHeight);
            Rect flavorSectionRect = new Rect(thumbnailSectionRect.xMax + Spacing, imageSectionY, FlavorImageWidth, flavorSectionHeight);

            DrawImageSection(thumbnailSectionRect, "ThumbnailImageLabel", thumbnailPath, path => thumbnailPath = path, ThumbnailSize, ThumbnailSize);
            DrawImageSection(flavorSectionRect, "FlavorImageLabel", flavorImagePath, path => flavorImagePath = path, FlavorImageWidth, ThumbnailSize);

            float bottomButtonY = inRect.yMax - ButtonHeight;
            float buttonWidth = 120f;

            Rect CancelRect = new Rect(inRect.x, bottomButtonY, buttonWidth, ButtonHeight);
            if (Widgets.ButtonText(CancelRect, "Cancel".Translate()))
            {
                Close();
            }

            Rect createButtonRect = new Rect(inRect.xMax - buttonWidth, bottomButtonY, buttonWidth, ButtonHeight);
            if (Widgets.ButtonText(createButtonRect, "WB_CreatePresetCreateButton".Translate()))
            {
                TryCreatePreset();
            }

            Rect settingsButtonRect = new Rect(createButtonRect.x - buttonWidth - Spacing, bottomButtonY, buttonWidth, ButtonHeight);
            if (Widgets.ButtonText(settingsButtonRect, "WB_CreatePresetWorldSettingsButton".Translate()))
            {
                Find.WindowStack.Add(new Window_WorldSettings(presetInProgress));
            }
        }

        private void DrawImageSection(Rect rect, string labelKey, string imagePath, Action<string> onPathSelected, float previewWidth, float previewHeight)
        {
            float labelWidth = Text.CalcSize(labelKey == "ThumbnailImageLabel" ? "WB_CreatePresetThumbLabel".Translate() : "WB_CreatePresetFlavorLabel".Translate()).x;
            float uploadButtonX = rect.x + labelWidth + Spacing;

            Widgets.Label(new Rect(rect.x, rect.y, labelWidth, InputFieldHeight), labelKey == "ThumbnailImageLabel" ? "WB_CreatePresetThumbLabel".Translate() : "WB_CreatePresetFlavorLabel".Translate());
            if (Widgets.ButtonText(new Rect(uploadButtonX, rect.y, UploadButtonWidth, InputFieldHeight), "WB_CreatePresetUploadImageButton".Translate()))
            {
                var fileSelector = new Dialog_FileSelector { onSelectAction = path => onPathSelected(path) };
                Find.WindowStack.Add(fileSelector);
            }

            float previewX = rect.x + (rect.width - previewWidth) / 2f;
            Rect previewRect = new Rect(previewX, rect.y + InputFieldHeight + Spacing, previewWidth, previewHeight);
            Widgets.DrawBox(previewRect);
            Texture2D tex = GetTextureForPreview(imagePath);
            if (tex != null)
            {
                GUI.DrawTexture(previewRect.ContractedBy(2f), tex, ScaleMode.ScaleToFit);
            }
        }

        private static Texture2D GetTextureForPreview(string path)
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
                textureCache[path] = MissingTexture;
                return MissingTexture;
            }
            catch (System.Exception ex)
            {
                Log.Error($"Worldbuilder: Exception loading texture from {path} for preview: {ex.Message}");
                textureCache[path] = MissingTexture;
                return MissingTexture;
            }
        }

        private void TryCreatePreset()
        {
            if (string.IsNullOrWhiteSpace(presetName))
            {
                Messages.Message("WB_CreatePresetNameEmptyError".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            if (presetName.Equals(Path.GetFileNameWithoutExtension("Default.xml"), StringComparison.OrdinalIgnoreCase))
            {
                Messages.Message("WB_CreatePresetNameReservedError".Translate("Default"), MessageTypeDefOf.RejectInput);
                return;
            }
            if (WorldPresetManager.GetPreset(presetName) != null && presetInProgress.name != presetName)
            {
                Messages.Message("WB_CreatePresetNameExistsError".Translate(presetName), MessageTypeDefOf.RejectInput);
                return;
            }

            presetInProgress.name = presetName;
            presetInProgress.description = presetDescription;
            if (presetInProgress.customizationDefaults == null)
            {
                presetInProgress.customizationDefaults = new Dictionary<ThingDef, CustomizationData>(CustomizationDataCollections.playerDefaultCustomizationData);
            }
            WorldbuilderMod.SaveWorldDataToPreset(presetInProgress);

            if (WorldPresetManager.SavePreset(presetInProgress, thumbnailPath, flavorImagePath))
            {
                Messages.Message("WB_CreatePresetSaveSuccess".Translate(presetName), MessageTypeDefOf.PositiveEvent);
                Close();
            }
            else
            {
                Messages.Message("WB_CreatePresetSaveFailed".Translate(presetName), MessageTypeDefOf.NegativeEvent);
            }
        }
    }

}