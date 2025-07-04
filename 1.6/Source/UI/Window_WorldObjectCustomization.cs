using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;


namespace Worldbuilder
{
    [HotSwappable]
    public abstract class Window_WorldObjectCustomization : Window_BaseCustomization
    {
        protected Vector2 factionIconScrollPosition = Vector2.zero;
        protected Vector2 culturalIconScrollPosition = Vector2.zero;
        protected Color? selectedColor;
        protected readonly List<FactionDef> availableFactionIcons;
        protected FactionDef selectedFactionIconDef;
        protected IdeoIconDef selectedCulturalIconDef;
        protected readonly List<IdeoIconDef> availableCulturalIcons;
        protected bool showingFactionIcons = true;
        protected const float IconSize = 64f;
        protected const float IconPadding = 10f;

        public Window_WorldObjectCustomization() : base()
        {
            availableFactionIcons = DefDatabase<FactionDef>.AllDefsListForReading
                .Where(f => !f.factionIconPath.NullOrEmpty() && ContentFinder<Texture2D>.Get(f.factionIconPath, false) != null)
                .OrderBy(f => f.defName)
                .ToList();

            availableCulturalIcons = DefDatabase<IdeoIconDef>.AllDefsListForReading
                .OrderBy(i => i.defName)
                .ToList();
        }

        protected override void DrawAppearanceTab(Rect tabRect)
        {
            float buttonHeight = 32f;
            float tabY = tabRect.y;
            float tabWidth = 200f;
            float buttonGap = 5f;
            Rect factionButtonRect = new Rect(tabRect.x, tabY, tabWidth - 15, buttonHeight);
            if (Widgets.ButtonText(factionButtonRect, "WB_ColonyCustomizeFactionIconsLabel".Translate()))
            {
                showingFactionIcons = true;
            }
            Rect culturalButtonRect = new Rect(tabRect.x, factionButtonRect.yMax + buttonGap, tabWidth - 15, buttonHeight);
            if (Widgets.ButtonText(culturalButtonRect, "WB_ColonyCustomizeCulturalIconsLabel".Translate()))
            {
                showingFactionIcons = false;
            }
            Rect labelRect = new Rect(tabRect.x + tabWidth, tabY, tabRect.width - tabWidth, 24f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(labelRect, "WB_SetFactionIcon".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            float iconGridHeight = tabRect.height - 30f;
            Rect iconGridRect = new Rect(tabRect.x + tabWidth + 5, labelRect.yMax + 5f, tabRect.width - tabWidth, iconGridHeight);

            if (showingFactionIcons)
            {
                DrawFactionIconSelectorGrid(iconGridRect, availableFactionIcons, ref selectedFactionIconDef, ref factionIconScrollPosition);
            }
            else
            {
                DrawCulturalIconSelectorGrid(iconGridRect, availableCulturalIcons, ref selectedCulturalIconDef, ref culturalIconScrollPosition);
            }

            DrawColorSelector(
                culturalButtonRect.x,
                culturalButtonRect.yMax + 15,
                tabWidth - 15,
                selectedColor,
                newColor => selectedColor = newColor
            );
        }

        protected void DrawCulturalIconSelectorGrid(Rect rect, List<IdeoIconDef> iconDefs, ref IdeoIconDef selectedDef, ref Vector2 scrollPos)
        {
            int iconsPerRow = Mathf.FloorToInt(rect.width / (IconSize + IconPadding));
            if (iconsPerRow < 1) iconsPerRow = 1;

            float totalGridHeight = Mathf.Ceil((float)iconDefs.Count / iconsPerRow) * (IconSize + IconPadding);
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, totalGridHeight);

            Widgets.BeginScrollView(rect, ref scrollPos, viewRect);

            for (int i = 0; i < iconDefs.Count; i++)
            {
                int row = i / iconsPerRow;
                int col = i % iconsPerRow;

                float x = col * (IconSize + IconPadding);
                float y = row * (IconSize + IconPadding);

                Rect iconRect = new Rect(x + 5, y + 5, IconSize, IconSize);
                Widgets.DrawOptionBackground(iconRect, selectedDef == iconDefs[i]);

                Texture2D iconTex = ContentFinder<Texture2D>.Get(iconDefs[i].iconPath, false);
                if (iconTex != null)
                {
                    Color originalColor = GUI.color;
                    if (selectedColor.HasValue)
                    {
                        GUI.color = selectedColor.Value;
                    }

                    GUI.DrawTexture(iconRect.ContractedBy(4f), iconTex, ScaleMode.ScaleToFit);
                    GUI.color = originalColor;
                }

                if (Widgets.ButtonInvisible(iconRect))
                {
                    selectedDef = iconDefs[i];
                }
                TooltipHandler.TipRegion(iconRect, iconDefs[i].defName);
            }

            Widgets.EndScrollView();
        }

        protected void DrawFactionIconSelectorGrid(Rect rect, List<FactionDef> iconDefs, ref FactionDef selectedDef, ref Vector2 scrollPos)
        {
            int iconsPerRow = Mathf.FloorToInt(rect.width / (IconSize + IconPadding));
            if (iconsPerRow < 1) iconsPerRow = 1;

            float totalGridHeight = Mathf.Ceil((float)iconDefs.Count / iconsPerRow) * (IconSize + IconPadding);
            Rect viewRect = new Rect(0f, 0f, rect.width - 16f, totalGridHeight);

            Widgets.BeginScrollView(rect, ref scrollPos, viewRect);

            for (int i = 0; i < iconDefs.Count; i++)
            {
                int row = i / iconsPerRow;
                int col = i % iconsPerRow;

                float x = col * (IconSize + IconPadding);
                float y = row * (IconSize + IconPadding);

                Rect iconRect = new Rect(x + 5, y + 5, IconSize, IconSize);
                Widgets.DrawOptionBackground(iconRect, selectedDef == iconDefs[i]);

                Texture2D iconTex = ContentFinder<Texture2D>.Get(iconDefs[i].factionIconPath, false);
                if (iconTex != null)
                {
                    Color originalColor = GUI.color;
                    if (selectedColor.HasValue)
                    {
                        GUI.color = selectedColor.Value;
                    }

                    GUI.DrawTexture(iconRect.ContractedBy(4f), iconTex, ScaleMode.ScaleToFit);
                    GUI.color = originalColor;
                }

                if (Widgets.ButtonInvisible(iconRect))
                {
                    selectedDef = iconDefs[i];
                }
                TooltipHandler.TipRegion(iconRect, iconDefs[i].LabelCap);
            }

            Widgets.EndScrollView();
        }
    }
}
