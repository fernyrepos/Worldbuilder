using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    internal sealed class Window_DefPicker<T> : Window where T : Def
    {
        private sealed class DefEntry
        {
            internal readonly T Def;
            internal readonly string Label;
            internal readonly string DefName;
            internal readonly string Description;
            internal readonly string SourceLabel;
            internal readonly string SourceKey;
            internal readonly Texture2D Icon;
            internal readonly Color IconColor;
            internal readonly AcceptanceReport Acceptance;

            internal DefEntry(
                T def,
                DefPickerPresentation<T> presentation)
            {
                Def = def;
                var customLabel = presentation?.labelGetter?.Invoke(def);
                Label = customLabel.NullOrEmpty()
                    ? DefLabel(def)
                    : customLabel;
                DefName = def.defName ?? string.Empty;
                var customTooltip = presentation?.tooltipGetter?.Invoke(def);
                Description = customTooltip.NullOrEmpty()
                    ? def.description.NullOrEmpty()
                        ? Label
                        : def.description.Trim()
                    : customTooltip;
                SourceLabel = GetSourceLabel(def.modContentPack);
                SourceKey = GetSourceKey(def.modContentPack);
                Icon = presentation?.iconGetter?.Invoke(def);
                IconColor = presentation?.iconColorGetter == null
                    ? Color.white
                    : presentation.iconColorGetter(def);
                Acceptance = presentation?.acceptanceGetter == null
                    ? true
                    : presentation.acceptanceGetter(def);
            }
        }

        private sealed class SourceGroup
        {
            internal readonly string Key;
            internal readonly string Label;
            internal readonly List<DefEntry> Defs;

            internal SourceGroup(
                string key,
                string label,
                IEnumerable<DefEntry> defs)
            {
                Key = key;
                Label = label;
                Defs = defs.ToList();
            }
        }

        private const float HeaderHeight = 32f;
        private const float SearchHeight = 28f;
        private const float SourceHeaderHeight = 34f;
        private const float CompactRowHeight = 32f;
        private const float DetailedRowHeight = 52f;
        private const float Gap = 10f;

        private readonly string title;
        private readonly string emptyLabel;
        private readonly Action<T> onSelect;
        private readonly List<SourceGroup> groups;
        private readonly bool showContentSource;
        private readonly bool iconAfterLabel;
        private readonly bool showInfoCard;
        private readonly Func<T, int> countGetter;
        private readonly HashSet<string> expandedSources =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly int totalDefs;

        private string searchText = string.Empty;
        private string cachedSearchText;
        private List<DefEntry> cachedFilteredDefs;
        private Vector2 scrollPosition;
        private float cachedGroupedHeight = -1f;
        private float cachedFilteredHeight = -1f;
        private int cachedExpandedCount = -1;

        internal Window_DefPicker(
            string title,
            IEnumerable<T> defs,
            Action<T> onSelect,
            string emptyLabel,
            DefPickerPresentation<T> presentation = null)
        {
            this.title = title;
            this.onSelect = onSelect;
            this.emptyLabel = emptyLabel;
            iconAfterLabel = presentation?.iconAfterLabel == true;
            countGetter = presentation?.countGetter;
            showInfoCard = presentation?.showInfoCard == true;
            showContentSource =
                WorldbuilderMod.settings?.showContentSourceOnScrollWindow == true;

            groups = (defs ?? Enumerable.Empty<T>())
                .Where(def => def != null)
                .Select(def => new DefEntry(def, presentation))
                .GroupBy(entry => entry.SourceKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => new SourceGroup(
                    group.Key,
                    group.First().SourceLabel,
                    group
                        .OrderBy(
                            entry => entry.Label,
                            StringComparer.OrdinalIgnoreCase)
                        .ThenBy(
                            entry => entry.DefName,
                            StringComparer.Ordinal)))
                .OrderBy(
                    group => group.Label,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    group => group.Key,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            totalDefs = groups.Sum(group => group.Defs.Count);
            if (showContentSource && groups.Count == 1)
            {
                expandedSources.Add(groups[0].Key);
            }

            doCloseX = true;
            closeOnCancel = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            draggable = true;
            forcePause = true;
        }

        public override Vector2 InitialSize => new Vector2(500f, 560f);

        public override void DoWindowContents(Rect inRect)
        {
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Medium;
            Widgets.Label(
                new Rect(inRect.x, inRect.y, inRect.width, HeaderHeight),
                title);

            Text.Font = GameFont.Small;
            var body = new Rect(
                inRect.x,
                inRect.y + HeaderHeight + Gap,
                inRect.width,
                inRect.height - HeaderHeight - Gap);
            Widgets.DrawMenuSection(body);
            var inner = body.ContractedBy(8f);

            var searchLabelRect = new Rect(
                inner.x,
                inner.y + 4f,
                62f,
                SearchHeight);
            Widgets.Label(
                searchLabelRect,
                "WB_DefPickerSearch".Translate());
            var searchRect = new Rect(
                searchLabelRect.xMax + 2f,
                inner.y,
                inner.width - searchLabelRect.width - 2f,
                SearchHeight);
            var oldSearchText = searchText;
            searchText = Widgets.TextField(
                searchRect,
                searchText ?? string.Empty);
            if (!string.Equals(
                    oldSearchText,
                    searchText,
                    StringComparison.Ordinal))
            {
                scrollPosition = Vector2.zero;
            }

            var searching = !string.IsNullOrWhiteSpace(searchText);
            var filtered = searching || !showContentSource
                ? FilteredDefs()
                : null;
            var countRect = new Rect(
                inner.x,
                searchRect.yMax + 4f,
                inner.width,
                20f);
            var oldColor = GUI.color;
            GUI.color = Color.gray;
            Widgets.Label(
                countRect,
                searching
                    ? "WB_DefPickerShown".Translate(
                        filtered?.Count ?? 0,
                        totalDefs)
                    : showContentSource
                        ? "WB_DefPickerSources".Translate(
                            groups.Count,
                            ExpandedDefCount(),
                            totalDefs)
                        : "WB_DefPickerEntries".Translate(totalDefs));
            GUI.color = oldColor;

            var outRect = new Rect(
                inner.x,
                countRect.yMax + 4f,
                inner.width,
                inner.yMax - countRect.yMax - 4f);
            var drawFlat = searching || !showContentSource;
            var contentHeight = drawFlat
                ? Mathf.Max(FilteredHeight(filtered), outRect.height)
                : Mathf.Max(GroupedHeight(), outRect.height);
            var viewRect = new Rect(
                0f,
                0f,
                outRect.width - 16f,
                contentHeight);
            Widgets.BeginScrollView(
                outRect,
                ref scrollPosition,
                viewRect);

            if (groups.Count == 0)
            {
                GUI.color = Color.gray;
                Widgets.Label(
                    new Rect(4f, 4f, viewRect.width - 8f, 24f),
                    emptyLabel);
                GUI.color = oldColor;
            }
            else if (drawFlat)
            {
                DrawFilteredDefs(
                    viewRect,
                    filtered,
                    outRect.height,
                    includeSource: searching && showContentSource);
            }
            else
            {
                DrawSourceGroups(
                    viewRect,
                    outRect.height);
            }

            Widgets.EndScrollView();
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private float GroupedHeight()
        {
            if (cachedGroupedHeight >= 0f)
            {
                return cachedGroupedHeight;
            }

            var height = 0f;
            foreach (var group in groups)
            {
                height += SourceHeaderHeight;
                if (expandedSources.Contains(group.Key))
                {
                    height += group.Defs.Count * CompactRowHeight;
                }
            }

            cachedGroupedHeight = height;
            return cachedGroupedHeight;
        }

        private void DrawSourceGroups(
            Rect viewRect,
            float visibleHeight)
        {
            var visibleTop = scrollPosition.y;
            var visibleBottom = visibleTop + visibleHeight;
            var y = 0f;

            foreach (var group in groups)
            {
                var expanded = expandedSources.Contains(group.Key);
                var headerText =
                    (expanded ? "- " : "+ ") +
                    group.Label +
                    " (" +
                    group.Defs.Count +
                    ")";
                var header = new Rect(
                    0f,
                    y,
                    viewRect.width,
                    SourceHeaderHeight - 2f);
                if (IsVisible(header, visibleTop, visibleBottom))
                {
                    Widgets.DrawHighlightIfMouseover(header);
                    DrawSingleLine(
                        new Rect(
                            header.x + 6f,
                            header.y + 6f,
                            header.width - 12f,
                            22f),
                        headerText);
                    if (Widgets.ButtonInvisible(header, true))
                    {
                        ToggleSource(group.Key);
                    }
                }

                y += SourceHeaderHeight;
                if (!expanded)
                {
                    continue;
                }

                for (var i = 0; i < group.Defs.Count; i++)
                {
                    var row = new Rect(
                        0f,
                        y,
                        viewRect.width,
                        CompactRowHeight - 2f);
                    if (IsVisible(row, visibleTop, visibleBottom))
                    {
                        DrawDefRow(
                            group.Defs[i],
                            i,
                            row,
                            includeSource: false);
                    }

                    y += CompactRowHeight;
                }
            }
        }

        private void DrawFilteredDefs(
            Rect viewRect,
            IReadOnlyList<DefEntry> defs,
            float visibleHeight,
            bool includeSource)
        {
            if (defs == null || defs.Count == 0)
            {
                var oldColor = GUI.color;
                GUI.color = Color.gray;
                Widgets.Label(
                    new Rect(4f, 4f, viewRect.width - 8f, 24f),
                    "WB_DefPickerNoMatches".Translate());
                GUI.color = oldColor;
                return;
            }

            var visibleTop = scrollPosition.y;
            var visibleBottom = visibleTop + visibleHeight;
            var y = 0f;
            for (var i = 0; i < defs.Count; i++)
            {
                var row = new Rect(
                    0f,
                    y,
                    viewRect.width,
                    (includeSource
                        ? DetailedRowHeight
                        : CompactRowHeight) - 2f);
                if (IsVisible(row, visibleTop, visibleBottom))
                {
                    DrawDefRow(
                        defs[i],
                        i,
                        row,
                        includeSource);
                }

                y += includeSource
                    ? DetailedRowHeight
                    : CompactRowHeight;
            }
        }

        private void DrawDefRow(
            DefEntry entry,
            int index,
            Rect row,
            bool includeSource)
        {
            if (index % 2 == 1)
            {
                Widgets.DrawLightHighlight(row);
            }

            Widgets.DrawHighlightIfMouseover(row);
            var oldColor = GUI.color;
            var accepted = entry.Acceptance.Accepted;
            GUI.color = accepted ? oldColor : Color.gray;

            var contentLeft = row.x + 18f;
            var contentRight = row.xMax - 6f;
            if (entry.Icon != null && !iconAfterLabel)
            {
                const float iconSize = 24f;
                var iconRect = new Rect(
                    row.x + 8f,
                    row.y + (row.height - iconSize) / 2f,
                    iconSize,
                    iconSize);
                GUI.color = accepted
                    ? entry.IconColor
                    : entry.IconColor * Color.gray;
                GUI.DrawTexture(
                    iconRect,
                    entry.Icon,
                    ScaleMode.ScaleToFit,
                    true);
                GUI.color = accepted ? oldColor : Color.gray;
                contentLeft = iconRect.xMax + 8f;
            }

            Rect infoCardRect = default;
            if (showInfoCard)
            {
                infoCardRect = new Rect(
                    row.xMax - 30f,
                    row.y + (row.height - 24f) / 2f,
                    24f,
                    24f);
                contentRight = infoCardRect.x - 4f;
                GUI.color = oldColor;
                Widgets.InfoCardButton(infoCardRect, entry.Def);
                GUI.color = accepted ? oldColor : Color.gray;
            }

            var label = entry.Label;
            if (countGetter != null)
            {
                var count = countGetter(entry.Def);
                if (count > 0)
                {
                    label = label + " (" + count + ")";
                }
            }

            var labelY = includeSource
                ? 5f
                : Mathf.Max(4f, (row.height - 22f) / 2f);
            var labelRect = new Rect(
                contentLeft,
                row.y + labelY,
                contentRight - contentLeft,
                22f);
            if (entry.Icon != null && iconAfterLabel)
            {
                const float iconSize = 22f;
                const float iconGap = 6f;
                labelRect.width = Mathf.Min(
                    Text.CalcSize(label).x,
                    Mathf.Max(0f, labelRect.width - iconSize - iconGap));
                DrawSingleLine(labelRect, label);

                var iconRect = new Rect(
                    labelRect.xMax + iconGap,
                    labelRect.y,
                    iconSize,
                    iconSize);
                GUI.color = accepted
                    ? entry.IconColor
                    : entry.IconColor * Color.gray;
                GUI.DrawTexture(
                    iconRect,
                    entry.Icon,
                    ScaleMode.ScaleToFit,
                    true);
                GUI.color = accepted ? oldColor : Color.gray;
            }
            else
            {
                DrawSingleLine(labelRect, label);
            }

            if (includeSource)
            {
                GUI.color = Color.gray;
                DrawSingleLine(
                    new Rect(
                        contentLeft,
                        row.y + 28f,
                        contentRight - contentLeft,
                        20f),
                    entry.SourceLabel);
            }

            GUI.color = oldColor;
            TooltipHandler.TipRegion(row, entry.Description);
            var selectRect = showInfoCard
                ? new Rect(
                    row.x,
                    row.y,
                    infoCardRect.x - row.x - 4f,
                    row.height)
                : row;
            if (accepted && Widgets.ButtonInvisible(selectRect, true))
            {
                onSelect(entry.Def);
                Close();
            }
        }

        private void ToggleSource(string sourceKey)
        {
            if (!expandedSources.Remove(sourceKey))
            {
                expandedSources.Add(sourceKey);
            }

            cachedGroupedHeight = -1f;
            cachedExpandedCount = -1;
        }

        private List<DefEntry> FilteredDefs()
        {
            var needle = searchText.Trim();
            if (cachedFilteredDefs == null ||
                !string.Equals(
                    cachedSearchText,
                    needle,
                    StringComparison.Ordinal))
            {
                cachedSearchText = needle;
                var terms = needle.Split(
                    new[] { ' ', '\t', '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries);
                var filtered = groups
                    .SelectMany(group => group.Defs)
                    .Where(entry => MatchesSearch(terms, entry));
                var ordered = showContentSource
                    ? filtered
                        .OrderBy(
                            entry => entry.SourceLabel,
                            StringComparer.OrdinalIgnoreCase)
                        .ThenBy(
                            entry => entry.Label,
                            StringComparer.OrdinalIgnoreCase)
                    : filtered.OrderBy(
                        entry => entry.Label,
                        StringComparer.OrdinalIgnoreCase);
                cachedFilteredDefs = ordered
                    .ThenBy(
                        entry => entry.DefName,
                        StringComparer.Ordinal)
                    .ToList();
                cachedFilteredHeight = -1f;
            }

            return cachedFilteredDefs;
        }

        private float FilteredHeight(
            IReadOnlyCollection<DefEntry> defs)
        {
            if (cachedFilteredHeight < 0f)
            {
                cachedFilteredHeight =
                    (defs?.Count ?? 0) *
                    (showContentSource &&
                     !string.IsNullOrWhiteSpace(searchText)
                        ? DetailedRowHeight
                        : CompactRowHeight);
            }

            return cachedFilteredHeight;
        }

        private int ExpandedDefCount()
        {
            if (cachedExpandedCount < 0)
            {
                cachedExpandedCount = groups
                    .Where(group => expandedSources.Contains(group.Key))
                    .Sum(group => group.Defs.Count);
            }

            return cachedExpandedCount;
        }

        private bool MatchesSearch(
            IEnumerable<string> terms,
            DefEntry entry)
        {
            foreach (var term in terms)
            {
                if (!Contains(entry.Label, term) &&
                    !Contains(entry.DefName, term) &&
                    !Contains(entry.Description, term) &&
                    (!showContentSource ||
                     !Contains(entry.SourceLabel, term) &&
                     !Contains(entry.SourceKey, term)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Contains(
            string value,
            string term)
        {
            return !value.NullOrEmpty() &&
                   value.IndexOf(
                       term,
                       StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string DefLabel(T def)
        {
            return def.label.NullOrEmpty()
                ? def.defName
                : def.LabelCap.ToString();
        }

        private static string GetSourceKey(ModContentPack source)
        {
            return source?.PackageId ?? "unknown";
        }

        private static string GetSourceLabel(ModContentPack source)
        {
            return source == null || source.Name.NullOrEmpty()
                ? "WB_DefPickerUnknownSource".Translate().ToString()
                : source.Name;
        }

        private static bool IsVisible(
            Rect row,
            float visibleTop,
            float visibleBottom)
        {
            return row.yMax >= visibleTop &&
                   row.y <= visibleBottom;
        }

        private static void DrawSingleLine(
            Rect rect,
            string text)
        {
            var oldWordWrap = Text.WordWrap;
            Text.WordWrap = false;
            Widgets.Label(
                rect,
                GenText.Truncate(text, rect.width));
            Text.WordWrap = oldWordWrap;
        }
    }
}
