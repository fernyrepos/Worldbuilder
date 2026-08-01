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

        private readonly TileBrushSession session;
        private Vector2 optionScrollPosition;
        private string optionSearch = string.Empty;
        private IReadOnlyList<Def> cachedFilterOptions;
        private string cachedFilterSearch;
        private bool cachedFilterShowsContentSource;
        private List<Def> cachedFilteredOptions;
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
            var options = Settings.Tool.Options;
            var selected = Settings.SelectedDef;
            var top = inner.y;

            if (options.Count > 0)
            {
                var selectionLabel = selected == null
                    ? "WB_TileBrushNoSelection".Translate()
                    : "WB_TileBrushSelectedDef".Translate(
                        selected.LabelCap);
                Widgets.Label(
                    new Rect(inner.x, top, inner.width, 24f),
                    selectionLabel);
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

            var viewWidth = Mathf.Max(0f, outRect.width - 18f);
            var viewRect = new Rect(
                0f,
                0f,
                viewWidth,
                Mathf.Max(outRect.height, filtered.Count * 28f));

            Widgets.BeginScrollView(
                outRect,
                ref optionScrollPosition,
                viewRect);
            for (var i = 0; i < filtered.Count; i++)
            {
                var def = filtered[i];
                var row = new Rect(0f, i * 28f, viewWidth, 27f);
                if (def == selected)
                {
                    Widgets.DrawHighlightSelected(row);
                }
                else if (Mouse.IsOver(row))
                {
                    Widgets.DrawHighlight(row);
                }

                if (Widgets.ButtonInvisible(row))
                {
                    Settings.SetSelectedDef(def);
                    session.Activate();
                }

                Widgets.Label(
                    row.ContractedBy(4f, 2f),
                    BuildDefListLabel(def));
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
                Settings.ToolKind == TileBrushToolKind.Trees)
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
                    enabled: true))
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

        private static List<Def> FilterOptions(
            IReadOnlyList<Def> options,
            string search,
            bool showContentSource)
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

            var ordered = showContentSource
                ? filtered
                    .OrderBy(
                        def => def.modContentPack?.Name ?? string.Empty,
                        StringComparer.OrdinalIgnoreCase)
                    .ThenBy(
                        def => def.LabelCap.ToString(),
                        StringComparer.OrdinalIgnoreCase)
                : filtered.OrderBy(
                    def => def.LabelCap.ToString(),
                    StringComparer.OrdinalIgnoreCase);

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
            if (ReferenceEquals(cachedFilterOptions, options) &&
                cachedFilterSearch == search &&
                cachedFilterShowsContentSource == showContentSource &&
                cachedFilteredOptions != null)
            {
                return cachedFilteredOptions;
            }

            cachedFilterOptions = options;
            cachedFilterSearch = search;
            cachedFilterShowsContentSource = showContentSource;
            cachedFilteredOptions = FilterOptions(
                options,
                search,
                showContentSource);
            return cachedFilteredOptions;
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
