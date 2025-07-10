using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;
using System.IO;
using HarmonyLib;
using LudeonTK;

namespace Worldbuilder
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    public class Window_PawnCustomization : Window_BaseCustomization
    {
        private Pawn pawn;

        private static readonly Dictionary<string, Texture2D> portraitTextureCache = new Dictionary<string, Texture2D>();
        private static readonly Texture2D MissingTexture = SolidColorMaterials.NewSolidColorTexture(Color.magenta);

        private static Type pawnEditorStaticType;
        private static MethodInfo selectMethod;
        private static Type dialogType;

        public override Vector2 InitialSize => new Vector2(700, 600);
        public Dialog_NamePawn dialog;
        public Window_PawnCustomization(Pawn pawn)
            : base()
        {
            this.pawn = pawn;
            SetCustomizationData(pawn);
        }

        private void SetCustomizationData(Pawn pawn)
        {
            var existingData = pawn.GetCustomizationData();
            if (existingData is null)
            {
                this.customizationData = CreateCustomization(pawn);
                this.customizationData.originalPawnName = pawn.Name;
            }
            else
            {
                this.customizationData = existingData.Copy();
            }
        }

        public static Dialog_NamePawn NamePawnDialog(Pawn pawn)
        {
            Dictionary<NameFilter, List<string>> suggestedNames = null;
            NameFilter editableNames;
            NameFilter visibleNames;
            visibleNames = NameFilter.First | NameFilter.Nick | NameFilter.Last | NameFilter.Title;
            editableNames = NameFilter.First | NameFilter.Nick | NameFilter.Last | NameFilter.Title;
            return new Dialog_NamePawn(pawn, visibleNames, editableNames, suggestedNames, null);
        }


        private CustomizationData CreateCustomization(Pawn p)
        {
            CustomizationData data = new CustomizationData();
            data.originalStyleDef = null;
            data.color = null;
            data.descriptionOverride = null;
            data.narrativeText = "";
            data.nameOverride = null;
            data.originalPawnName = p.Name;
            return data;
        }

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);
            if (Event.current.type == EventType.Layout && !string.IsNullOrEmpty(dialog?.focusControlOverride))
            {
                GUI.FocusControl(dialog.focusControlOverride);
                dialog.focusControlOverride = null;
            }
        }

        protected override void DrawAppearanceTab(Rect tabRect)
        {
            float previewSize = 200f;
            float spacing = 15f;
            float uploadButtonWidth = 120f;
            float buttonHeight = 30f;
            Rect previewRect = new Rect(tabRect.x, tabRect.y, previewSize, previewSize);
            Widgets.DrawBox(previewRect);
            Texture portraitTex = null;
            bool useCustomImage = !string.IsNullOrEmpty(customizationData.selectedImagePath);
            string imagePathToLoad = customizationData.selectedImagePath;

            if (useCustomImage)
            {
                if (imagePathToLoad.StartsWith("CustomImages/") && WorldPresetManager.CurrentlyLoadedPreset != null)
                {
                    imagePathToLoad = Path.Combine(WorldPresetManager.CurrentlyLoadedPreset.presetFolder, imagePathToLoad.Replace('/', Path.DirectorySeparatorChar));
                }

                if (File.Exists(imagePathToLoad))
                {
                    portraitTex = GetTextureForPreview(imagePathToLoad);
                }
                else
                {
                    customizationData.selectedImagePath = null;
                    useCustomImage = false;
                }
            }
            if (!useCustomImage)
            {
                PortraitsCache.SetDirty(pawn);
                portraitTex = PortraitsCache.Get(pawn, previewRect.size, Rot4.South, default, 1f, true, true, true, true);
            }
            if (portraitTex != null && portraitTex != BaseContent.BadTex)
            {
                GUI.DrawTexture(previewRect.ContractedBy(2f), portraitTex, ScaleMode.ScaleToFit);
            }
            else
            {
                Widgets.Label(previewRect.ContractedBy(10f), "(Portrait Unavailable)");
            }
            float controlsX = previewRect.xMax + spacing;
            float controlsWidth = tabRect.width - previewRect.width - spacing;
            Rect controlsArea = new Rect(controlsX, tabRect.y, controlsWidth, tabRect.height);
            float controlsCenterY = previewRect.center.y;
            string addPortraitLabel = "WB_PawnCustomizeAddPortraitLabel".Translate();
            Vector2 labelSize = Text.CalcSize(addPortraitLabel);
            Rect labelRect = new Rect(controlsArea.x, controlsCenterY - labelSize.y / 2f, labelSize.x, labelSize.y);
            Widgets.Label(labelRect, addPortraitLabel);
            Rect uploadButtonRect = new Rect(labelRect.xMax + spacing, controlsCenterY - buttonHeight / 2f, uploadButtonWidth, buttonHeight);
            if (Widgets.ButtonText(uploadButtonRect, "WB_PawnCustomizeUploadPortraitButton".Translate()))
            {
                var fileSelector = new Dialog_FileSelector
                {
                    onSelectAction = path =>
                    {
                        customizationData.selectedImagePath = path;
                        if (!string.IsNullOrEmpty(imagePathToLoad)) portraitTextureCache.Remove(imagePathToLoad);
                    }
                };
                Find.WindowStack.Add(fileSelector);
            }
        }

        protected override void DrawDetailTab(Rect tabRect)
        {
            if (pawn.RaceProps.Humanlike)
            {
                NamePawn_DoWindowContents(tabRect);
            }
            else
            {
                DrawNonHumanlikeDetailsTab(tabRect);
            }
        }

        private void DrawNonHumanlikeDetailsTab(Rect tabRect)
        {
            float previewSize = 200f;
            float spacing = 15f;
            Rect previewRect = new Rect(tabRect.x, tabRect.y, previewSize, previewSize);
            Widgets.DrawBox(previewRect);
            Texture portraitTex = PortraitsCache.Get(pawn, new Vector2(previewSize, previewSize), Rot4.South, default, 1f, true, true, true, true);
            if (portraitTex != null && portraitTex != BaseContent.BadTex)
            {
                GUI.DrawTexture(previewRect.ContractedBy(2f), portraitTex, ScaleMode.ScaleToFit);
            }
            else
            {
                Widgets.Label(previewRect.ContractedBy(10f), "(Portrait Unavailable)");
            }

            float lineHeight = 30f;
            float explanationHeight = 60f;

            float labelY = previewRect.y;
            float explanationY = previewRect.yMax + 10f;
            Rect rightArea = new Rect(previewRect.xMax + spacing, labelY, tabRect.width - previewRect.width - spacing, tabRect.height);

            float currentY = rightArea.y;

            Widgets.Label(new Rect(rightArea.x, currentY, 100f, lineHeight), "WB_CustomizeLabel".Translate());
            Rect labelEditRect = new Rect(rightArea.x + 100f, currentY, rightArea.width - 100f - 70f, lineHeight);
            customizationData.nameOverride = new NameSingle(Widgets.TextField(labelEditRect, customizationData.nameOverride?.ToStringFull ?? pawn.Name
            ?.ToStringFull ?? pawn.LabelCap));

            Rect resetLabelButtonRect = new Rect(labelEditRect.xMax + 5f, currentY, 65f, lineHeight);
            if (Widgets.ButtonText(resetLabelButtonRect, "Reset".Translate()))
            {
                customizationData.nameOverride = null;
            }

            currentY += lineHeight + 10f;

            Widgets.Label(new Rect(rightArea.x, currentY, 100f, lineHeight), "WB_CustomizeDescription".Translate());
            Rect descriptionEditRect = new Rect(rightArea.x + 100f, currentY, rightArea.width - 100f - 70f, 100f);
            customizationData.descriptionOverride = Widgets.TextArea(descriptionEditRect, customizationData.descriptionOverride ?? pawn.DescriptionFlavor ?? pawn.def.description);

            Rect resetDescriptionButtonRect = new Rect(descriptionEditRect.xMax + 5f, currentY, 65f, lineHeight);
            if (Widgets.ButtonText(resetDescriptionButtonRect, "Reset".Translate()))
            {
                customizationData.descriptionOverride = pawn.def.description;
            }
            Rect explanationRect = new Rect(previewRect.x, explanationY, previewSize, explanationHeight);
            Widgets.Label(explanationRect, "WB_CustomizeDetailTabExplanation".Translate());
        }
        public void NamePawn_DoWindowContents(Rect inRect)
        {
            dialog ??= NamePawnDialog(pawn);
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            {
                Event.current.Use();
            }
            bool flag2 = false;
            bool forward = true;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Tab)
            {
                flag2 = true;
                forward = !Event.current.shift;
                Event.current.Use();
            }
            if (!dialog.firstCall && Event.current.type == EventType.Layout)
            {
                dialog.currentControl = GUI.GetNameOfFocusedControl();
            }
            RectAggregator rectAggregator = new RectAggregator(new Rect(inRect.x, inRect.y, inRect.width, 0f), 136098329, new Vector2(17f, 4f));
            if (!dialog.renameHeight.HasValue)
            {
                Text.Font = GameFont.Medium;
                dialog.renameHeight = Mathf.Ceil(dialog.renameText.RawText.GetHeightCached());
                Text.Font = GameFont.Small;
            }
            dialog.descriptionHeight = dialog.descriptionHeight ?? Mathf.Ceil(Text.CalcHeight(dialog.descriptionText, rectAggregator.Rect.width - dialog.portraitSize - 17f));
            float num = dialog.renameHeight.Value + 4f + dialog.descriptionHeight.Value;
            if (!pawn.RaceProps.Humanlike && dialog.portraitSize > num)
            {
                num = dialog.portraitSize;
            }
            RectDivider rectDivider = rectAggregator.NewRow(num);
            Text.Font = GameFont.Medium;
            RenderTexture image = PortraitsCache.Get(pawn, new Vector2(dialog.portraitSize, dialog.portraitSize), dialog.portraitDirection, default(Vector3), healthStateOverride: PawnHealthState.Mobile, cameraZoom: dialog.cameraZoom);
            Rect position = rectDivider.NewCol(dialog.portraitSize);
            if (pawn.RaceProps.Humanlike)
            {
                position.y += dialog.humanPortraitVerticalOffset;
            }
            position.height = dialog.portraitSize;
            GUI.DrawTexture(position, image);
            RectDivider rectDivider2 = rectDivider.NewRow(dialog.renameHeight.Value);
            Rect rect = rectDivider2.NewCol(dialog.renameHeight.Value, HorizontalJustification.Right);
            GUI.DrawTexture(rect, pawn.gender.GetIcon());
            TooltipHandler.TipRegion(rect, dialog.genderText);
            Widgets.Label(rectDivider2, dialog.renameText);
            Text.Font = GameFont.Small;
            Widgets.Label(rectDivider.NewRow(dialog.descriptionHeight.Value), dialog.descriptionText);
            Text.Anchor = TextAnchor.MiddleLeft;
            foreach (Dialog_NamePawn.NameContext name2 in dialog.names)
            {
                RectDivider divider = rectAggregator.NewRow(30f);
                name2.MakeRow(pawn, dialog.randomizeButtonWidth, dialog.randomizeText, dialog.suggestedText, ref divider, ref dialog.focusControlOverride);
            }
            Text.Anchor = TextAnchor.UpperLeft;
            rectAggregator.NewRow(17.5f);
            RectDivider rectDivider3 = rectAggregator.NewRow(35f);
            float width = Mathf.Floor((rectDivider3.Rect.width - 17f) / 2f);

            dialog.size = new Vector2(dialog.size.x, Mathf.Ceil(dialog.size.y + (rectAggregator.Rect.height - inRect.height)));
            SetInitialSizeAndPosition();
            if (flag2 || dialog.firstCall)
            {
                dialog.FocusNextControl(dialog.currentControl, forward);
                dialog.firstCall = false;
            }
            if (Event.current.type == EventType.Layout && !string.IsNullOrEmpty(dialog?.focusControlOverride))
            {
                GUI.FocusControl(dialog.focusControlOverride);
                dialog.focusControlOverride = null;
            }
        }

        protected override void SaveIndividualChanges()
        {
            if (pawn.RaceProps.Humanlike)
            {
                if (dialog == null)
                {
                    dialog = NamePawnDialog(pawn);
                }
                Name newName = dialog.BuildName();
                if (newName == null || !newName.IsValid)
                {
                    Messages.Message("NameIsInvalid".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
                    return;
                }
                else
                {
                    customizationData.nameOverride = newName;
                    pawn.Name = newName;
                }
            }
            else if (customizationData.nameOverride is not null)
            {
                pawn.Name = customizationData.nameOverride;
            }

            CustomizationDataCollections.thingCustomizationData[pawn] = customizationData.Copy();
            Messages.Message("WB_PawnCustomizeSaveSuccess".Translate(pawn.LabelShortCap), MessageTypeDefOf.PositiveEvent);
        }

        protected override void DrawBottomButtons(Rect inRect)
        {
            int numButtons = 4;
            float buttonWidth = 150f;
            float buttonHeight = 32f;
            float totalButtonWidth = buttonWidth * numButtons;
            float totalSpacing = inRect.width - totalButtonWidth - 30f;
            float buttonSpacing = (numButtons > 1) ? totalSpacing / (numButtons - 1) : 0f;
            float buttonY = inRect.yMax - buttonHeight;
            float currentButtonX = inRect.x + 15f;
            Rect worldButtonRect = new Rect(currentButtonX, buttonY, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(worldButtonRect, "WB_World".Translate()))
            {
                List<FloatMenuOption> worldOptions = new List<FloatMenuOption>();
                foreach (WorldPreset preset in WorldPresetManager.GetAllPresets())
                {
                    WorldPreset localPreset = preset;
                    worldOptions.Add(new FloatMenuOption("WB_PawnCustomizeSaveDefaultToPreset".Translate(pawn.def.label, localPreset.name), () =>
                    {
                        ShowSavePawnDefaultToPresetDialog(localPreset);
                    }));
                }
                worldOptions.Add(new FloatMenuOption("WB_SelectPresetCreateNewButton".Translate(), () =>
                {
                    Find.WindowStack.Add(new Window_CreateOrEditWorld());
                }));
                Find.WindowStack.Add(new FloatMenu(worldOptions));
            }
            currentButtonX += buttonWidth + buttonSpacing;

            Rect saveButtonRect = new Rect(currentButtonX, buttonY, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(saveButtonRect, "Save".Translate()))
            {
                SaveIndividualChanges();
            }
            if (ModsConfig.IsActive("ISOREX.PawnEditor"))
            {
                Rect pawnEditorButtonRect = new Rect(currentButtonX + buttonWidth / 2, buttonY - buttonHeight - 30, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(pawnEditorButtonRect, "Pawn Editor"))
                {
                    if (pawnEditorStaticType == null)
                    {
                        pawnEditorStaticType = AccessTools.TypeByName("PawnEditor.PawnEditor");
                        if (pawnEditorStaticType != null)
                        {
                            selectMethod = pawnEditorStaticType.GetMethod("Select", new[] { typeof(Pawn) });
                        }
                    }
                    selectMethod.Invoke(null, new object[] { pawn });
                    if (dialogType == null)
                    {
                        dialogType = AccessTools.TypeByName("PawnEditor.Dialog_PawnEditor_InGame");
                    }
                    var window = Activator.CreateInstance(dialogType) as Window;
                    if (window != null)
                    {
                        Find.WindowStack.Add(window);
                    }
                }
            }
            currentButtonX += buttonWidth + buttonSpacing;

            Rect mapActionsButtonRect = new Rect(currentButtonX, buttonY, buttonWidth, buttonHeight);
            List<FloatMenuOption> mapOptions = new List<FloatMenuOption>();

            mapOptions.Add(new FloatMenuOption("WB_PawnCustomizeSaveMapAll".Translate(pawn.kindDef.label), () =>
            {
                Name nameToApply = dialog.BuildName();
                if (nameToApply == null || !nameToApply.IsValid)
                {
                    Messages.Message("NameIsInvalid".Translate(), pawn, MessageTypeDefOf.RejectInput, historical: false);
                }
                else
                {
                    Dialog_MessageBox confirmationDialog = Dialog_MessageBox.CreateConfirmation(
                        "WB_PawnCustomizeSaveToAllConfirm".Translate(pawn.kindDef.label),
                        () =>
                        {
                            int count = 0;
                            CustomizationData dataToApply = customizationData.Copy();
                            dataToApply.nameOverride = nameToApply;
                            Map currentMap = Find.CurrentMap;
                            if (currentMap != null)
                            {
                                foreach (Pawn mapPawn in currentMap.mapPawns.AllPawnsSpawned.Where(p => p.kindDef == pawn.kindDef))
                                {
                                    mapPawn.Name = nameToApply;
                                    CustomizationDataCollections.thingCustomizationData[mapPawn] = dataToApply.Copy();
                                    count++;
                                }
                            }
                            Messages.Message("WB_PawnCustomizeSaveToAllSuccess".Translate(count, pawn.kindDef.label), MessageTypeDefOf.PositiveEvent);
                        }
                    );
                    Find.WindowStack.Add(confirmationDialog);
                }
            }));

            if (Widgets.ButtonText(mapActionsButtonRect, "WB_PawnCustomizeMapActions".Translate()))
            {
                Find.WindowStack.Add(new FloatMenu(mapOptions));
            }
            currentButtonX += buttonWidth + buttonSpacing;

            Rect resetButtonRect = new Rect(currentButtonX, buttonY, buttonWidth, buttonHeight);
            if (Widgets.ButtonText(resetButtonRect, "WB_CustomizeResetThing".Translate()))
            {
                if (Current.Game == null)
                {
                    Find.WindowStack.Add(new Dialog_MessageBox("WB_PawnCustomizeResetNeedsSave".Translate()));
                    return;
                }
                CustomizationDataCollections.thingCustomizationData.Remove(pawn);
                this.customizationData = CreateCustomization(pawn);
                pawn.Name = customizationData.originalPawnName;
                Messages.Message("WB_PawnCustomizeResetSuccess".Translate(pawn.LabelShortCap), MessageTypeDefOf.PositiveEvent);
            }
        }
        private void ShowSavePawnDefaultToPresetDialog(WorldPreset targetPreset)
        {
            string presetNameForMessage = targetPreset.name;
            Dialog_MessageBox confirmationDialog = Dialog_MessageBox.CreateConfirmation(
               "WB_PawnCustomizeSaveDefaultToPresetConfirm".Translate(pawn.def.label, presetNameForMessage),
               () =>
               {
                   if (targetPreset.customizationDefaults == null)
                   {
                       targetPreset.customizationDefaults = new Dictionary<ThingDef, CustomizationData>();
                   }
                   CustomizationData dataToSave = new CustomizationData();
                   dataToSave.selectedImagePath = customizationData.selectedImagePath;
                   dataToSave.narrativeText = customizationData.narrativeText;

                   targetPreset.customizationDefaults[pawn.def] = dataToSave;
                   bool savedSuccessfully = WorldPresetManager.SavePreset(targetPreset, null, null);

                   if (savedSuccessfully)
                   {
                       Messages.Message("WB_PawnCustomizeDefaultPresetSaveSuccess".Translate(pawn.def.label, presetNameForMessage), MessageTypeDefOf.PositiveEvent);
                   }
                   else
                   {
                       Messages.Message("WB_PawnCustomizeDefaultPresetSaveFailed".Translate(pawn.def.label, presetNameForMessage), MessageTypeDefOf.NegativeEvent);
                   }
               }
           );
            Find.WindowStack.Add(confirmationDialog);
        }
        private static Texture2D GetTextureForPreview(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                return null;
            }

            if (portraitTextureCache.TryGetValue(path, out Texture2D cachedTex))
            {
                return cachedTex;
            }

            try
            {
                byte[] fileData = File.ReadAllBytes(path);
                Texture2D newTex = new Texture2D(2, 2);
                if (newTex.LoadImage(fileData))
                {
                    portraitTextureCache[path] = newTex;
                    return newTex;
                }
                portraitTextureCache[path] = MissingTexture;
                return MissingTexture;
            }
            catch (System.Exception ex)
            {
                Log.Error($"Worldbuilder: Exception loading texture from {path} for pawn portrait: {ex.Message}");
                portraitTextureCache[path] = MissingTexture;
                return MissingTexture;
            }
        }
    }
}
