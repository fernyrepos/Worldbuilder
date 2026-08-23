using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    internal sealed class Window_TileBrushEditor : Window
    {
        private const float StandardGap = 8f;
        private const float MinimumWidth = 480f;
        private const float MinimumHeight = 402f;
        private const float ActionRowHeight = 32f;
        private const float OptionRowHeight = 28f;
        private const float OptionThumbnailSize = 24f;
        private const float FavoriteStarSize = 20f;

        private readonly TileBrushSession session;
        private readonly HashSet<string> favoriteKeys =
            new HashSet<string>(StringComparer.Ordinal);
        private Vector2 optionScrollPosition;
        private string optionSearch = string.Empty;
        private IReadOnlyList<Def> cachedFilterOptions;
        private IReadOnlyList<Def> cachedVisibleOptionsSource;
        private IReadOnlyList<Def> cachedVisibleOptions;
        private string cachedFilterSearch;
        private bool cachedFilterShowsContentSource;
        private bool cachedVisibleOptionsHideNonRenderedTerrain;
        private TileBrushToolKind cachedFilterToolKind;
        private int cachedFilterFavoriteRevision = -1;
        private List<Def> cachedFilteredOptions;
        private int favoriteRevision;
        private bool hasCachedWindowRect;
        private Rect cachedWindowRect;

        internal Window_TileBrushEditor(TileBrushSession session)
        {
            this.session = session;
            layer = WindowLayer.GameUI;
            forcePause = true;
            preventCameraMotion = false;
            absorbInputAroundWindow = false;
            closeOnAccept = false;
            closeOnCancel = true;
            doCloseX = true;
            draggable = true;
            resizeable = true;
            onlyOneOfTypeAllowed = true;
        }

        private TileBrushSettings Settings => session.Settings;
        private TileBrushController Controller => session.Controller;

        internal Map EditedMap => session.Map;

        public override Vector2 InitialSize => new Vector2(600f, 542f);

        public override void WindowOnGUI()
        {
            windowRect = ClampWindowRect(windowRect);
            base.WindowOnGUI();
        }

        public override void PreOpen()
        {
            base.PreOpen();
            if (hasCachedWindowRect)
            {
                windowRect = ClampWindowRect(cachedWindowRect);
            }

            RefreshFavoriteKeys();
            session.Activate();
        }

        public override void PostClose()
        {
            cachedWindowRect = windowRect;
            hasCachedWindowRect = true;
            session.NotifyEditorClosed();
            base.PostClose();
        }

        public override void DoWindowContents(Rect inRect)
        {
            const float dropdownHeight = 32f;
            var actionWidth = Mathf.Clamp(
                inRect.width * 0.31f,
                155f,
                210f);
            var leftWidth = Mathf.Max(
                220f,
                inRect.width - actionWidth - StandardGap);
            actionWidth = inRect.width - leftWidth - StandardGap;

            DrawToolDropdown(new Rect(
                0f,
                0f,
                leftWidth,
                dropdownHeight));
            DrawShapeDropdown(new Rect(
                0f,
                dropdownHeight + StandardGap,
                leftWidth,
                dropdownHeight));

            var contentTop = 2f * (dropdownHeight + StandardGap);
            var contentHeight = Mathf.Max(
                180f,
                inRect.height - contentTop);
            DrawOptionsPanel(new Rect(
                0f,
                contentTop,
                leftWidth,
                contentHeight));
            DrawActionColumn(new Rect(
                leftWidth + StandardGap,
                contentTop,
                actionWidth,
                contentHeight));
        }

        private void DrawToolDropdown(Rect rect)
        {
            if (!Widgets.ButtonText(
                    rect,
                    "WB_TileBrushFeatureDropdown".Translate(
                        Settings.Tool.Label)))
            {
                return;
            }

            var options = AvailableTools()
                .Select(tool =>
                    new FloatMenuOption(
                        tool.Label,
                        () => SelectTool(tool.Kind)))
                .ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static IEnumerable<ITileBrushTool> AvailableTools()
        {
            return TileBrushToolRegistry.All.Where(tool =>
                tool.Kind != TileBrushToolKind.Pollution ||
                ModsConfig.BiotechActive);
        }

        private void DrawShapeDropdown(Rect rect)
        {
            if (!Widgets.ButtonText(
                    rect,
                    "WB_TileBrushBrushShapeDropdown".Translate(
                        ShapeLabel(Settings.Shape))))
            {
                return;
            }

            var options = Enum
                .GetValues(typeof(TileBrushShape))
                .Cast<TileBrushShape>()
                .Select(shape =>
                    new FloatMenuOption(
                        ShapeLabel(shape),
                        () => SelectShape(shape)))
                .ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void SelectShape(TileBrushShape shape)
        {
            Controller.CancelStroke();
            Settings.Shape = shape;
            session.Activate();
        }

        private static string ShapeLabel(TileBrushShape shape)
        {
            switch (shape)
            {
                case TileBrushShape.Rugged:
                    return "WB_TileBrushBrushShapeRugged".Translate();
                case TileBrushShape.Rectangle:
                    return "WB_TileBrushBrushShapeRectangle".Translate();
                default:
                    return "WB_TileBrushBrushShapeCircle".Translate();
            }
        }

        private void SelectTool(TileBrushToolKind kind)
        {
            Controller.CancelStroke();
            Settings.SelectTool(kind);
            optionSearch = string.Empty;
            optionScrollPosition = Vector2.zero;
            session.Activate();
        }

        private void DrawOptionsPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            var inner = rect.ContractedBy(8f);
            var options = GetVisibleOptions(Settings.Tool.Options);
            var selected = Settings.SelectedDef;
            var vegetationMix =
                Settings.ToolKind == TileBrushToolKind.VegetationMix;
            var top = inner.y;

            if (options.Count > 0)
            {
                if (vegetationMix)
                {
                    const float clearButtonWidth = 90f;
                    Widgets.Label(
                        new Rect(
                            inner.x,
                            top,
                            inner.width - clearButtonWidth - StandardGap,
                            24f),
                        "WB_TileBrushSelectedVegetation".Translate(
                            Settings.VegetationMixSelections.Count));
                    if (Widgets.ButtonText(
                            new Rect(
                                inner.xMax - clearButtonWidth,
                                top,
                                clearButtonWidth,
                                24f),
                            "WB_TileBrushClearSelection".Translate(),
                            active:
                                Settings.VegetationMixSelections.Count > 0))
                    {
                        Settings.ClearVegetationMix();
                        session.Activate();
                    }
                }
                else
                {
                    var selectionLabel = selected == null
                        ? "WB_TileBrushNoSelection".Translate()
                        : "WB_TileBrushSelectedDef".Translate(
                            selected.LabelCap);
                    Widgets.Label(
                        new Rect(inner.x, top, inner.width, 24f),
                        selectionLabel);
                }

                top += 24f;

                if (selected is TerrainDef terrain)
                {
                    var fertility = terrain.fertility < 0f
                        ? "WB_TileBrushNotApplicable".Translate().ToString()
                        : terrain.fertility.ToStringPercent();
                    Widgets.Label(
                        new Rect(inner.x, top, inner.width, 24f),
                        "WB_TileBrushFertility".Translate(fertility));
                    top += 24f;
                }
            }

            DrawToolSliders(inner, ref top);

            if (options.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(
                    new Rect(
                        inner.x,
                        top,
                        inner.width,
                        Mathf.Max(0f, inner.yMax - top)),
                    "WB_TileBrushDirectToolHint".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            var searchLabelWidth = Mathf.Min(64f, inner.width * 0.25f);
            Widgets.Label(
                new Rect(
                    inner.x,
                    top,
                    searchLabelWidth,
                    24f),
                "WB_TileBrushSearch".Translate());
            var newSearch = Widgets.TextField(
                new Rect(
                    inner.x + searchLabelWidth,
                    top,
                    inner.width - searchLabelWidth,
                    24f),
                optionSearch ?? string.Empty);
            if (newSearch != optionSearch)
            {
                optionSearch = newSearch;
                optionScrollPosition = Vector2.zero;
            }

            top += 28f;
            var filtered = GetFilteredOptions(options, optionSearch);
            var outRect = new Rect(
                inner.x,
                top,
                inner.width,
                Mathf.Max(0f, inner.yMax - top));
            if (outRect.height <= 0f)
            {
                return;
            }

            var grid = ResponsiveOptionGrid.Create(
                outRect,
                filtered.Count,
                OptionRowHeight);
            grid.ClampScrollPosition(ref optionScrollPosition);
            var viewRect = grid.ViewRect;

            Widgets.BeginScrollView(
                outRect,
                ref optionScrollPosition,
                viewRect);
            var visibleTop = optionScrollPosition.y;
            var visibleBottom = visibleTop + outRect.height;
            for (var i = 0; i < filtered.Count; i++)
            {
                var def = filtered[i];
                var row = grid.RowRect(i);
                if (row.yMax < visibleTop || row.y > visibleBottom)
                {
                    continue;
                }

                var favorite = IsFavorite(Settings.Tool.Kind, def);
                var rowSelected = vegetationMix
                    ? Settings.IsVegetationMixSelected(def)
                    : def == selected;
                if (rowSelected)
                {
                    Widgets.DrawHighlightSelected(row);
                }
                else if (favorite)
                {
                    Widgets.DrawLightHighlight(row);
                }
                else
                {
                    if (grid.IsAlternatingRow(i))
                    {
                        Widgets.DrawLightHighlight(row);
                    }

                    if (Mouse.IsOver(row))
                    {
                        Widgets.DrawHighlight(row);
                    }
                }

                var contentLeft = row.x + 2f;
                Rect checkboxRect = default;
                if (vegetationMix)
                {
                    checkboxRect = new Rect(
                        contentLeft,
                        row.y + (row.height - 24f) / 2f,
                        24f,
                        24f);
                    contentLeft = checkboxRect.xMax + 2f;
                }

                var thumbnailRect = new Rect(
                    contentLeft,
                    row.y + (row.height - OptionThumbnailSize) / 2f,
                    OptionThumbnailSize,
                    OptionThumbnailSize);
                var starRect = new Rect(
                    row.xMax - FavoriteStarSize - 2f,
                    row.y + (row.height - FavoriteStarSize) / 2f,
                    FavoriteStarSize,
                    FavoriteStarSize);
                var selectionLeft = vegetationMix
                    ? checkboxRect.xMax + 2f
                    : row.x;
                var selectionRect = new Rect(
                    selectionLeft,
                    row.y,
                    Mathf.Max(
                        0f,
                        starRect.x - selectionLeft - 2f),
                    row.height);
                var labelRect = new Rect(
                    thumbnailRect.xMax + 4f,
                    row.y + 2f,
                    Mathf.Max(
                        0f,
                        starRect.x - thumbnailRect.xMax - 8f),
                    row.height - 4f);

                var selectionChanged = Widgets.ButtonInvisible(selectionRect);
                if (vegetationMix)
                {
                    var checkedValue = rowSelected;
                    Widgets.Checkbox(
                        checkboxRect.x,
                        checkboxRect.y,
                        ref checkedValue);
                    selectionChanged |= checkedValue != rowSelected;
                }

                if (selectionChanged)
                {
                    if (vegetationMix)
                    {
                        Settings.ToggleVegetationMixDef(def);
                    }
                    else
                    {
                        Settings.SetSelectedDef(def);
                    }

                    session.Activate();
                }

                if (CanDrawDefThumbnail(def))
                {
                    Widgets.DefIcon(thumbnailRect, def);
                }

                Widgets.Label(labelRect, BuildDefListLabel(def));

                if (Widgets.ButtonImage(
                        starRect,
                        favorite
                            ? Page_SelectWorld.StarIcon
                            : Page_SelectWorld.EmptyStarIcon))
                {
                    ToggleFavorite(Settings.Tool.Kind, def);
                }

                TooltipHandler.TipRegion(
                    starRect,
                    favorite
                        ? "WB_TileBrushRemoveFavorite".Translate()
                        : "WB_TileBrushAddFavorite".Translate());
            }

            Widgets.EndScrollView();
        }

        private void DrawToolSliders(Rect rect, ref float top)
        {
            Widgets.Label(
                new Rect(rect.x, top, rect.width, 22f),
                "WB_TileBrushBrushRadius".Translate(Settings.Radius));
            top += 20f;
            Settings.Radius = Mathf.RoundToInt(
                Widgets.HorizontalSlider(
                    new Rect(rect.x, top, rect.width, 22f),
                    Settings.Radius,
                    0f,
                    25f,
                    roundTo: 1f));
            top += 25f;

            if (Settings.Tool.SupportsDensity)
            {
                var density = Settings.DensityForCurrentTool();
                DrawPercentSlider(
                    rect,
                    ref top,
                    "WB_TileBrushDensity".Translate(density.ToStringPercent()),
                    ref density);
                switch (Settings.ToolKind)
                {
                    case TileBrushToolKind.Terrain:
                        Settings.TerrainDensity = density;
                        break;
                    case TileBrushToolKind.Plants:
                    case TileBrushToolKind.Trees:
                    case TileBrushToolKind.VegetationMix:
                        Settings.VegetationDensity = density;
                        break;
                    case TileBrushToolKind.Ores:
                        Settings.OreDensity = density;
                        break;
                    case TileBrushToolKind.Chunks:
                        Settings.ChunkDensity = density;
                        break;
                }
            }

            if (Settings.ToolKind == TileBrushToolKind.Plants ||
                Settings.ToolKind == TileBrushToolKind.Trees ||
                Settings.ToolKind == TileBrushToolKind.VegetationMix)
            {
                DrawPercentSlider(
                    rect,
                    ref top,
                    "WB_TileBrushGrowth".Translate(
                        Settings.PlantGrowth.ToStringPercent()),
                    ref Settings.PlantGrowth);
            }

            if (Settings.ToolKind == TileBrushToolKind.Snow)
            {
                DrawPercentSlider(
                    rect,
                    ref top,
                    "WB_TileBrushDepth".Translate(
                        Settings.SnowDepth.ToStringPercent()),
                    ref Settings.SnowDepth);
            }
        }

        private static void DrawPercentSlider(
            Rect rect,
            ref float top,
            TaggedString label,
            ref float value)
        {
            Widgets.Label(
                new Rect(rect.x, top, rect.width, 22f),
                label);
            top += 20f;
            value = Widgets.HorizontalSlider(
                new Rect(rect.x, top, rect.width, 22f),
                value,
                0f,
                1f,
                roundTo: 0.01f);
            top += 25f;
        }

        private void DrawActionColumn(Rect rect)
        {
            var top = rect.y;
            if (DrawActionChoice(
                    new Rect(
                        rect.x,
                        top,
                        rect.width,
                        ActionRowHeight),
                    "WB_TileBrushEyedropper".Translate(),
                    Settings.EyedropperActive,
                    enabled: true))
            {
                Settings.EyedropperActive = true;
                session.Activate();
            }

            top += ActionRowHeight + StandardGap;
            if (DrawActionChoice(
                    new Rect(
                        rect.x,
                        top,
                        rect.width,
                        ActionRowHeight),
                    "WB_TileBrushPaint".Translate(),
                    !Settings.EyedropperActive &&
                    Settings.Operation == TileBrushOperation.Paint,
                    Settings.CanPaint))
            {
                Settings.Operation = TileBrushOperation.Paint;
                Settings.EyedropperActive = false;
                session.Activate();
            }

            top += ActionRowHeight + StandardGap;
            var clearSupported = Settings.Tool.SupportsClear;
            if (DrawActionChoice(
                    new Rect(
                        rect.x,
                        top,
                        rect.width,
                        ActionRowHeight),
                    "WB_TileBrushEraser".Translate(),
                    !Settings.EyedropperActive &&
                    Settings.Operation == TileBrushOperation.Clear,
                    clearSupported))
            {
                Settings.Operation = TileBrushOperation.Clear;
                Settings.EyedropperActive = false;
                session.Activate();
            }

            top += ActionRowHeight + StandardGap;
            DrawReplacementToggle(new Rect(
                rect.x,
                top,
                rect.width,
                ActionRowHeight));
            top += ActionRowHeight + StandardGap;

            DrawHistory(new Rect(
                rect.x,
                Mathf.Max(top, rect.yMax - 30f),
                rect.width,
                30f));
        }

        private static bool DrawActionChoice(
            Rect rect,
            TaggedString label,
            bool selected,
            bool enabled)
        {
            var radioClicked = Widgets.RadioButton(
                rect.x,
                rect.y + (rect.height - 24f) / 2f,
                selected,
                disabled: !enabled);
            var labelRect = new Rect(
                rect.x + 32f,
                rect.y,
                Mathf.Max(0f, rect.width - 32f),
                rect.height);
            var oldColor = GUI.color;
            if (!enabled)
            {
                GUI.color = Color.gray;
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, label);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = oldColor;

            return enabled &&
                   (radioClicked || Widgets.ButtonInvisible(labelRect));
        }

        private void DrawReplacementToggle(Rect rect)
        {
            var requested = Settings.FullReplacement;
            var oldColor = GUI.color;
            if (Settings.FullReplacement)
            {
                GUI.color = new Color(1f, 0.45f, 0.45f);
            }

            Widgets.Checkbox(
                rect.x,
                rect.y + (rect.height - 24f) / 2f,
                ref requested);
            var labelRect = new Rect(
                rect.x + 32f,
                rect.y,
                Mathf.Max(0f, rect.width - 32f),
                rect.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(
                labelRect,
                "WB_TileBrushReplaceMode".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            if (Widgets.ButtonInvisible(labelRect))
            {
                requested = !requested;
            }

            GUI.color = oldColor;
            if (requested == Settings.FullReplacement)
            {
                return;
            }

            Settings.FullReplacement = requested;
            session.Activate();
        }

        private void DrawHistory(Rect rect)
        {
            var buttonWidth = (rect.width - StandardGap) / 2f;
            var buttonY = rect.yMax - 30f;
            if (Widgets.ButtonText(
                    new Rect(
                        rect.x,
                        buttonY,
                        buttonWidth,
                        30f),
                    "WB_TileBrushUndo".Translate(),
                    active: Controller.CanUndo))
            {
                Controller.Undo();
            }

            if (Widgets.ButtonText(
                    new Rect(
                        rect.x + buttonWidth + StandardGap,
                        buttonY,
                        buttonWidth,
                        30f),
                    "WB_TileBrushRedo".Translate(),
                    active: Controller.CanRedo))
            {
                Controller.Redo();
            }
        }

        private List<Def> FilterOptions(
            IReadOnlyList<Def> options,
            string search,
            bool showContentSource,
            TileBrushToolKind toolKind)
        {
            var filtered = search.NullOrEmpty()
                ? options.AsEnumerable()
                : options.Where(def =>
                    def.LabelCap.ToString().IndexOf(
                        search,
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    def.defName.IndexOf(
                        search,
                        StringComparison.OrdinalIgnoreCase) >= 0 ||
                    showContentSource &&
                    (def.modContentPack?.Name?.IndexOf(
                         search,
                         StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);

            IOrderedEnumerable<Def> ordered;
            if (showContentSource)
            {
                ordered = filtered
                    .OrderByDescending(def => IsFavorite(toolKind, def))
                    .ThenBy(
                        def => def.modContentPack?.Name ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase)
                    .ThenBy(
                        def => def.LabelCap.ToString(),
                        StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                ordered = filtered
                    .OrderByDescending(def => IsFavorite(toolKind, def))
                    .ThenBy(
                        def => def.LabelCap.ToString(),
                        StringComparer.OrdinalIgnoreCase);
            }

            return ordered
                .ThenBy(
                    def => def.defName,
                    StringComparer.Ordinal)
                .ToList();
        }

        private List<Def> GetFilteredOptions(
            IReadOnlyList<Def> options,
            string search)
        {
            var showContentSource =
                WorldbuilderMod.settings?.showContentSourceOnScrollWindow == true;
            var toolKind = Settings.Tool.Kind;
            if (ReferenceEquals(cachedFilterOptions, options) &&
                cachedFilterSearch == search &&
                cachedFilterShowsContentSource == showContentSource &&
                cachedFilterToolKind == toolKind &&
                cachedFilterFavoriteRevision == favoriteRevision &&
                cachedFilteredOptions != null)
            {
                return cachedFilteredOptions;
            }

            cachedFilterOptions = options;
            cachedFilterSearch = search;
            cachedFilterShowsContentSource = showContentSource;
            cachedFilterToolKind = toolKind;
            cachedFilterFavoriteRevision = favoriteRevision;
            cachedFilteredOptions = FilterOptions(
                options,
                search,
                showContentSource,
                toolKind);
            return cachedFilteredOptions;
        }

        private IReadOnlyList<Def> GetVisibleOptions(
            IReadOnlyList<Def> options)
        {
            var hideNonRenderedTerrain =
                (Settings.ToolKind == TileBrushToolKind.Terrain ||
                 Settings.ToolKind == TileBrushToolKind.Water) &&
                IsOrdinarySurfaceMap();
            if (ReferenceEquals(cachedVisibleOptionsSource, options) &&
                cachedVisibleOptionsHideNonRenderedTerrain ==
                hideNonRenderedTerrain &&
                cachedVisibleOptions != null)
            {
                return cachedVisibleOptions;
            }

            cachedVisibleOptionsSource = options;
            cachedVisibleOptionsHideNonRenderedTerrain =
                hideNonRenderedTerrain;
            cachedVisibleOptions = hideNonRenderedTerrain
                ? options
                    .Where(def =>
                        !(def is TerrainDef terrain) ||
                        !terrain.dontRender)
                    .ToList()
                : options;
            return cachedVisibleOptions;
        }

        private bool IsOrdinarySurfaceMap()
        {
            return session.Map.generatorDef?.isUnderground != true &&
                   session.Map.Tile.LayerDef?.isSpace != true;
        }

        private static bool CanDrawDefThumbnail(Def def)
        {
            if (!Widgets.CanDrawIconFor(def))
            {
                return false;
            }

            if (!(def is BuildableDef buildable))
            {
                return true;
            }

            var iconName = buildable.uiIcon?.name;
            return buildable.uiIcon != null &&
                   buildable.uiIcon != BaseContent.BadTex &&
                   buildable.uiIcon != BaseContent.PlaceholderImage &&
                   !string.Equals(
                       iconName,
                       BaseContent.PlaceholderImagePath,
                       StringComparison.OrdinalIgnoreCase) &&
                   !string.Equals(
                       iconName,
                       BaseContent.PlaceholderGearImagePath,
                       StringComparison.OrdinalIgnoreCase);
        }

        private void RefreshFavoriteKeys()
        {
            var refreshedKeys = new HashSet<string>(
                WorldbuilderMod.settings?.tileBrushFavoriteKeys ??
                Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            if (favoriteKeys.SetEquals(refreshedKeys))
            {
                return;
            }

            favoriteKeys.Clear();
            favoriteKeys.UnionWith(refreshedKeys);
            favoriteRevision++;
            cachedFilteredOptions = null;
        }

        private bool IsFavorite(TileBrushToolKind toolKind, Def def)
        {
            var key = BuildFavoriteKey(toolKind, def);
            return key != null && favoriteKeys.Contains(key);
        }

        private void ToggleFavorite(TileBrushToolKind toolKind, Def def)
        {
            var key = BuildFavoriteKey(toolKind, def);
            var settings = WorldbuilderMod.settings;
            if (key == null || settings == null)
            {
                return;
            }

            settings.tileBrushFavoriteKeys ??= new List<string>();
            if (favoriteKeys.Remove(key))
            {
                settings.tileBrushFavoriteKeys.RemoveAll(existingKey =>
                    string.Equals(
                        existingKey,
                        key,
                        StringComparison.Ordinal));
            }
            else
            {
                favoriteKeys.Add(key);
                if (!settings.tileBrushFavoriteKeys.Any(existingKey =>
                        string.Equals(
                            existingKey,
                            key,
                            StringComparison.Ordinal)))
                {
                    settings.tileBrushFavoriteKeys.Add(key);
                }
            }

            favoriteRevision++;
            cachedFilteredOptions = null;
            settings.Write();
        }

        private static string BuildFavoriteKey(
            TileBrushToolKind toolKind,
            Def def)
        {
            if (def == null || def.defName.NullOrEmpty())
            {
                return null;
            }

            var type = def.GetType();
            return $"{toolKind}|{type.FullName ?? type.Name}|{def.defName}";
        }

        private static string BuildDefListLabel(Def def)
        {
            if (WorldbuilderMod.settings?.showContentSourceOnScrollWindow != true)
            {
                return def.LabelCap.ToString();
            }

            var source = def.modContentPack?.Name;
            return source.NullOrEmpty()
                ? def.LabelCap.ToString()
                : $"{def.LabelCap} - {source}";
        }

        private static Rect ClampWindowRect(Rect rect)
        {
            var minimumWidth = Mathf.Min(MinimumWidth, UI.screenWidth);
            var minimumHeight = Mathf.Min(MinimumHeight, UI.screenHeight);
            rect.width = Mathf.Clamp(
                rect.width,
                minimumWidth,
                UI.screenWidth);
            rect.height = Mathf.Clamp(
                rect.height,
                minimumHeight,
                UI.screenHeight);
            rect.x = Mathf.Clamp(
                rect.x,
                0f,
                Mathf.Max(0f, UI.screenWidth - rect.width));
            rect.y = Mathf.Clamp(
                rect.y,
                0f,
                Mathf.Max(0f, UI.screenHeight - rect.height));
            return rect;
        }
    }

}
