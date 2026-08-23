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

        private sealed class DefGroup
        {
            internal readonly string Key;
            internal readonly string Label;
            internal readonly int Order;
            internal readonly Texture2D Icon;
            internal readonly List<DefEntry> Defs;

            internal DefGroup(
                string key,
                string label,
                int order,
                Texture2D icon,
                IEnumerable<DefEntry> defs)
            {
                Key = key;
                Label = label;
                Order = order;
                Icon = icon;
                Defs = defs.ToList();
            }
        }

        private sealed class PickerState
        {
            internal bool Initialized;
            internal string SearchText = string.Empty;
            internal string SelectedGroupKey;
            internal Vector2 ScrollPosition;
            internal readonly HashSet<string> ExpandedGroups =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private const float HeaderHeight = 32f;
        private const float SearchHeight = 28f;
        private const float FilterHeight = 28f;
        private const float GroupHeaderHeight = 34f;
        private const float CompactRowHeight = 32f;
        private const float DetailedRowHeight = 52f;
        private const float Gap = 10f;

        private readonly string title;
        private readonly string emptyLabel;
        private readonly Action<T> onSelect;
        private readonly List<DefGroup> groups;
        private readonly bool useCustomGrouping;
        private readonly bool useGroupFilter;
        private readonly bool showContentSource;
        private readonly string groupFilterLabel;
        private readonly string allGroupsLabel;
        private readonly bool iconAfterLabel;
        private readonly bool showInfoCard;
        private readonly Func<T, int> countGetter;
        private readonly HashSet<string> expandedGroups =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly int totalDefs;
        private static readonly PickerState cachedState = new PickerState();

        private string searchText = string.Empty;
        private string selectedGroupKey;
        private string cachedSearchText;
        private string cachedGroupKey;
        private List<DefEntry> cachedFilteredDefs;
        private List<DefGroup> cachedFilteredGroups;
        private Vector2 scrollPosition;
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
            useGroupFilter =
                presentation?.useGroupFilter == true &&
                presentation.groupKeyGetter != null;
            useCustomGrouping =
                presentation?.groupKeyGetter != null &&
                !useGroupFilter;
            groupFilterLabel = presentation?.groupFilterLabel;
            allGroupsLabel = presentation?.allGroupsLabel;
            showContentSource =
                !useCustomGrouping &&
                !useGroupFilter &&
                WorldbuilderMod.settings?.showContentSourceOnScrollWindow == true;

            groups = (defs ?? Enumerable.Empty<T>())
                .Where(def => def != null)
                .Select(def => new DefEntry(def, presentation))
                .GroupBy(
                    entry => useCustomGrouping || useGroupFilter
                        ? presentation.groupKeyGetter(entry.Def) ?? string.Empty
                        : entry.SourceKey,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var first = group.First();
                    var customLabel = useCustomGrouping || useGroupFilter
                        ? presentation.groupLabelGetter?.Invoke(first.Def)
                        : null;
                    return new DefGroup(
                        group.Key,
                        customLabel.NullOrEmpty()
                            ? useCustomGrouping || useGroupFilter
                                ? group.Key
                                : first.SourceLabel
                            : customLabel,
                        useCustomGrouping || useGroupFilter
                            ? presentation.groupOrderGetter?.Invoke(first.Def) ?? 0
                            : 0,
                        useGroupFilter
                            ? presentation.groupIconGetter?.Invoke(first.Def)
                            : null,
                        group
                            .OrderBy(
                                entry => entry.Label,
                                StringComparer.OrdinalIgnoreCase)
                            .ThenBy(
                                entry => entry.DefName,
                                StringComparer.Ordinal));
                })
                .OrderBy(group => group.Order)
                .ThenBy(
                    group => group.Label,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    group => group.Key,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            totalDefs = groups.Sum(group => group.Defs.Count);
            if ((useCustomGrouping || showContentSource) && groups.Count == 1)
            {
                expandedGroups.Add(groups[0].Key);
            }

            doCloseX = true;
            closeOnCancel = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            draggable = true;
            resizeable = true;
            forcePause = true;
        }

        public override Vector2 InitialSize => new Vector2(500f, 560f);

        public override void PreOpen()
        {
            base.PreOpen();
            if (!cachedState.Initialized)
            {
                return;
            }

            searchText = cachedState.SearchText ?? string.Empty;
            scrollPosition = cachedState.ScrollPosition;
            expandedGroups.Clear();
            expandedGroups.UnionWith(cachedState.ExpandedGroups);
            selectedGroupKey = cachedState.SelectedGroupKey;
            if (!selectedGroupKey.NullOrEmpty() &&
                groups.All(group => !string.Equals(
                    group.Key,
                    selectedGroupKey,
                    StringComparison.OrdinalIgnoreCase)))
            {
                selectedGroupKey = null;
            }

            cachedExpandedCount = -1;
        }

        public override void PostClose()
        {
            cachedState.Initialized = true;
            cachedState.SearchText = searchText ?? string.Empty;
            cachedState.SelectedGroupKey = selectedGroupKey;
            cachedState.ScrollPosition = scrollPosition;
            cachedState.ExpandedGroups.Clear();
            cachedState.ExpandedGroups.UnionWith(expandedGroups);
            base.PostClose();
        }

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
                Mathf.Max(0f, inRect.width),
                Mathf.Max(0f, inRect.height - HeaderHeight - Gap));
            if (body.width <= 0f || body.height <= 0f)
            {
                return;
            }

            Widgets.DrawMenuSection(body);
            var inner = body.ContractedBy(8f);
            if (inner.width <= 0f || inner.height <= 0f)
            {
                return;
            }

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

            var nextRowY = searchRect.yMax + 4f;
            if (useGroupFilter)
            {
                DrawGroupFilter(new Rect(
                    inner.x,
                    nextRowY,
                    inner.width,
                    FilterHeight));
                nextRowY += FilterHeight + 4f;
            }

            var searching = !string.IsNullOrWhiteSpace(searchText);
            var filtering =
                useGroupFilter &&
                !selectedGroupKey.NullOrEmpty();
            var filtered = searching || filtering ||
                           !showContentSource && !useCustomGrouping
                ? FilteredDefs()
                : null;
            var drawGrouped = useCustomGrouping ||
                              showContentSource && !searching;
            var groupsToDraw = useCustomGrouping && searching
                ? FilteredGroups(filtered)
                : groups;
            var forceExpandedGroups = useCustomGrouping && searching;
            var countRect = new Rect(
                inner.x,
                nextRowY,
                inner.width,
                20f);
            var oldColor = GUI.color;
            GUI.color = Color.gray;
            Widgets.Label(
                countRect,
                searching || filtering
                    ? "WB_DefPickerShown".Translate(
                        filtered?.Count ?? 0,
                        totalDefs)
                    : useCustomGrouping
                        ? "WB_DefPickerCategories".Translate(
                            groups.Count,
                            ExpandedDefCount(),
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
                Mathf.Max(0f, inner.yMax - countRect.yMax - 4f));
            if (outRect.width <= 0f || outRect.height <= 0f)
            {
                return;
            }

            var rowHeight = !drawGrouped &&
                            searching &&
                            showContentSource
                ? DetailedRowHeight
                : CompactRowHeight;
            var grid = ResponsiveOptionGrid.Create(
                outRect,
                drawGrouped ? 0 : filtered?.Count ?? 0,
                rowHeight);
            if (drawGrouped)
            {
                grid = ResponsiveOptionGrid.CreateForContentHeight(
                    outRect,
                    GroupedHeight(
                        grid,
                        groupsToDraw,
                        forceExpandedGroups),
                    CompactRowHeight);
            }

            grid.ClampScrollPosition(ref scrollPosition);
            var viewRect = grid.ViewRect;
            Widgets.BeginScrollView(
                outRect,
                ref scrollPosition,
                viewRect);

            if (groupsToDraw.Count == 0)
            {
                GUI.color = Color.gray;
                Widgets.Label(
                    new Rect(4f, 4f, viewRect.width - 8f, 24f),
                    searching
                        ? "WB_DefPickerNoMatches".Translate().ToString()
                        : emptyLabel);
                GUI.color = oldColor;
            }
            else if (!drawGrouped)
            {
                DrawFilteredDefs(
                    grid,
                    filtered,
                    outRect.height,
                    includeSource: searching && showContentSource);
            }
            else
            {
                DrawGroups(
                    grid,
                    outRect.height,
                    groupsToDraw,
                    forceExpandedGroups);
            }

            Widgets.EndScrollView();
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private void DrawGroupFilter(Rect rect)
        {
            var selectedGroup = selectedGroupKey.NullOrEmpty()
                ? null
                : groups.FirstOrDefault(group => string.Equals(
                    group.Key,
                    selectedGroupKey,
                    StringComparison.OrdinalIgnoreCase));
            var selectedLabel = selectedGroup?.Label;
            if (selectedLabel.NullOrEmpty())
            {
                selectedLabel = allGroupsLabel;
            }

            var buttonLabel = groupFilterLabel.NullOrEmpty()
                ? selectedLabel
                : groupFilterLabel + ": " + selectedLabel;
            var selectedIcon = selectedGroup?.Icon;
            if (Widgets.ButtonText(
                    rect,
                    selectedIcon == null ? buttonLabel : string.Empty))
            {
                OpenGroupFilterMenu();
            }

            if (selectedIcon != null)
            {
                const float iconSize = 24f;
                const float iconLabelGap = 4f;
                const float horizontalPadding = 12f;
                var labelWidth = Mathf.Min(
                    Text.CalcSize(buttonLabel).x,
                    Mathf.Max(
                        1f,
                        rect.width - iconSize - iconLabelGap -
                        horizontalPadding));
                var contentWidth = iconSize + iconLabelGap + labelWidth;
                var iconRect = new Rect(
                    rect.center.x - contentWidth / 2f,
                    rect.y + (rect.height - iconSize) / 2f,
                    iconSize,
                    iconSize);
                var oldColor = GUI.color;
                GUI.color = Color.white;
                GUI.DrawTexture(
                    iconRect,
                    selectedIcon,
                    ScaleMode.ScaleToFit,
                    true);
                GUI.color = oldColor;

                var oldAnchor = Text.Anchor;
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(
                    new Rect(
                        iconRect.xMax + iconLabelGap,
                        rect.y,
                        labelWidth,
                        rect.height),
                    buttonLabel);
                Text.Anchor = oldAnchor;
            }
        }

        private void OpenGroupFilterMenu()
        {
            var options = new List<FloatMenuOption>
            {
                new FloatMenuOption(
                    allGroupsLabel,
                    () => SetGroupFilter(null))
            };
            foreach (var group in groups)
            {
                var groupKey = group.Key;
                options.Add(new FloatMenuOption(
                    group.Label,
                    () => SetGroupFilter(groupKey)));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void SetGroupFilter(string groupKey)
        {
            if (string.Equals(
                    selectedGroupKey,
                    groupKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            selectedGroupKey = groupKey;
            scrollPosition = Vector2.zero;
            cachedFilteredDefs = null;
            cachedFilteredGroups = null;
        }

        private float GroupedHeight(
            ResponsiveOptionGrid grid,
            IReadOnlyList<DefGroup> groupsToDraw,
            bool forceExpanded)
        {
            var height = 0f;
            foreach (var group in groupsToDraw)
            {
                height += GroupHeaderHeight;
                if (forceExpanded || expandedGroups.Contains(group.Key))
                {
                    height += grid.RowsHeight(group.Defs.Count);
                }
            }

            return height;
        }

        private void DrawGroups(
            ResponsiveOptionGrid grid,
            float visibleHeight,
            IReadOnlyList<DefGroup> groupsToDraw,
            bool forceExpanded)
        {
            var visibleTop = scrollPosition.y;
            var visibleBottom = visibleTop + visibleHeight;
            var y = 0f;

            foreach (var group in groupsToDraw)
            {
                var expanded = forceExpanded ||
                               expandedGroups.Contains(group.Key);
                var headerText =
                    (forceExpanded
                        ? string.Empty
                        : expanded
                            ? "- "
                            : "+ ") +
                    group.Label +
                    " (" +
                    group.Defs.Count +
                    ")";
                var header = new Rect(
                    0f,
                    y,
                    grid.ViewRect.width,
                    GroupHeaderHeight - 2f);
                if (IsVisible(header, visibleTop, visibleBottom))
                {
                    if (!forceExpanded)
                    {
                        Widgets.DrawHighlightIfMouseover(header);
                    }

                    DrawSingleLine(
                        new Rect(
                            header.x + 6f,
                            header.y + 6f,
                            header.width - 12f,
                            22f),
                        headerText);
                    if (!forceExpanded &&
                        Widgets.ButtonInvisible(header, true))
                    {
                        ToggleGroup(group.Key);
                    }
                }

                y += GroupHeaderHeight;
                if (!expanded)
                {
                    continue;
                }

                for (var i = 0; i < group.Defs.Count; i++)
                {
                    var row = grid.RowRect(i);
                    row.y += y;
                    if (IsVisible(row, visibleTop, visibleBottom))
                    {
                        DrawDefRow(
                            group.Defs[i],
                            row,
                            includeSource: false,
                            alternatingRow: grid.IsAlternatingRow(i));
                    }
                }

                y += grid.RowsHeight(group.Defs.Count);
            }
        }

        private void DrawFilteredDefs(
            ResponsiveOptionGrid grid,
            IReadOnlyList<DefEntry> defs,
            float visibleHeight,
            bool includeSource)
        {
            if (defs == null || defs.Count == 0)
            {
                var oldColor = GUI.color;
                GUI.color = Color.gray;
                Widgets.Label(
                    new Rect(4f, 4f, grid.ViewRect.width - 8f, 24f),
                    "WB_DefPickerNoMatches".Translate());
                GUI.color = oldColor;
                return;
            }

            var visibleTop = scrollPosition.y;
            var visibleBottom = visibleTop + visibleHeight;
            for (var i = 0; i < defs.Count; i++)
            {
                var row = grid.RowRect(i);
                if (IsVisible(row, visibleTop, visibleBottom))
                {
                    DrawDefRow(
                        defs[i],
                        row,
                        includeSource,
                        grid.IsAlternatingRow(i));
                }
            }
        }

        private void DrawDefRow(
            DefEntry entry,
            Rect row,
            bool includeSource,
            bool alternatingRow)
        {
            if (alternatingRow)
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

        private void ToggleGroup(string groupKey)
        {
            if (!expandedGroups.Remove(groupKey))
            {
                expandedGroups.Add(groupKey);
            }

            cachedExpandedCount = -1;
        }

        private List<DefEntry> FilteredDefs()
        {
            var needle = searchText.Trim();
            if (cachedFilteredDefs == null ||
                !string.Equals(
                    cachedSearchText,
                    needle,
                    StringComparison.Ordinal) ||
                !string.Equals(
                    cachedGroupKey,
                    selectedGroupKey,
                    StringComparison.OrdinalIgnoreCase))
            {
                cachedSearchText = needle;
                cachedGroupKey = selectedGroupKey;
                var terms = needle.Split(
                    new[] { ' ', '\t', '\r', '\n' },
                    StringSplitOptions.RemoveEmptyEntries);
                var filtered = groups
                    .Where(group =>
                        !useGroupFilter ||
                        selectedGroupKey.NullOrEmpty() ||
                        string.Equals(
                            group.Key,
                            selectedGroupKey,
                            StringComparison.OrdinalIgnoreCase))
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
                cachedFilteredGroups = null;
            }

            return cachedFilteredDefs;
        }

        private List<DefGroup> FilteredGroups(
            IReadOnlyCollection<DefEntry> filteredDefs)
        {
            if (cachedFilteredGroups != null)
            {
                return cachedFilteredGroups;
            }

            var includedDefs = new HashSet<DefEntry>(
                filteredDefs ?? Enumerable.Empty<DefEntry>());
            cachedFilteredGroups = groups
                .Select(group => new DefGroup(
                    group.Key,
                    group.Label,
                    group.Order,
                    group.Icon,
                    group.Defs.Where(includedDefs.Contains)))
                .Where(group => group.Defs.Count > 0)
                .ToList();
            return cachedFilteredGroups;
        }

        private int ExpandedDefCount()
        {
            if (cachedExpandedCount < 0)
            {
                cachedExpandedCount = groups
                    .Where(group => expandedGroups.Contains(group.Key))
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
