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
        public override void SetInitialSizeAndPosition()
        {
            windowRect = new Rect(0f, 0f, InitialSize.x, InitialSize.y).Rounded();
        }

        public override Vector2 InitialSize => new Vector2(400f, 535f);
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
        public override void ExtraOnGUI()
        {
            base.ExtraOnGUI();

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

            var biomeLabelRect = new Rect(panelRect.x, curY, labelWidth, Text.LineHeight);
            Widgets.Label(biomeLabelRect, "WB_MapEditorBiome".Translate());
            var biomeDropdownRect = new Rect(biomeLabelRect.xMax, curY, dropdownWidth, 30f);
            if (Widgets.ButtonText(biomeDropdownRect, selectedBiome?.LabelCap ?? selectedBiome?.defName ?? "WB_Select".Translate()))
            {
                var options = new List<FloatMenuOption>();
                foreach (var biome in DefDatabase<BiomeDef>.AllDefs.Where(x => x.generatesNaturally).OrderBy(b => b.label))
                {
                    options.Add(new FloatMenuOption(biome.LabelCap, () => selectedBiome = biome));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            curY += 35f;

            var hillinessLabelRect = new Rect(panelRect.x, curY, labelWidth, Text.LineHeight);
            Widgets.Label(hillinessLabelRect, "WB_MapEditorTerrain".Translate());
            var hillinessDropdownRect = new Rect(hillinessLabelRect.xMax, curY, dropdownWidth, 30f);
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
                DrawDefListSection(ref curY, panelRect, "WB_MapEditorLandmarks".Translate(), selectedLandmarks, ref selectedLandmarkEntry, ref landmarksScrollPosition,
                    (LandmarkDef l) => l.LabelCap,
                    () => DefDatabase<LandmarkDef>.AllDefs.OrderBy(l => l.label),
                    (LandmarkDef l) => selectedLandmarks.Add(l),
                    (LandmarkDef l) => selectedLandmarks.Remove(l));
                curY += 10f;
            }
            DrawDefListSection(ref curY, panelRect, "WB_MapEditorFeatures".Translate(), selectedFeatures, ref selectedFeatureEntry, ref featuresScrollPosition,
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
                if (selectedBiome != null)
                {
                    tileData.biome = selectedBiome;
                }
                if (selectedHilliness != Hilliness.Undefined)
                {
                    tileData.hilliness = selectedHilliness;
                }
                if (ModsConfig.OdysseyActive)
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
                if (selectedFeatures.Any() && tileData.Mutators.NullOrEmpty() is false)
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

        private void DrawDefListSection<T>(ref float curY, Rect panelRect, string sectionLabel, List<T> selectedDefs, ref T selectedEntry, ref Vector2 scrollPosition,
            System.Func<T, string> labelSelector, System.Func<IEnumerable<T>> allDefsSelector, System.Action<T> addAction, System.Action<T> removeAction) where T : Def
        {
            var labelRect = new Rect(panelRect.x, curY, panelRect.width - 20f, Text.LineHeight);
            Widgets.Label(labelRect, sectionLabel);
            curY += Text.LineHeight + 5f;

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
    }
}
