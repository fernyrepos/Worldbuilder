using System;
using Verse;
using RimWorld;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Worldbuilder
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    public class Window_CreateOrEditWorld : Window
    {
        private WorldPreset presetInProgress;

        private string originalPresetName;
        private string presetName = "";
        private string presetDescription = "";
        private string planetType = "";
        private int difficulty = 2;

        private byte[] thumbnailBytes;
        private byte[] flavorImageBytes;

        private Texture2D cachedThumbnailTex;
        private Texture2D cachedFlavorTex;

        private bool isEditingExistingPreset = false;

        private float ButtonHeight => 35f;
        private float InputFieldHeight => 30f;
        private float Spacing => 10f;
        private float LabelWidth => 100f;
        private float ThumbnailSize => 250f;
        private float FlavorImageWidth => 440f;
        private float FlavorImageHeight => 200f;
        private float UploadButtonWidth => 100f;
        private bool enableAllCheckboxes = false;

        public override Vector2 InitialSize => new Vector2(850f, 768f);

        public Window_CreateOrEditWorld(WorldPreset existingPreset = null, bool enableAllCheckboxes = false, bool isEditingExistingPreset = false, (ThingDef def, CustomizationData data) customizationData = default)
        {
            forcePause = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            draggable = true;
            closeOnAccept = false;
            this.isEditingExistingPreset = isEditingExistingPreset;
            this.enableAllCheckboxes = enableAllCheckboxes;

            if (existingPreset != null)
            {
                presetInProgress = existingPreset;
                presetName = presetInProgress.name;
                originalPresetName = presetName;
                if (isEditingExistingPreset is false)
                {
                    presetName = "Copy of " + presetName;
                }
                presetDescription = presetInProgress.description;
                planetType = presetInProgress.planetType;
                difficulty = presetInProgress.difficulty;

                try
                {
                    string thumbnailPath = WorldPresetManager.GetThumbnailPath(presetInProgress.name);
                    if (File.Exists(thumbnailPath)) thumbnailBytes = File.ReadAllBytes(thumbnailPath);

                    string flavorImagePath = WorldPresetManager.GetFlavorImagePath(presetInProgress.name);
                    if (File.Exists(flavorImagePath)) flavorImageBytes = File.ReadAllBytes(flavorImagePath);
                }
                catch (Exception ex)
                {
                    Log.Error($"Worldbuilder: Failed to load preset images into memory. {ex.Message}");
                }
            }
            else
            {
                presetInProgress = new WorldPreset();
                var currentPreset = WorldPresetManager.CurrentlyLoadedPreset;
                if (currentPreset != null)
                {
                    presetInProgress.presetFolder = currentPreset.presetFolder;
                }
            }

            if (this.enableAllCheckboxes)
            {
                presetInProgress.saveFactions = true;
                presetInProgress.saveIdeologies = true;
                presetInProgress.saveTerrain = true;
                presetInProgress.saveBases = true;
                presetInProgress.saveMapMarkers = true;
                presetInProgress.saveWorldFeatures = true;
                presetInProgress.saveStorykeeperEntries = false;
            }
            if (customizationData != default)
            {
                presetInProgress.customizationDefaults ??= new Dictionary<string, CustomizationData>();
                presetInProgress.customizationDefaults[customizationData.def.defName] = customizationData.data;
            }
        }

        public override void PostClose()
        {
            base.PostClose();
            if (cachedThumbnailTex != null) UnityEngine.Object.Destroy(cachedThumbnailTex);
            if (cachedFlavorTex != null) UnityEngine.Object.Destroy(cachedFlavorTex);
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

            float descriptionHeight = 200f;
            Widgets.Label(new Rect(inRect.x, currentY, LabelWidth, descriptionHeight), "WB_CreatePresetDescLabel".Translate());
            presetDescription = Widgets.TextArea(new Rect(inRect.x + LabelWidth + Spacing, currentY, inputAreaWidth, descriptionHeight), presetDescription);
            currentY += descriptionHeight + Spacing * 2;

            float imageSectionY = currentY;
            float totalImageBlockWidth = ThumbnailSize + Spacing + FlavorImageWidth;
            float imageBlockStartX = inRect.x + LabelWidth + Spacing;

            float thumbnailSectionHeight = InputFieldHeight + Spacing + ThumbnailSize;
            float flavorSectionHeight = InputFieldHeight + Spacing + FlavorImageHeight;

            Rect thumbnailSectionRect = new Rect(imageBlockStartX, imageSectionY, ThumbnailSize, thumbnailSectionHeight);
            Rect flavorSectionRect = new Rect(thumbnailSectionRect.xMax + Spacing, imageSectionY, FlavorImageWidth, flavorSectionHeight);

            DrawThumbnailImageSection(thumbnailSectionRect);
            DrawFlavorImageSection(flavorSectionRect);

            currentY = Mathf.Max(thumbnailSectionRect.yMax, flavorSectionRect.yMax) + Spacing * 2;

            DrawPlanetTypeInput(new Rect(imageBlockStartX, currentY, totalImageBlockWidth, InputFieldHeight));
            currentY += InputFieldHeight;

            DrawDifficultyInput(new Rect(imageBlockStartX, currentY, totalImageBlockWidth, InputFieldHeight));

            float bottomButtonY = inRect.yMax - ButtonHeight;
            float buttonWidth = 120f;

            Rect CancelRect = new Rect(inRect.x, bottomButtonY, buttonWidth, ButtonHeight);
            if (Widgets.ButtonText(CancelRect, "Cancel".Translate()))
            {
                Close();
            }

            Rect createButtonRect = new Rect(inRect.xMax - buttonWidth, bottomButtonY, buttonWidth, ButtonHeight);
            if (isEditingExistingPreset)
            {
                if (Widgets.ButtonText(createButtonRect, "WB_EditExistingWorldLabel".Translate()))
                {
                    TryEditExistingPreset();
                }
            }
            else
            {
                if (Widgets.ButtonText(createButtonRect, "WB_CreatePresetButton".Translate()))
                {
                    TryCreatePreset();
                }
            }

            Rect settingsButtonRect = new Rect(createButtonRect.x - buttonWidth - Spacing, bottomButtonY, buttonWidth, ButtonHeight);
            if (Widgets.ButtonText(settingsButtonRect, "WB_CreatePresetWorldSettingsButton".Translate()))
            {
                Find.WindowStack.Add(new Window_WorldSettings(presetInProgress));
            }
        }
        private void DrawThumbnailImageSection(Rect rect)
        {
            string labelText = "WB_CreatePresetThumbLabel".Translate();
            float labelWidth = Text.CalcSize(labelText).x;
            float uploadButtonX = rect.x + labelWidth + Spacing;

            Widgets.Label(new Rect(rect.x, rect.y, labelWidth, InputFieldHeight), labelText);
            if (Widgets.ButtonText(new Rect(uploadButtonX, rect.y, UploadButtonWidth, InputFieldHeight), "WB_CreatePresetUploadImageButton".Translate()))
            {
                var fileSelector = new Dialog_FileSelector
                {
                    onSelectAction = path =>
                    {
                        try
                        {
                            this.thumbnailBytes = File.ReadAllBytes(path);
                            if (this.cachedThumbnailTex != null) UnityEngine.Object.Destroy(this.cachedThumbnailTex);
                            this.cachedThumbnailTex = null;
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Worldbuilder: Failed to read selected thumbnail image file. {ex.Message}");
                        }
                    }
                };
                Find.WindowStack.Add(fileSelector);
            }

            float previewX = rect.x + (rect.width - ThumbnailSize) / 2f;
            Rect previewRect = new Rect(previewX, rect.y + InputFieldHeight + Spacing, ThumbnailSize, ThumbnailSize);
            Widgets.DrawBox(previewRect);

            if (this.cachedThumbnailTex == null && this.thumbnailBytes != null && this.thumbnailBytes.Length > 0)
            {
                this.cachedThumbnailTex = new Texture2D(2, 2);
                this.cachedThumbnailTex.LoadImage(this.thumbnailBytes);
            }

            if (this.cachedThumbnailTex != null)
            {
                GUI.DrawTexture(previewRect.ContractedBy(2f), this.cachedThumbnailTex, ScaleMode.ScaleToFit);
            }
        }

        private void DrawPlanetTypeInput(Rect rect)
        {
            var label = "WB_PlanetTypeLabel".Translate();
            var labelWidth = Text.CalcSize(label).x;
            Widgets.Label(new Rect(rect.x, rect.y, labelWidth, rect.height), label);
            planetType = Widgets.TextField(new Rect(rect.x + labelWidth + Spacing, rect.y, rect.width - labelWidth - Spacing, rect.height), planetType);
        }

        private void DrawDifficultyInput(Rect rect)
        {
            var label = "WB_DifficultyLabel".Translate();
            var labelWidth = Text.CalcSize(label).x;
            Widgets.Label(new Rect(rect.x, rect.y, labelWidth, rect.height), label);

            Rect starRect = new Rect(rect.x + labelWidth + Spacing, rect.y + (rect.height - 20f) / 2, 20f, 20f);
            for (int i = 0; i < 5; i++)
            {
                if (i < difficulty)
                {
                    if (Widgets.ButtonImage(starRect, Page_SelectWorld.StarIcon))
                    {
                        difficulty = i + 1;
                    }
                }
                else
                {
                    if (Widgets.ButtonImage(starRect, Page_SelectWorld.EmptyStarIcon))
                    {
                        difficulty = i + 1;
                    }
                }
                starRect.x += starRect.width;
            }
        }

        private void DrawFlavorImageSection(Rect rect)
        {
            string labelText = "WB_CreatePresetFlavorLabel".Translate();
            float labelWidth = Text.CalcSize(labelText).x;
            float uploadButtonX = rect.x + labelWidth + Spacing;

            Widgets.Label(new Rect(rect.x, rect.y, labelWidth, InputFieldHeight), labelText);
            if (Widgets.ButtonText(new Rect(uploadButtonX, rect.y, UploadButtonWidth, InputFieldHeight), "WB_CreatePresetUploadImageButton".Translate()))
            {
                var fileSelector = new Dialog_FileSelector
                {
                    onSelectAction = path =>
                    {
                        try
                        {
                            this.flavorImageBytes = File.ReadAllBytes(path);
                            if (this.cachedFlavorTex != null) UnityEngine.Object.Destroy(this.cachedFlavorTex);
                            this.cachedFlavorTex = null;
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"Worldbuilder: Failed to read selected flavor image file. {ex.Message}");
                        }
                    }
                };
                Find.WindowStack.Add(fileSelector);
            }

            float previewX = rect.x + (rect.width - FlavorImageWidth) / 2f;
            Rect previewRect = new Rect(previewX, rect.y + InputFieldHeight + Spacing, FlavorImageWidth, FlavorImageHeight);
            Widgets.DrawBox(previewRect);

            if (this.cachedFlavorTex == null && this.flavorImageBytes != null && this.flavorImageBytes.Length > 0)
            {
                this.cachedFlavorTex = new Texture2D(2, 2);
                this.cachedFlavorTex.LoadImage(this.flavorImageBytes);
            }

            if (this.cachedFlavorTex != null)
            {
                GUI.DrawTexture(previewRect.ContractedBy(2f), this.cachedFlavorTex, ScaleMode.ScaleToFit);
            }
        }

        private void TryEditExistingPreset()
        {
            if (string.IsNullOrWhiteSpace(presetName))
            {
                Messages.Message("WB_CreatePresetNameEmptyError".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            bool isRenaming = !originalPresetName.Equals(presetName, StringComparison.OrdinalIgnoreCase);

            if (isRenaming && WorldPresetManager.GetPreset(presetName) != null)
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "WB_CreatePresetOverwriteConfirm".Translate(presetName),
                    () => { ProceedWithSave(isRenaming); },
                    destructive: true));
            }
            else
            {
                ProceedWithSave(isRenaming);
            }
        }

        private void ProceedWithSave(bool isRenaming)
        {
            presetInProgress.name = presetName;
            presetInProgress.description = presetDescription;
            presetInProgress.planetType = planetType;
            presetInProgress.difficulty = difficulty;

            if (WorldPresetManager.SavePreset(presetInProgress, thumbnailBytes, flavorImageBytes))
            {
                Messages.Message("WB_CreatePresetSaveSuccess".Translate(presetName), MessageTypeDefOf.PositiveEvent);

                if (isRenaming)
                {
                    WorldPresetManager.DeletePreset(originalPresetName);
                }

                WorldbuilderMod.ApplyCustomizationsToExistingThings();
                Close();
            }
            else
            {
                Messages.Message($"Worldbuilder: Failed to save preset '{presetName}'.", MessageTypeDefOf.NegativeEvent);
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
            if (WorldPresetManager.GetPreset(presetName) != null)
            {
                Messages.Message("WB_CreatePresetNameExistsError".Translate(presetName), MessageTypeDefOf.RejectInput);
                return;
            }

            presetInProgress.name = presetName;
            presetInProgress.description = presetDescription;
            presetInProgress.planetType = planetType;
            presetInProgress.difficulty = difficulty;
            WorldbuilderMod.SaveWorldDataToPreset(presetInProgress);
            if (WorldPresetManager.SavePreset(presetInProgress, thumbnailBytes, flavorImageBytes))
            {
                Messages.Message("WB_CreatePresetSaveSuccess".Translate(presetName), MessageTypeDefOf.PositiveEvent);
                WorldbuilderMod.ApplyCustomizationsToExistingThings();
                Close();
            }
            else
            {
                Messages.Message("WB_CreatePresetSaveFailed".Translate(presetName), MessageTypeDefOf.NegativeEvent);
            }
        }
    }
}
