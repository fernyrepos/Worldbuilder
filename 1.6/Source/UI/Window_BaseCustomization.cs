using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using LudeonTK;
using Verse.Sound;

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
        protected List<Color> cachedColors = new List<Color>();
        public override Vector2 InitialSize => new Vector2(800, 700);
        protected Window_BaseCustomization()
        {
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
            this.preventCameraMotion = false;
            this.closeOnAccept = false;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
            cachedColors = DefDatabase<ColorDef>.AllDefsListForReading.Select(c => c.color).ToList();
            cachedColors.AddRange(Find.FactionManager.AllFactionsVisible.Select(f => f.Color));
            cachedColors.SortByColor(c => c);
            DefsOf.WB_Customize.PlayOneShotOnCamera();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            var tabAreaRect = new Rect(inRect.x, inRect.y + 32f, inRect.width, 32f);
            var contentRect = new Rect(inRect.x, tabAreaRect.y, inRect.width, inRect.height - tabAreaRect.height - 32f - 15);

            List<TabRecord> tabsList = new List<TabRecord>
            {
                new TabRecord("WB_CustomizeAppearance".Translate(), () => currentTab = 0, currentTab == 0),
                new TabRecord("WB_CustomizeDetail".Translate(), () => currentTab = 1, currentTab == 1),
                new TabRecord("WB_CustomizeNarrative".Translate(), () => currentTab = 2, currentTab == 2)
            };

            TabDrawer.DrawTabs(tabAreaRect, tabsList, maxTabWidth: 300);

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
            customizationData.narrativeText = DevGUI.TextAreaScrollable(tabRect, customizationData.narrativeText, ref narrativeScrollPosition);
        }
        protected virtual void DrawBottomButtons(Rect inRect)
        {
            float buttonWidth = 150f;
            float buttonY = inRect.yMax - ButtonHeight - 15f;
            Rect saveButtonRect = new Rect(inRect.xMax - buttonWidth - 15f, buttonY, buttonWidth, ButtonHeight);
            if (Widgets.ButtonText(saveButtonRect, "Save".Translate()))
            {
                SaveIndividualChanges();
            }
        }
        protected abstract void SaveIndividualChanges();
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
                Find.WindowStack.Add(new Dialog_ChooseColor(
                    "WB_CustomizeChooseColor".Translate(),
                    Color.white,
                    cachedColors,
                    delegate (Color color)
                    {
                        onColorSelected(color);
                    }));
            }
        }
    }
}
