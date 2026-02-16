using Verse;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;

namespace Worldbuilder
{
    public enum MapEditingMode
    {
        Paint,
        Copy
    }

    [HotSwappable]
    public class Window_MapEditor : Window
    {
        public MapEditingMode currentMode;
        public BiomeDef selectedBiome;
        public Hilliness selectedHilliness = Hilliness.Flat;
        public List<LandmarkDef> selectedLandmarks = new List<LandmarkDef>();
        public List<TileMutatorDef> selectedFeatures = new List<TileMutatorDef>();
        public PlanetTile copiedTileID = PlanetTile.Invalid;
        public PlanetTile selectedTileID = PlanetTile.Invalid;
        public LandmarkDef selectedLandmarkEntry;
        public TileMutatorDef selectedFeatureEntry;
        public Vector2 biomeScrollPosition = Vector2.zero;
        public Vector2 landmarksScrollPosition = Vector2.zero;
        public Vector2 featuresScrollPosition = Vector2.zero;
        public bool dragging = false;
        public int brushSize = 0;
        private const int MaxBrushSize = 500;
        private List<PlanetTile> tmpTiles = new List<PlanetTile>();
        private HashSet<PlanetTile> tilesInCurrentPaintOperation = new HashSet<PlanetTile>();
        public bool paintLandmarks = false;
        public bool paintFeatures = false;
        public bool paintBiomes = false;
        public bool paintTerrain = false;
        private bool editingRoads = false;
        private bool editingRivers = false;
        private RoadDef selectedRoadDef = null;
        private RiverDef selectedRiverDef = null;
        private PlanetTile pathStartTile = PlanetTile.Invalid;
        private List<PlanetTile> previewPath = new List<PlanetTile>();
        private List<PlanetTile> currentPath = new List<PlanetTile>();
        private List<(SurfaceTile from, SurfaceTile to, SurfaceTile.RiverLink link)> temporaryRiverLinks = new List<(SurfaceTile, SurfaceTile, SurfaceTile.RiverLink)>();
        private List<(SurfaceTile from, SurfaceTile to, SurfaceTile.RoadLink link)> temporaryRoadLinks = new List<(SurfaceTile, SurfaceTile, SurfaceTile.RoadLink)>();
        private PlanetTile lastPreviewTile = PlanetTile.Invalid;
        private bool rightClickDragging = false;
        public override void SetInitialSizeAndPosition()
        {
            windowRect = new Rect(0f, 0f, InitialSize.x, InitialSize.y).Rounded();
        }

        public override Vector2 InitialSize => new Vector2(400f, 625f);
        public Window_MapEditor()
        {
            forcePause = true;
            doCloseX = false;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
            draggable = true;
            currentMode = MapEditingMode.Copy;
            if (Find.WorldSelector.selectedTile.Valid)
            {
                CopyTileProperties(Find.WorldSelector.selectedTile);
            }
        }

        public static HashSet<PlanetTile> tilesToDraw = new HashSet<PlanetTile>();
        public override void OnCancelKeyPressed()
        {
            if (editingRivers || editingRoads)
            {
                var modeType = editingRivers ? "WB_River".Translate() : "WB_Road".Translate();
                CancelEditing();
                Event.current.Use();
                Messages.Message("WB_EditModeLeft".Translate(modeType), MessageTypeDefOf.NeutralEvent);
                return;
            }

            base.OnCancelKeyPressed();
        }

        public override void ExtraOnGUI()
        {
            base.ExtraOnGUI();

            if (editingRivers || editingRoads)
            {
                HandlePathEditing();
                return;
            }

            EventType eventType = Event.current.type;

            if (eventType == EventType.MouseDown && Event.current.button == 0)
            {
                if (!dragging)
                {
                    dragging = true;
                    tilesInCurrentPaintOperation.Clear();

                    Find.WorldSelector.dragBox.active = false;
                }
            }
            else if (eventType == EventType.MouseUp && Event.current.button == 0)
            {
                if (dragging && update)
                {
                }
                dragging = false;
            }

            if (dragging)
            {
                Find.WorldSelector.dragBox.active = false;

                var tile = GenWorld.TileAt(UI.MousePositionOnUI);

                if (tile.Valid && selectedTileID != tile)
                {
                    selectedTileID = tile;
                    if (currentMode == MapEditingMode.Paint)
                    {
                        PaintTile(tile);
                        Find.World.renderer.GetLayer<WorldDrawLayer_SelectedTiles>(tile.Layer)?.RegenerateNow();
                        update = true;
                    }
                    else if (currentMode == MapEditingMode.Copy)
                    {
                        CopyTileProperties(tile);
                    }
                }

                if (Event.current.isMouse)
                {
                    Event.current.Use();
                }
            }
            else if (update)
            {
                update = false;
                tilesToDraw.Clear();
                Find.World.renderer.GetLayer<WorldDrawLayer_Terrain>(Find.WorldGrid.Surface).RegenerateNow();
                Find.World.renderer.GetLayer<WorldDrawLayer_Landmarks>(Find.WorldGrid.Surface).RegenerateNow();
                Find.World.renderer.GetLayer<WorldDrawLayer_Hills>(Find.WorldGrid.Surface).RegenerateNow();
                Find.World.renderer.GetLayer<WorldDrawLayer_SelectedTiles>(Find.WorldGrid.Surface).RegenerateNow();
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (WorldRendererUtility.WorldRendered is false)
            {
                Close();
                return;
            }
            var panelRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - 45f);
            float curY = panelRect.y;
            Text.Font = GameFont.Small;

            float iconSize = 24f;
            float buttonHeight = 30f;
            float buttonSpacing = 5f;

            float labelWidth = 100f;
            float dropdownWidth = panelRect.width - 20f - labelWidth - 10f;

            DrawModeButton(ref curY, panelRect, buttonHeight, buttonSpacing, iconSize, "Worldbuilder/UI/MapEditor/paint", "WB_MapEditorPaintTile".Translate(), MapEditingMode.Paint);
            DrawModeButton(ref curY, panelRect, buttonHeight, buttonSpacing, iconSize, "Worldbuilder/UI/MapEditor/copy", "WB_MapEditorCopyTile".Translate(), MapEditingMode.Copy);

            var brushRect = new Rect(panelRect.x, curY, panelRect.width - 20f, 40f);
            brushSize = (int)Widgets.HorizontalSlider(brushRect, brushSize, 0f, MaxBrushSize, false, "WB_MapEditorBrushSize".Translate() + ": " + brushSize.ToString(), "0", MaxBrushSize.ToString(), 1f);
            curY += 45f;

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(panelRect.x, curY - 5, panelRect.width - 20f, Text.LineHeight), "WB_MapEditorBrushProperties".Translate());
            curY += Text.LineHeight;
            Text.Font = GameFont.Small;

            var paintBiomesRect = new Rect(panelRect.x, curY, panelRect.width - 20f, 24f);
            Widgets.Checkbox(paintBiomesRect.x, paintBiomesRect.y + 3f, ref paintBiomes);
            var paintBiomesLabelRect = new Rect(paintBiomesRect.x + 28f, paintBiomesRect.y + 5f, 100f, paintBiomesRect.height);
            Widgets.Label(paintBiomesLabelRect, "WB_MapEditorPaintBiomes".Translate());
            if (Widgets.ButtonInvisible(paintBiomesLabelRect))
            {
                paintBiomes = !paintBiomes;
            }

            var biomeDropdownRect = new Rect(paintBiomesLabelRect.xMax + 10f, curY, panelRect.width - paintBiomesLabelRect.xMax - 30f, 24f);
            if (Widgets.ButtonText(biomeDropdownRect, selectedBiome?.LabelCap ?? "WB_Select".Translate()))
            {
                var options = new List<FloatMenuOption>();
                foreach (var biome in DefDatabase<BiomeDef>.AllDefs.Where(x => x.generatesNaturally).OrderBy(b => b.label))
                {
                    options.Add(new FloatMenuOption(biome.LabelCap, () => selectedBiome = biome));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            curY += 30f;

            var paintTerrainRect = new Rect(panelRect.x, curY, panelRect.width - 20f, 24f);
            Widgets.Checkbox(paintTerrainRect.x, paintTerrainRect.y + 3f, ref paintTerrain);
            var paintTerrainLabelRect = new Rect(paintTerrainRect.x + 28f, paintTerrainRect.y + 5f, 100f, paintTerrainRect.height);
            Widgets.Label(paintTerrainLabelRect, "WB_MapEditorPaintTerrain".Translate());
            if (Widgets.ButtonInvisible(paintTerrainLabelRect))
            {
                paintTerrain = !paintTerrain;
            }

            var hillinessDropdownRect = new Rect(paintTerrainLabelRect.xMax + 10f, curY, panelRect.width - paintTerrainLabelRect.xMax - 30f, 24f);
            if (Widgets.ButtonText(hillinessDropdownRect, selectedHilliness.GetLabelCap()))
            {
                var options = new List<FloatMenuOption>();
                foreach (Hilliness hilliness in System.Enum.GetValues(typeof(Hilliness)))
                {
                    if (hilliness != Hilliness.Undefined)
                    {
                        options.Add(new FloatMenuOption(hilliness.GetLabelCap(), () => selectedHilliness = hilliness));
                    }
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }

            curY += 30f;

            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(panelRect.x, curY, panelRect.width - 20f, Text.LineHeight * 2), "WB_MapEditorTerrainPropertiesNote".Translate());
            curY += Text.LineHeight * 2;
            Text.Font = GameFont.Small;
            if (ModsConfig.OdysseyActive)
            {
                var paintLandmarksRect = new Rect(panelRect.x, curY, panelRect.width - 20f, 24f);
                Widgets.Checkbox(paintLandmarksRect.x, paintLandmarksRect.y + 3f, ref paintLandmarks);
                var paintLandmarksLabelRect = new Rect(paintLandmarksRect.x + 28f, paintLandmarksRect.y + 5f, paintLandmarksRect.width - 28f, paintLandmarksRect.height);
                Widgets.Label(paintLandmarksLabelRect, "WB_MapEditorPaintLandmarks".Translate());
                if (Widgets.ButtonInvisible(paintLandmarksLabelRect))
                {
                    paintLandmarks = !paintLandmarks;
                }
                curY += 30f;
                DrawDefListSection(ref curY, panelRect, selectedLandmarks, ref selectedLandmarkEntry, ref landmarksScrollPosition,
                    (LandmarkDef l) => l.LabelCap,
                    () => DefDatabase<LandmarkDef>.AllDefs.OrderBy(l => l.label),
                    (LandmarkDef l) => selectedLandmarks.Add(l),
                    (LandmarkDef l) => selectedLandmarks.Remove(l));
                curY += 10f;
            }
            var paintFeaturesRect = new Rect(panelRect.x, curY, panelRect.width - 20f, 24f);
            Widgets.Checkbox(paintFeaturesRect.x, paintFeaturesRect.y + 3f, ref paintFeatures);
            var paintFeaturesLabelRect = new Rect(paintFeaturesRect.x + 28f, paintFeaturesRect.y + 5f, paintFeaturesRect.width - 28f, paintFeaturesRect.height);
            Widgets.Label(paintFeaturesLabelRect, "WB_MapEditorPaintFeatures".Translate());
            if (Widgets.ButtonInvisible(paintFeaturesLabelRect))
            {
                paintFeatures = !paintFeatures;
            }
            curY += 30f;
            DrawDefListSection(ref curY, panelRect, selectedFeatures, ref selectedFeatureEntry, ref featuresScrollPosition,
                (TileMutatorDef f) => f.LabelCap,
                () => DefDatabase<TileMutatorDef>.AllDefs.OrderBy(f => f.label),
                (TileMutatorDef f) => selectedFeatures.Add(f),
                (TileMutatorDef f) => selectedFeatures.Remove(f));

            float doneButtonWidth = 120f;
            float doneButtonHeight = 35f;
            var doneButtonRect = new Rect(inRect.width - doneButtonWidth, inRect.y, doneButtonWidth, doneButtonHeight);
            if (Widgets.ButtonText(doneButtonRect, "DoneButton".Translate()))
            {
                Close();
            }

            float bottomY = inRect.yMax - 40f;
            float buttonWidth = (inRect.width - 20f) / 3f;
            float bottomButtonHeight = 35f;
            float spacing = 10f;

            var riversButtonRect = new Rect(inRect.x, bottomY, buttonWidth, bottomButtonHeight);
            if (Widgets.ButtonText(riversButtonRect, "WB_EditRivers".Translate()))
            {
                ShowRiverDefMenu();
            }

            var mapTextButtonRect = new Rect(riversButtonRect.xMax + spacing, bottomY, buttonWidth, bottomButtonHeight);
            if (Widgets.ButtonText(mapTextButtonRect, "WB_EditMapText".Translate()))
            {
                Find.WindowStack.Add(new Window_MapTextEditor());
            }

            var roadsButtonRect = new Rect(mapTextButtonRect.xMax + spacing, bottomY, buttonWidth - spacing, bottomButtonHeight);
            if (Widgets.ButtonText(roadsButtonRect, "WB_EditRoads".Translate()))
            {
                ShowRoadDefMenu();
            }
        }

        private List<PlanetTile> GetTilesInRadius(PlanetTile centerTile, int radius)
        {
            if (radius < 0) return new List<PlanetTile> { centerTile };

            var result = new List<PlanetTile>();
            centerTile.Layer.Filler.FloodFill(centerTile, (PlanetTile x) => true, delegate (PlanetTile tile, int dist)
            {
                if (dist <= radius && !tilesInCurrentPaintOperation.Contains(tile))
                {
                    result.Add(tile);
                }
            }, radius);

            return result;
        }

        public void PaintTile(PlanetTile tile)
        {
            tmpTiles.Clear();
            if (brushSize == 0)
            {
                tmpTiles.Add(tile);
            }
            else
            {
                tmpTiles = GetTilesInRadius(tile, brushSize);
            }

            foreach (var t in tmpTiles)
            {
                tilesInCurrentPaintOperation.Add(t);
                Tile tileData = Find.WorldGrid[t];
                if (paintBiomes && selectedBiome != null)
                {
                    tileData.biome = selectedBiome;
                }
                if (paintTerrain && selectedHilliness != Hilliness.Undefined)
                {
                    tileData.hilliness = selectedHilliness;
                }
                if (ModsConfig.OdysseyActive && paintLandmarks)
                {
                    if (Find.World.landmarks[t] != null)
                    {
                        Find.World.landmarks.RemoveLandmark(tileData.tile);
                    }
                    foreach (LandmarkDef landmarkDef in selectedLandmarks)
                    {
                        if (landmarkDef.IsValidTile(tileData.tile, tileData.Layer))
                        {
                            Find.World.landmarks.AddLandmark(landmarkDef, tileData.tile, tileData.Layer, true);
                        }
                        else
                        {
                            Messages.Message("WB_MapEditorLandmarkInvalidTile".Translate(landmarkDef.label), MessageTypeDefOf.RejectInput);
                        }
                    }
                }
                if (paintFeatures)
                {
                    if (tileData.Mutators.NullOrEmpty() is false)
                    {
                        foreach (var mutator in tileData.Mutators.ToList())
                        {
                            tileData.RemoveMutator(mutator);
                        }
                    }
                    foreach (TileMutatorDef tileMutatorDef in selectedFeatures)
                    {
                        tileData.AddMutator(tileMutatorDef);
                    }
                }
                tilesToDraw.Add(t);
            }
            update = true;
        }

        public static bool update;
        public void CopyTileProperties(PlanetTile tile)
        {
            Tile tileData = Find.WorldGrid[tile];
            selectedBiome = tileData.biome;
            selectedHilliness = tileData.hilliness;
            selectedLandmarks.Clear();
            if (ModsConfig.OdysseyActive)
            {
                Landmark landmark = Find.World.landmarks[tile];
                if (landmark != null)
                {
                    selectedLandmarks.Add(landmark.def);
                }
            }
            selectedFeatures.Clear();
            if (tileData.Mutators != null)
            {
                selectedFeatures.AddRange(tileData.Mutators);
            }
        }

        private void DrawModeButton(ref float curY, Rect panelRect, float buttonHeight, float buttonSpacing, float iconSize, string texturePath, string label, MapEditingMode mode)
        {
            var buttonRect = new Rect(panelRect.x, curY, panelRect.width - 200f, buttonHeight);
            var iconRect = new Rect(buttonRect.x, buttonRect.y + (buttonHeight - iconSize) / 2, iconSize, iconSize);
            var labelRect = new Rect(iconRect.xMax + 5f, buttonRect.y, buttonRect.width - iconSize - 5f, buttonHeight);

            Widgets.DrawHighlightIfMouseover(buttonRect);
            if (currentMode == mode) Widgets.DrawHighlightSelected(buttonRect);
            GUI.DrawTexture(iconRect, ContentFinder<Texture2D>.Get(texturePath));
            Widgets.Label(labelRect, label);
            if (Widgets.ButtonInvisible(buttonRect))
            {
                currentMode = mode;
            }
            curY += buttonHeight + buttonSpacing;
        }

        private void DrawDefListSection<T>(ref float curY, Rect panelRect, List<T> selectedDefs, ref T selectedEntry, ref Vector2 scrollPosition,
            System.Func<T, string> labelSelector, System.Func<IEnumerable<T>> allDefsSelector, System.Action<T> addAction, System.Action<T> removeAction) where T : Def
        {
            var listRect = new Rect(panelRect.x, curY, panelRect.width - 20f, 100f);
            Widgets.DrawMenuSection(listRect);

            var viewRect = new Rect(0, 0, listRect.width - 16f, selectedDefs.Count * 24f);
            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);
            float currentDefY = 0;
            for (int i = 0; i < selectedDefs.Count; i++)
            {
                var rowRect = new Rect(0, currentDefY, viewRect.width, 24f);
                Widgets.DrawHighlightIfMouseover(rowRect);
                if (EqualityComparer<T>.Default.Equals(selectedEntry, selectedDefs[i])) Widgets.DrawHighlightSelected(rowRect);
                Widgets.Label(new Rect(rowRect.x + 5f, rowRect.y, rowRect.width - 5f, rowRect.height), labelSelector(selectedDefs[i]));
                if (Widgets.ButtonInvisible(rowRect))
                {
                    selectedEntry = selectedDefs[i];
                }
                currentDefY += 24f;
            }
            Widgets.EndScrollView();
            curY += 100f + 5f;

            var buttonsRect = new Rect(panelRect.x, curY, panelRect.width - 20f, 24f);
            var removeButtonRect = new Rect(buttonsRect.xMax - 48f, buttonsRect.y, 24f, 24f);
            var addButtonRect = new Rect(buttonsRect.xMax - 24f, buttonsRect.y, 24f, 24f);

            if (selectedEntry != null && Widgets.ButtonImage(removeButtonRect, TexButton.Minus))
            {
                removeAction(selectedEntry);
                selectedEntry = default(T);
            }
            if (Widgets.ButtonImage(addButtonRect, TexButton.Plus))
            {
                var options = new List<FloatMenuOption>();
                foreach (var def in allDefsSelector())
                {
                    if (!selectedDefs.Contains(def))
                    {
                        options.Add(new FloatMenuOption(labelSelector(def), () => addAction(def)));
                    }
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
        }

        private void ShowRiverDefMenu()
        {
            var options = new List<FloatMenuOption>();

            foreach (var riverDef in DefDatabase<RiverDef>.AllDefs.OrderBy(r => r.label))
            {
                options.Add(new FloatMenuOption(riverDef.LabelCap, () => {
                    selectedRiverDef = riverDef;
                    editingRivers = true;
                    editingRoads = false;
                    selectedRoadDef = null;
                    pathStartTile = PlanetTile.Invalid;
                    previewPath.Clear();
                    Messages.Message("WB_RiverEditModeEntered".Translate(riverDef.LabelCap), MessageTypeDefOf.NeutralEvent);
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void ShowRoadDefMenu()
        {
            var options = new List<FloatMenuOption>();

            foreach (var roadDef in DefDatabase<RoadDef>.AllDefs.OrderBy(r => r.label))
            {
                options.Add(new FloatMenuOption(roadDef.LabelCap, () => {
                    selectedRoadDef = roadDef;
                    editingRoads = true;
                    editingRivers = false;
                    selectedRiverDef = null;
                    pathStartTile = PlanetTile.Invalid;
                    previewPath.Clear();
                    Messages.Message("WB_RoadEditModeEntered".Translate(roadDef.LabelCap), MessageTypeDefOf.NeutralEvent);
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private void HandlePathEditing()
        {
            EventType eventType = Event.current.type;

            var mouseTile = GenWorld.TileAt(UI.MousePositionOnUI);

            if (eventType == EventType.MouseDown && Event.current.button == 1)
            {
                if (pathStartTile.Valid)
                {
                    ClearTemporaryLinks();
                    pathStartTile = PlanetTile.Invalid;
                    previewPath.Clear();
                    lastPreviewTile = PlanetTile.Invalid;

                    if (mouseTile.Valid)
                    {
                        RegenerateActiveLayer(mouseTile);
                    }
                }

                if (mouseTile.Valid)
                {
                    rightClickDragging = true;
                    DeleteSegmentsAtTile(mouseTile);
                    lastPreviewTile = mouseTile;
                    Event.current.Use();
                }
                return;
            }

            if (eventType == EventType.MouseUp && Event.current.button == 1)
            {
                rightClickDragging = false;
                lastPreviewTile = PlanetTile.Invalid;
                Event.current.Use();
                return;
            }

            if (rightClickDragging && mouseTile.Valid && mouseTile != lastPreviewTile)
            {
                DeleteSegmentsAtTile(mouseTile);
                lastPreviewTile = mouseTile;
                Event.current.Use();
                return;
            }

            if (!rightClickDragging && pathStartTile.Valid && mouseTile.Valid && mouseTile != pathStartTile)
            {
                if (mouseTile != lastPreviewTile)
                {
                    ClearTemporaryLinks();
                    previewPath = FindPathBetweenTiles(pathStartTile, mouseTile);

                    if (previewPath.Count > 0)
                    {
                        AddTemporaryLinks(previewPath);
                        RegenerateActiveLayer(mouseTile);
                    }

                    lastPreviewTile = mouseTile;
                }
            }
            else if (!rightClickDragging && (!pathStartTile.Valid || !mouseTile.Valid || mouseTile == pathStartTile))
            {
                if (previewPath.Count > 0 || temporaryRiverLinks.Count > 0 || temporaryRoadLinks.Count > 0)
                {
                    ClearTemporaryLinks();
                    previewPath.Clear();
                    lastPreviewTile = PlanetTile.Invalid;

                    if (pathStartTile.Valid)
                    {
                        RegenerateActiveLayer(pathStartTile);
                    }
                }
            }

            if (eventType == EventType.MouseDown && Event.current.button == 0)
            {
                if (!mouseTile.Valid)
                {
                    Event.current.Use();
                    return;
                }

                if (!pathStartTile.Valid)
                {
                    pathStartTile = mouseTile;
                    previewPath.Clear();
                    lastPreviewTile = PlanetTile.Invalid;
                }
                else
                {
                    if (previewPath.Count > 0)
                    {
                        ClearTemporaryLinks();
                        CreatePathLinks(previewPath);

                        pathStartTile = mouseTile;
                        previewPath.Clear();
                        lastPreviewTile = PlanetTile.Invalid;

                        RegenerateActiveLayer(mouseTile);
                    }
                }

                Event.current.Use();
            }
        }

        private List<PlanetTile> FindPathBetweenTiles(PlanetTile start, PlanetTile end)
        {
            var path = new List<PlanetTile>();
            var surfaceLayer = (SurfaceLayer)start.Layer;

            var cameFrom = new Dictionary<PlanetTile, PlanetTile>();
            var frontier = new Queue<PlanetTile>();
            frontier.Enqueue(start);
            cameFrom[start] = PlanetTile.Invalid;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();

                if (current == end)
                {
                    var tile = end;
                    while (tile.Valid && tile != start)
                    {
                        path.Add(tile);
                        tile = cameFrom[tile];
                    }
                    path.Add(start);
                    path.Reverse();
                    return path;
                }

                var neighbors = new List<PlanetTile>();
                Find.WorldGrid.GetTileNeighbors(current, neighbors);

                foreach (var next in neighbors)
                {
                    if (!cameFrom.ContainsKey(next))
                    {
                        frontier.Enqueue(next);
                        cameFrom[next] = current;
                    }
                }
            }

            return path;
        }

        private void CreatePathLinks(List<PlanetTile> path)
        {
            var surfaceLayer = (SurfaceLayer)path[0].Layer;

            for (int i = 0; i < path.Count - 1; i++)
            {
                var fromTile = surfaceLayer[path[i]];
                var toTile = surfaceLayer[path[i + 1]];

                if (editingRivers)
                {
                    if (selectedRiverDef == null)
                    {
                        RemoveRiverLink(fromTile, toTile);
                    }
                    else
                    {
                        AddRiverLink(fromTile, toTile, selectedRiverDef);
                    }
                }
                else if (editingRoads)
                {
                    if (selectedRoadDef == null)
                    {
                        RemoveRoadLink(fromTile, toTile);
                    }
                    else
                    {
                        AddRoadLink(fromTile, toTile, selectedRoadDef);
                    }
                }
            }
        }

        private void AddRiverLink(SurfaceTile from, SurfaceTile to, RiverDef riverDef)
        {
            if (from.potentialRivers == null)
                from.potentialRivers = new List<SurfaceTile.RiverLink>();
            if (to.potentialRivers == null)
                to.potentialRivers = new List<SurfaceTile.RiverLink>();

            from.potentialRivers.RemoveAll(r => r.neighbor == to.tile);
            to.potentialRivers.RemoveAll(r => r.neighbor == from.tile);

            from.potentialRivers.Add(new SurfaceTile.RiverLink { neighbor = to.tile, river = riverDef });
            to.potentialRivers.Add(new SurfaceTile.RiverLink { neighbor = from.tile, river = riverDef });
        }

        private void RemoveRiverLink(SurfaceTile from, SurfaceTile to)
        {
            from.potentialRivers?.RemoveAll(r => r.neighbor == to.tile);
            if (from.potentialRivers != null && from.potentialRivers.Count == 0)
            {
                from.potentialRivers = null;
            }

            to.potentialRivers?.RemoveAll(r => r.neighbor == from.tile);
            if (to.potentialRivers != null && to.potentialRivers.Count == 0)
            {
                to.potentialRivers = null;
            }
        }

        private void AddRoadLink(SurfaceTile from, SurfaceTile to, RoadDef roadDef)
        {
            if (from.potentialRoads == null)
                from.potentialRoads = new List<SurfaceTile.RoadLink>();
            if (to.potentialRoads == null)
                to.potentialRoads = new List<SurfaceTile.RoadLink>();

            from.potentialRoads.RemoveAll(r => r.neighbor == to.tile);
            to.potentialRoads.RemoveAll(r => r.neighbor == from.tile);

            from.potentialRoads.Add(new SurfaceTile.RoadLink { neighbor = to.tile, road = roadDef });
            to.potentialRoads.Add(new SurfaceTile.RoadLink { neighbor = from.tile, road = roadDef });
        }

        private void RemoveRoadLink(SurfaceTile from, SurfaceTile to)
        {
            from.potentialRoads?.RemoveAll(r => r.neighbor == to.tile);
            if (from.potentialRoads != null && from.potentialRoads.Count == 0)
            {
                from.potentialRoads = null;
            }

            to.potentialRoads?.RemoveAll(r => r.neighbor == from.tile);
            if (to.potentialRoads != null && to.potentialRoads.Count == 0)
            {
                to.potentialRoads = null;
            }
        }

        private void AddTemporaryLinks(List<PlanetTile> path)
        {
            if (path.Count < 2) return;

            var surfaceLayer = (SurfaceLayer)path[0].Layer;

            for (int i = 0; i < path.Count - 1; i++)
            {
                var fromTile = surfaceLayer[path[i]];
                var toTile = surfaceLayer[path[i + 1]];

                if (editingRivers)
                {
                    if (selectedRiverDef != null)
                    {
                        bool linkExists = fromTile.potentialRivers?.Any(r => r.neighbor == toTile.tile && r.river == selectedRiverDef) ?? false;

                        if (!linkExists)
                        {
                            if (fromTile.potentialRivers == null)
                                fromTile.potentialRivers = new List<SurfaceTile.RiverLink>();
                            if (toTile.potentialRivers == null)
                                toTile.potentialRivers = new List<SurfaceTile.RiverLink>();

                            var linkFromTo = new SurfaceTile.RiverLink { neighbor = toTile.tile, river = selectedRiverDef };
                            var linkToFrom = new SurfaceTile.RiverLink { neighbor = fromTile.tile, river = selectedRiverDef };

                            fromTile.potentialRivers.Add(linkFromTo);
                            toTile.potentialRivers.Add(linkToFrom);

                            temporaryRiverLinks.Add((fromTile, toTile, linkFromTo));
                            temporaryRiverLinks.Add((toTile, fromTile, linkToFrom));
                        }
                    }
                }
                else if (editingRoads)
                {
                    if (selectedRoadDef != null)
                    {
                        bool linkExists = fromTile.potentialRoads?.Any(r => r.neighbor == toTile.tile && r.road == selectedRoadDef) ?? false;

                        if (!linkExists)
                        {
                            if (fromTile.potentialRoads == null)
                                fromTile.potentialRoads = new List<SurfaceTile.RoadLink>();
                            if (toTile.potentialRoads == null)
                                toTile.potentialRoads = new List<SurfaceTile.RoadLink>();

                            var linkFromTo = new SurfaceTile.RoadLink { neighbor = toTile.tile, road = selectedRoadDef };
                            var linkToFrom = new SurfaceTile.RoadLink { neighbor = fromTile.tile, road = selectedRoadDef };

                            fromTile.potentialRoads.Add(linkFromTo);
                            toTile.potentialRoads.Add(linkToFrom);

                            temporaryRoadLinks.Add((fromTile, toTile, linkFromTo));
                            temporaryRoadLinks.Add((toTile, fromTile, linkToFrom));
                        }
                    }
                }
            }
        }

        private void CancelEditing()
        {
            ClearTemporaryLinks();
            pathStartTile = PlanetTile.Invalid;
            previewPath.Clear();
            lastPreviewTile = PlanetTile.Invalid;
            rightClickDragging = false;
            editingRoads = false;
            editingRivers = false;
            selectedRoadDef = null;
            selectedRiverDef = null;

            if (pathStartTile.Valid)
            {
                RegenerateActiveLayer(pathStartTile);
            }
        }

        private void DeleteSegmentsAtTile(PlanetTile tile)
        {
            var surfaceLayer = (SurfaceLayer)tile.Layer;
            var tileData = surfaceLayer[tile];

            if (editingRivers)
            {
                if (tileData.potentialRivers != null)
                {
                    var neighbors = tileData.potentialRivers.Select(r => r.neighbor).ToList();

                    foreach (var neighbor in neighbors)
                    {
                        var neighborTile = surfaceLayer[neighbor];
                        RemoveRiverLink(tileData, neighborTile);
                    }

                    RegenerateActiveLayer(tile);
                }
            }
            else if (editingRoads)
            {
                if (tileData.potentialRoads != null)
                {
                    var neighbors = tileData.potentialRoads.Select(r => r.neighbor).ToList();

                    foreach (var neighbor in neighbors)
                    {
                        var neighborTile = surfaceLayer[neighbor];
                        RemoveRoadLink(tileData, neighborTile);
                    }

                    RegenerateActiveLayer(tile);
                }
            }
        }

        private void RegenerateActiveLayer(PlanetTile tile)
        {
            if (editingRivers)
            {
                Find.World.renderer.GetLayer<WorldDrawLayer_Rivers>(tile.Layer)?.RegenerateNow();
            }
            else if (editingRoads)
            {
                Find.World.renderer.GetLayer<WorldDrawLayer_Roads>(tile.Layer)?.RegenerateNow();
            }
        }

        private void ClearTemporaryLinks()
        {
            foreach (var (from, to, link) in temporaryRiverLinks)
            {
                if (from.potentialRivers != null)
                {
                    for (int i = from.potentialRivers.Count - 1; i >= 0; i--)
                    {
                        if (from.potentialRivers[i].neighbor == link.neighbor &&
                            from.potentialRivers[i].river == link.river)
                        {
                            if (i == from.potentialRivers.Count - 1 ||
                                from.potentialRivers.FindLastIndex(r => r.neighbor == link.neighbor && r.river == link.river) == i)
                            {
                                from.potentialRivers.RemoveAt(i);
                                if (from.potentialRivers.Count == 0)
                                {
                                    from.potentialRivers = null;
                                }
                                break;
                            }
                        }
                    }
                }
            }
            temporaryRiverLinks.Clear();

            foreach (var (from, to, link) in temporaryRoadLinks)
            {
                if (from.potentialRoads != null)
                {
                    for (int i = from.potentialRoads.Count - 1; i >= 0; i--)
                    {
                        if (from.potentialRoads[i].neighbor == link.neighbor &&
                            from.potentialRoads[i].road == link.road)
                        {
                            if (i == from.potentialRoads.Count - 1 ||
                                from.potentialRoads.FindLastIndex(r => r.neighbor == link.neighbor && r.road == link.road) == i)
                            {
                                from.potentialRoads.RemoveAt(i);
                                if (from.potentialRoads.Count == 0)
                                {
                                    from.potentialRoads = null;
                                }
                                break;
                            }
                        }
                    }
                }
            }
            temporaryRoadLinks.Clear();
        }

        public override void Close(bool doCloseSound = true)
        {
            ClearTemporaryLinks();
            rightClickDragging = false;

            if (pathStartTile.Valid)
            {
                RegenerateActiveLayer(pathStartTile);
            }

            base.Close(doCloseSound);
        }
    }
}
