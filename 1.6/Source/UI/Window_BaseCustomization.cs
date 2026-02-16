using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using LudeonTK;
using Verse.Sound;
using RimWorld;

namespace Worldbuilder
{
    [HotSwappable]
    public abstract class Window_BaseCustomization : Window
    {
        protected CustomizationData customizationData;
        protected int currentTab = 0;
        protected Vector2 narrativeScrollPosition = Vector2.zero;
        protected const float ButtonHeight = 32f;
        protected const float StandardSpacing = 15f;
        protected const float PreviewSize = 200f;
        protected virtual bool ShowNarrativeTab => true;
        public override Vector2 InitialSize => new Vector2(800, 700);
        protected Window_BaseCustomization()
        {
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.preventCameraMotion = false;
            this.closeOnAccept = false;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            DefsOf.WB_Customize.PlayOneShotOnCamera();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            var tabAreaRect = new Rect(inRect.x, inRect.y + 32f, inRect.width, 32f);
            var contentRect = new Rect(inRect.x, tabAreaRect.y, inRect.width, inRect.height - tabAreaRect.height - 32f - 15);

            var tabsList = new List<TabRecord>
            {
                new TabRecord("WB_CustomizeAppearance".Translate(), () => currentTab = 0, currentTab == 0),
                new TabRecord("WB_CustomizeDetail".Translate(), () => currentTab = 1, currentTab == 1)
            };
            if (ShowNarrativeTab)
            {
                tabsList.Add(new TabRecord("WB_CustomizeNarrative".Translate(), () => currentTab = 2, currentTab == 2));
            }

            float maxTabWidth = tabAreaRect.width / tabsList.Count;
            TabDrawer.DrawTabs(tabAreaRect, tabsList, maxTabWidth: maxTabWidth);

            Widgets.DrawMenuSection(contentRect);
            var innerContentRect = contentRect.ContractedBy(15f);
            switch (currentTab)
            {
                case 0:
                    DrawAppearanceTab(innerContentRect);
                    break;
                case 1:
                    DrawDetailTab(innerContentRect);
                    break;
                case 2:
                    DrawNarrativeTab(innerContentRect);
                    break;
            }
            DrawBottomButtons(inRect);
        }
        protected abstract void DrawAppearanceTab(Rect tabRect);
        protected abstract void DrawDetailTab(Rect tabRect);
        protected virtual void DrawNarrativeTab(Rect tabRect)
        {
            float syncUIHeight = 180f;
            var narrativeEditRect = new Rect(tabRect.x, tabRect.y + 15, tabRect.width, tabRect.height - 60 - syncUIHeight);
            customizationData.narrativeText = DevGUI.TextAreaScrollable(narrativeEditRect, customizationData.narrativeText, ref narrativeScrollPosition);

            Rect syncRect = new Rect(narrativeEditRect.x, narrativeEditRect.yMax, narrativeEditRect.width, syncUIHeight);
            DrawFileSyncUI(syncRect);
        }

        protected void DrawFileSyncUI(Rect syncRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(syncRect);

            Text.Font = GameFont.Small;
            listing.Label("WB_FileSyncing".Translate());
            listing.Gap(5f);

            Rect lineRect = listing.GetRect(30f);
            Rect checkRect = lineRect.LeftPartPixels(30f);
            Rect buttonRect = new Rect(checkRect.xMax + 5f, lineRect.y, syncRect.width - checkRect.width - 5f, 30f);

            Texture2D checkTex = customizationData.syncToExternalFile ? Widgets.CheckboxOnTex : Widgets.CheckboxOffTex;
            if (Widgets.ButtonImage(checkRect.ContractedBy(3f), checkTex))
            {
                customizationData.syncToExternalFile = !customizationData.syncToExternalFile;
            }

            if (Widgets.ButtonText(buttonRect, "WB_SelectFile".Translate()))
            {
                var fileSelector = new Dialog_FileSelector() { searchPattern = "*.txt" };
                fileSelector.onSelectAction = (string path) =>
                {
                    customizationData.syncedFilePath = path;
                    customizationData.syncToExternalFile = true;
                };
                Find.WindowStack.Add(fileSelector);
            }

            if (!string.IsNullOrEmpty(customizationData.syncedFilePath))
            {
                Rect pathLabelRect = new Rect(lineRect.x, lineRect.yMax, lineRect.width, 55f);
                Widgets.Label(pathLabelRect, System.IO.Path.GetFullPath(customizationData.syncedFilePath));
                listing.Gap(55);
            }

            listing.Gap(5f);
            Text.Font = GameFont.Tiny;
            listing.Label("WB_FileSyncingDescription".Translate());
            Text.Font = GameFont.Small;
            listing.End();
        }
        protected virtual void DrawBottomButtons(Rect inRect)
        {
            float buttonWidth = 150f;
            float buttonY = inRect.yMax - ButtonHeight - 15f;
            var saveButtonRect = new Rect(inRect.xMax - buttonWidth - 15f, buttonY, buttonWidth, ButtonHeight);
            if (Widgets.ButtonText(saveButtonRect, "Save".Translate()))
            {
                SaveIndividualChanges();
            }
        }
        protected virtual void SaveIndividualChanges()
        {
            if (customizationData.syncToExternalFile && !string.IsNullOrEmpty(customizationData.syncedFilePath))
            {
                try
                {
                    System.IO.File.WriteAllText(customizationData.syncedFilePath, customizationData.narrativeText);
                    Messages.Message("WB_NarrativeSynced".Translate(), MessageTypeDefOf.TaskCompletion, false);
                }
                catch (System.Exception ex)
                {
                    Log.Error($"Worldbuilder: Failed to sync narrative to file: {ex.Message}");
                    Messages.Message("WB_NarrativeSyncFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                }
            }
        }
        protected void DrawLabelBelowThumbnail(Rect thumbnailRect, string label)
        {
            Text.Font = GameFont.Small;
            var labelBox = new Rect(thumbnailRect.x, thumbnailRect.yMax + 2, thumbnailRect.width, 22);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(labelBox, label);
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        protected void DrawColorSelector(float x, float y, float width, Color? currentColor, Action<Color?> onColorSelected)
        {
            float initialY = y;
            var colorBlock = new Rect(x, initialY, 50f, 50f);

            bool colorIsCurrentlySet = currentColor.HasValue;
            Widgets.DrawBoxSolid(colorBlock, colorIsCurrentlySet ? currentColor.Value : Color.clear);

            float checkboxY = initialY - 5f;

            bool enableColoringCheckboxState = colorIsCurrentlySet;
            Widgets.CheckboxLabeled(new Rect(colorBlock.xMax + 5f, checkboxY, width - colorBlock.width, 30f), "WB_CustomizeEnableColoring".Translate(), ref enableColoringCheckboxState);

            if (enableColoringCheckboxState && !colorIsCurrentlySet)
            {
                onColorSelected(Color.white);
            }
            else if (!enableColoringCheckboxState && colorIsCurrentlySet)
            {
                onColorSelected(null);
            }

            float buttonY = checkboxY + 30f;

            if (Widgets.ButtonText(new Rect(colorBlock.xMax + 5f, buttonY, width - colorBlock.width - 5f, 30f), "WB_CustomizeSetColor".Translate()))
            {
                Find.WindowStack.Add(new Window_ColorPicker(currentColor ?? Color.white, delegate (Color color)
                {
                    onColorSelected(color);
                }));
            }
        }
    }
}
