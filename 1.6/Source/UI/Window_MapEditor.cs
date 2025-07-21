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
        public MapEditingMode currentMode = MapEditingMode.Paint;
        public BiomeDef selectedBiome;
        public Hilliness selectedHilliness = Hilliness.Flat;
        public List<LandmarkDef> selectedLandmarks = new List<LandmarkDef>();
        public List<FeatureDef> selectedFeatures = new List<FeatureDef>();
        public int copiedTileID = -1;
        public int selectedTileID = -1;
        public LandmarkDef selectedLandmarkEntry;
        public FeatureDef selectedFeatureEntry;
        public Dictionary<FeatureDef, string> copiedFeatureNames = new Dictionary<FeatureDef, string>();
        public Vector2 biomeScrollPosition = Vector2.zero;
        public Vector2 landmarksScrollPosition = Vector2.zero;
        public Vector2 featuresScrollPosition = Vector2.zero;

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
        }

        public override void ExtraOnGUI()
        {
            base.ExtraOnGUI();
            if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && Event.current.button == 0 && !Mouse.IsOver(this.windowRect))
            {
                int tile = GenWorld.MouseTile(false);
                if (tile >= 0)
                {
                    if (selectedTileID != tile)
                    {
                        selectedTileID = tile;
                        if (currentMode == MapEditingMode.Paint)
                        {
                            PaintTile(tile);
                        }
                        else if (currentMode == MapEditingMode.Copy)
                        {
                            CopyTileProperties(tile);
                        }
                    }
                    Event.current.Use();
                }
            }
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect panelRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height - 45f);
            float curY = panelRect.y;
            Text.Font = GameFont.Small;

            float iconSize = 24f;
            float buttonHeight = 30f;
            float buttonSpacing = 5f;

            float labelWidth = 100f;
            float dropdownWidth = panelRect.width - 20f - labelWidth - 10f;

            DrawModeButton(ref curY, panelRect, buttonHeight, buttonSpacing, iconSize, "Worldbuilder/UI/MapEditor/paint", "WB_MapEditorPaintTile".Translate(), MapEditingMode.Paint);
            DrawModeButton(ref curY, panelRect, buttonHeight, buttonSpacing, iconSize, "Worldbuilder/UI/MapEditor/copy", "WB_MapEditorCopyTile".Translate(), MapEditingMode.Copy);

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(panelRect.x, curY - 5, panelRect.width - 20f, Text.LineHeight), "WB_MapEditorBrushProperties".Translate());
            curY += Text.LineHeight;
            Text.Font = GameFont.Small;

            Rect biomeLabelRect = new Rect(panelRect.x, curY, labelWidth, Text.LineHeight);
            Widgets.Label(biomeLabelRect, "WB_MapEditorBiome".Translate());
            Rect biomeDropdownRect = new Rect(biomeLabelRect.xMax, curY, dropdownWidth, 30f);
            if (Widgets.ButtonText(biomeDropdownRect, selectedBiome?.LabelCap ?? "None".Translate()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                options.Add(new FloatMenuOption("None".Translate(), () => selectedBiome = null));
                foreach (BiomeDef biome in DefDatabase<BiomeDef>.AllDefs.Where(x => x.generatesNaturally).OrderBy(b => b.label))
                {
                    options.Add(new FloatMenuOption(biome.LabelCap, () => selectedBiome = biome));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            curY += 35f;

            Rect hillinessLabelRect = new Rect(panelRect.x, curY, labelWidth, Text.LineHeight);
            Widgets.Label(hillinessLabelRect, "WB_MapEditorTerrain".Translate());
            Rect hillinessDropdownRect = new Rect(hillinessLabelRect.xMax, curY, dropdownWidth, 30f);
            if (Widgets.ButtonText(hillinessDropdownRect, selectedHilliness.GetLabelCap()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
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

            DrawDefListSection(ref curY, panelRect, "WB_MapEditorLandmarks".Translate(), selectedLandmarks, ref selectedLandmarkEntry, ref landmarksScrollPosition,
                (LandmarkDef l) => l.LabelCap,
                () => DefDatabase<LandmarkDef>.AllDefs.OrderBy(l => l.label),
                (LandmarkDef l) => selectedLandmarks.Add(l),
                (LandmarkDef l) => selectedLandmarks.Remove(l));
            curY += 10f;
            DrawDefListSection(ref curY, panelRect, "WB_MapEditorFeatures".Translate(), selectedFeatures, ref selectedFeatureEntry, ref featuresScrollPosition,
                (FeatureDef f) => f.defName,
                () => DefDatabase<FeatureDef>.AllDefs.OrderBy(f => f.label),
                (FeatureDef f) => selectedFeatures.Add(f),
                (FeatureDef f) => selectedFeatures.Remove(f));

            float doneButtonWidth = 120f;
            float doneButtonHeight = 35f;
            Rect doneButtonRect = new Rect(inRect.width - doneButtonWidth, inRect.y, doneButtonWidth, doneButtonHeight);
            if (Widgets.ButtonText(doneButtonRect, "WB_Done".Translate()))
            {
                Close();
            }
        }

        public void PaintTile(int tileID)
        {
            Tile tile = Find.WorldGrid[tileID];

            if (selectedBiome != null)
            {
                tile.biome = selectedBiome;
            }
            if (selectedHilliness != Hilliness.Undefined)
            {
                tile.hilliness = selectedHilliness;
            }
            if (Find.World.landmarks[new PlanetTile(tileID, Find.WorldGrid[tileID].Layer)] != null)
            {
                Find.World.landmarks.RemoveLandmark(new PlanetTile(tileID, Find.WorldGrid[tileID].Layer));
            }
            foreach (LandmarkDef landmarkDef in selectedLandmarks)
            {
                if (landmarkDef.IsValidTile(tile.tile, tile.Layer))
                {
                    Find.World.landmarks.AddLandmark(landmarkDef, tile.tile, tile.Layer, true);
                }
                else
                {
                    Messages.Message("WB_MapEditorLandmarkInvalidTile".Translate(landmarkDef.label), MessageTypeDefOf.RejectInput);
                }
            }
            if (tile.feature != null)
            {
                Find.World.features.features.Remove(tile.feature);
                tile.feature = null;
            }
            foreach (FeatureDef featureDef in selectedFeatures)
            {
                WorldFeature newFeature = new WorldFeature(featureDef, Find.WorldGrid[tileID].Layer);
                newFeature.uniqueID = Find.UniqueIDsManager.GetNextWorldFeatureID();
                newFeature.drawCenter = Find.WorldGrid.GetTileCenter(tileID);
                newFeature.name = copiedFeatureNames.TryGetValue(featureDef, out string name) ? name : NameGenerator.GenerateName(featureDef.nameMaker, Find.WorldFeatures.features.Select((WorldFeature x) => x.name), appendNumberIfNameUsed: false, "r_name");
                if (newFeature.name.NullOrEmpty())
                {
                    newFeature.name = "Feature";
                }
                Find.WorldGrid[tileID].feature = newFeature;
                Find.World.features.features.Add(newFeature);
                Find.WorldFeatures.CreateTextsAndSetPosition();
            }
            Find.World.renderer.GetLayer<WorldDrawLayer_Terrain>(Find.WorldGrid.Surface).RegenerateNow();
            Find.World.renderer.GetLayer<WorldDrawLayer_Landmarks>(Find.WorldGrid.Surface).RegenerateNow();
            Find.World.renderer.GetLayer<WorldDrawLayer_Hills>(Find.WorldGrid.Surface).RegenerateNow();
            Find.World.renderer.RegenerateAllLayersNow();
        }

        public void CopyTileProperties(int tileID)
        {
            Tile tile = Find.WorldGrid[tileID];

            selectedBiome = tile.biome;
            selectedHilliness = tile.hilliness;

            selectedLandmarks.Clear();
            Landmark landmark = Find.World.landmarks[tileID];
            if (landmark != null)
            {
                selectedLandmarks.Add(landmark.def);
            }

            selectedFeatures.Clear();
            copiedFeatureNames.Clear();
            if (tile.feature != null)
            {
                selectedFeatures.Add(tile.feature.def);
                copiedFeatureNames[tile.feature.def] = tile.feature.name;
            }
            Find.World.renderer.RegenerateAllLayersNow();
            Find.WorldFeatures.CreateTextsAndSetPosition();
        }

        private void DrawModeButton(ref float curY, Rect panelRect, float buttonHeight, float buttonSpacing, float iconSize, string texturePath, string label, MapEditingMode mode)
        {
            Rect buttonRect = new Rect(panelRect.x, curY, panelRect.width - 200f, buttonHeight);
            Rect iconRect = new Rect(buttonRect.x, buttonRect.y + (buttonHeight - iconSize) / 2, iconSize, iconSize);
            Rect labelRect = new Rect(iconRect.xMax + 5f, buttonRect.y, buttonRect.width - iconSize - 5f, buttonHeight);

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
            Rect labelRect = new Rect(panelRect.x, curY, panelRect.width - 20f, Text.LineHeight);
            Widgets.Label(labelRect, sectionLabel);
            curY += Text.LineHeight + 5f;

            Rect listRect = new Rect(panelRect.x, curY, panelRect.width - 20f, 100f);
            Widgets.DrawMenuSection(listRect);

            Rect viewRect = new Rect(0, 0, listRect.width - 16f, selectedDefs.Count * 24f);
            Widgets.BeginScrollView(listRect, ref scrollPosition, viewRect);
            float currentDefY = 0;
            for (int i = 0; i < selectedDefs.Count; i++)
            {
                Rect rowRect = new Rect(0, currentDefY, viewRect.width, 24f);
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

            Rect buttonsRect = new Rect(panelRect.x, curY, panelRect.width - 20f, 24f);
            Rect removeButtonRect = new Rect(buttonsRect.xMax - 48f, buttonsRect.y, 24f, 24f);
            Rect addButtonRect = new Rect(buttonsRect.xMax - 24f, buttonsRect.y, 24f, 24f);

            if (selectedEntry != null && Widgets.ButtonImage(removeButtonRect, TexButton.Minus))
            {
                removeAction(selectedEntry);
                selectedEntry = default(T);
            }
            if (Widgets.ButtonImage(addButtonRect, TexButton.Plus))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (T def in allDefsSelector())
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
