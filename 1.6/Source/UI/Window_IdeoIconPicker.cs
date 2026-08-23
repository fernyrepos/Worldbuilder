using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public class Window_IdeoIconPicker : Window
    {
        private readonly Action<IdeoIconDef> onSelected;
        private readonly IdeoIconDef currentIcon;
        private readonly List<IdeoIconDef> icons;

        private Vector2 scrollPosition = Vector2.zero;
        private string searchText = "";

        private const float IconSize = 58f;
        private const float IconGap = 8f;

        public override Vector2 InitialSize => new Vector2(560f, 520f);

        public static bool Available => ModsConfig.IdeologyActive && DefDatabase<IdeoIconDef>.AllDefsListForReading.Any();

        public Window_IdeoIconPicker(IdeoIconDef currentIcon, Action<IdeoIconDef> onSelected)
        {
            this.currentIcon = currentIcon;
            this.onSelected = onSelected;
            this.icons = DefDatabase<IdeoIconDef>.AllDefsListForReading.OrderBy(i => i.defName).ToList();

            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            var titleRect = new Rect(inRect.x, inRect.y, inRect.width, 34f);
            Widgets.Label(titleRect, "WB_StoryChooseIcon".Translate());
            Text.Font = GameFont.Small;

            float y = titleRect.yMax + 4f;

            var searchRect = new Rect(inRect.x, y, inRect.width - 130f, 28f);
            searchText = Widgets.TextField(searchRect, searchText);
            if (searchText.NullOrEmpty())
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = new Color(1f, 1f, 1f, 0.4f);
                Widgets.Label(new Rect(searchRect.x + 6f, searchRect.y, searchRect.width - 12f, searchRect.height), "WB_Search".Translate());
                GUI.color = Color.white;
                Text.Anchor = TextAnchor.UpperLeft;
            }

            if (Widgets.ButtonText(new Rect(searchRect.xMax + 8f, y, 122f, 28f), "WB_StoryNoIcon".Translate()))
            {
                onSelected(null);
                Close();
            }
            y += 36f;

            var gridRect = new Rect(inRect.x, y, inRect.width, inRect.yMax - y);
            DrawIconGrid(gridRect);
        }

        private void DrawIconGrid(Rect rect)
        {
            var filtered = searchText.NullOrEmpty()
                ? icons
                : icons.Where(i => i.defName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            if (filtered.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(rect, "WB_StoryNoIconsFound".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            int perRow = Mathf.Max(1, Mathf.FloorToInt((rect.width - 16f) / (IconSize + IconGap)));
            int rows = Mathf.CeilToInt((float)filtered.Count / perRow);

            var viewRect = new Rect(0f, 0f, rect.width - 16f, rows * (IconSize + IconGap));
            Widgets.BeginScrollView(rect, ref scrollPosition, viewRect);

            for (int i = 0; i < filtered.Count; i++)
            {
                var iconRect = new Rect(
                    (i % perRow) * (IconSize + IconGap),
                    (i / perRow) * (IconSize + IconGap),
                    IconSize,
                    IconSize);

                if (iconRect.yMax < scrollPosition.y || iconRect.y > scrollPosition.y + rect.height)
                {
                    continue;
                }

                Widgets.DrawOptionBackground(iconRect, filtered[i] == currentIcon);

                var texture = filtered[i].Icon;
                if (texture != null)
                {
                    Widgets.DrawTextureFitted(iconRect.ContractedBy(8f), texture, 1f);
                }

                TooltipHandler.TipRegion(iconRect, filtered[i].defName);
                if (Widgets.ButtonInvisible(iconRect, doMouseoverSound: false))
                {
                    onSelected(filtered[i]);
                    Close();
                }
            }

            Widgets.EndScrollView();
        }
    }
}
