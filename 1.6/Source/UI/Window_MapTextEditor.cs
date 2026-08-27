using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using System.Linq;

namespace Worldbuilder
{
    [HotSwappable]
    public class Window_MapTextEditor : Window
    {
        private Vector2 scrollPositionLabels = Vector2.zero;
        private WorldFeature selectedFeature
        {
            get => _selectedFeature;
            set
            {
                _selectedFeature = value;
                if (value != null)
                {
                    var tileId = GetTileIdForFeature(value);
                    Vector2 coords = (tileId.Valid) ? Find.WorldGrid.LongLatOf(tileId) : Vector2.zero;
                    xPosBuffer = Mathf.RoundToInt(coords.x).ToString();
                    yPosBuffer = Mathf.RoundToInt(coords.y).ToString();
                    rotationBuffer = Mathf.RoundToInt(value.drawAngle).ToString();
                    sizeBuffer = Mathf.RoundToInt(value.maxDrawSizeInTiles).ToString();
                }
            }
        }
        private WorldFeature _selectedFeature = null;
        private string xPosBuffer = "";
        private string yPosBuffer = "";
        private string rotationBuffer = "";
        private string sizeBuffer = "";
        private readonly QuickSearchWidget quickSearchWidget = new QuickSearchWidget();
        private static Vector2 cachedScrollPositionLabels;
        private static string cachedSearchText = string.Empty;
        private static bool hasCachedState;
        public override Vector2 InitialSize => new Vector2(600f, 500f);
        public Window_MapTextEditor()
        {
            forcePause = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            draggable = true;
            resizeable = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            if (hasCachedState)
            {
                quickSearchWidget.filter.Text = cachedSearchText;
                scrollPositionLabels = cachedScrollPositionLabels;
            }

            selectedFeature = Find.World.features.features.FirstOrDefault(
                feature => quickSearchWidget.filter.Matches(feature.name));
        }

        public override void PostClose()
        {
            hasCachedState = true;
            cachedSearchText = quickSearchWidget.filter.Text ?? string.Empty;
            cachedScrollPositionLabels = scrollPositionLabels;
            base.PostClose();
        }

        public override void DoWindowContents(Rect inRect)
        {
            var leftRect = new Rect(inRect.x, inRect.y, inRect.width * 0.4f - 10f, inRect.height);
            var rightRect = new Rect(leftRect.xMax + 10f, inRect.y, inRect.width * 0.6f, inRect.height);

            DrawLabelList(leftRect);
            DrawEditPanel(rightRect);
        }

        private void DrawLabelList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            float curY = rect.y + 10;
            var contentRect = rect.ContractedBy(10f);

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(contentRect.x, curY, contentRect.width, Text.LineHeight), "WB_EditMapText".Translate());
            curY += Text.LineHeight + 5f;
            Text.Font = GameFont.Small;

            var searchRect = new Rect(contentRect.x, curY, contentRect.width, 24f);
            quickSearchWidget.OnGUI(searchRect, delegate
            {
                var filtered = Find.World.features.features.Where(f => quickSearchWidget.filter.Matches(f.name)).ToList();
                if (selectedFeature == null || !quickSearchWidget.filter.Matches(selectedFeature.name))
                {
                    selectedFeature = filtered.FirstOrDefault();
                }
            }, delegate
            {
                selectedFeature = Find.World.features.features.FirstOrDefault();
            });
            curY += 24f + 10f;

            float buttonHeight = 30f;
            float buttonSpacing = 10f;
            float scrollViewHeight = Mathf.Max(
                0f,
                contentRect.height -
                (curY - contentRect.y) -
                buttonHeight -
                buttonSpacing);

            var scrollRectOuter = new Rect(contentRect.x, curY, contentRect.width, scrollViewHeight);
            var labelFeatures = Find.World.features.features.Where(f => quickSearchWidget.filter.Matches(f.name)).ToList();
            var featureHeight = 30;
            if (scrollRectOuter.width > 0f && scrollRectOuter.height > 0f)
            {
                var grid = ResponsiveOptionGrid.Create(
                    scrollRectOuter,
                    labelFeatures.Count,
                    featureHeight);
                grid.ClampScrollPosition(ref scrollPositionLabels);
                var viewRect = grid.ViewRect;

                Widgets.BeginScrollView(
                    scrollRectOuter,
                    ref scrollPositionLabels,
                    viewRect);
                var visibleTop = scrollPositionLabels.y;
                var visibleBottom = visibleTop + scrollRectOuter.height;
                for (var index = 0; index < labelFeatures.Count; index++)
                {
                    var feature = labelFeatures[index];
                    var rowRect = grid.RowRect(index);
                    if (rowRect.yMax < visibleTop || rowRect.y > visibleBottom)
                    {
                        continue;
                    }

                    string label = feature.name;

                    if (selectedFeature == feature)
                    {
                        Widgets.DrawHighlightSelected(rowRect);
                    }
                    else
                    {
                        if (grid.IsAlternatingRow(index))
                        {
                            Widgets.DrawLightHighlight(rowRect);
                        }

                        if (Mouse.IsOver(rowRect))
                        {
                            Widgets.DrawHighlight(rowRect);
                        }
                    }

                    Widgets.Label(new Rect(rowRect.x, rowRect.y, rowRect.width - 24f, rowRect.height).ContractedBy(2f), label);
                    Rect deleteRect = new Rect(rowRect.xMax - 24f, rowRect.y + (rowRect.height - 24f) / 2f, 24f, 24f);
                    if (Widgets.ButtonImage(deleteRect, TexButton.Delete))
                    {
                        RemoveFeature(feature);
                        break;
                    }
                    else if (Widgets.ButtonInvisible(rowRect))
                    {
                        selectedFeature = feature;
                    }
                }

                Widgets.EndScrollView();
            }

            var buttonRect = new Rect(contentRect.x, scrollRectOuter.yMax + buttonSpacing, contentRect.width, buttonHeight);

            if (Widgets.ButtonText(buttonRect, "WB_AddMapTextButton".Translate()))
            {
                AddFeature("WB_NewMapTextLabel".Translate(), Find.WorldSelector.SelectedTile);
            }
        }

        private void DrawEditPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            var contentRect = rect.ContractedBy(15f);
            float curY = contentRect.y;

            if (selectedFeature == null)
            {
                return;
            }
            var labelTextRect = new Rect(contentRect.x, curY, contentRect.width, Text.LineHeight);
            Rect nameFieldRect = labelTextRect;
            nameFieldRect.width -= 85f;
            var oldName = selectedFeature.name;
            selectedFeature.name = Widgets.TextField(nameFieldRect, selectedFeature.name);
            if (oldName != selectedFeature.name)
            {
                Find.WorldFeatures.CreateTextsAndSetPosition();
            }
            var randomizeRect = new Rect(nameFieldRect.xMax + 5f, curY, 80f, Text.LineHeight);
            if (Widgets.ButtonText(randomizeRect, "WB_MapTextEditorRandomize".Translate()))
            {
                if (selectedFeature.def?.nameMaker != null)
                {
                    selectedFeature.name = NameGenerator.GenerateName(selectedFeature.def.nameMaker, Find.WorldFeatures.features.Select((WorldFeature x) => x.name), appendNumberIfNameUsed: false, "r_name");
                }
                else
                {
                    selectedFeature.name = NameGenerator.GenerateName(DefsOf.NamerSettlementOutlander);
                }
                Find.WorldFeatures.CreateTextsAndSetPosition();
            }
            curY += Text.LineHeight + 10f;
            var tileId = GetTileIdForFeature(selectedFeature);
            Vector2 coords = (tileId.Valid) ? Find.WorldGrid.LongLatOf(tileId) : Vector2.zero;
            var oldXPos = Mathf.RoundToInt(coords.x);
            var oldYPos = Mathf.RoundToInt(coords.y);

            var xLabelRect = new Rect(contentRect.x, curY, contentRect.width, Text.LineHeight);
            Widgets.Label(xLabelRect.LeftHalf(), "WB_XPositionLabel".Translate());
            var xPos = Mathf.RoundToInt(coords.x);
            var xPosRect = xLabelRect.RightHalf();
            Widgets.TextFieldNumeric(xPosRect, ref xPos, ref xPosBuffer, min: int.MinValue, max: int.MaxValue);
            curY += Text.LineHeight;

            var yLabelRect = new Rect(contentRect.x, curY, contentRect.width, Text.LineHeight);
            Widgets.Label(yLabelRect.LeftHalf(), "WB_YPositionLabel".Translate());
            var yPos = Mathf.RoundToInt(coords.y);
            var yPosRect = yLabelRect.RightHalf();
            Widgets.TextFieldNumeric(yPosRect, ref yPos, ref yPosBuffer, min: int.MinValue, max: int.MaxValue);
            if (oldXPos != xPos || oldYPos != yPos)
            {
                SaveChanges(FindTile(xPos, yPos));
            }
            curY += Text.LineHeight;

            var rotationLabelRect = new Rect(contentRect.x, curY, contentRect.width, Text.LineHeight);
            Widgets.Label(rotationLabelRect.LeftHalf(), "WB_RotationLabel".Translate());
            var rotation = Mathf.RoundToInt(selectedFeature.drawAngle);
            var rotationRect = rotationLabelRect.RightHalf();
            Widgets.TextFieldNumeric(rotationRect, ref rotation, ref rotationBuffer, min: 0, max: 360);
            if (Mathf.RoundToInt(selectedFeature.drawAngle) != rotation)
            {
                selectedFeature.drawAngle = rotation;
                Find.WorldFeatures.CreateTextsAndSetPosition();
            }
            curY += Text.LineHeight;

            var sizeLabelRect = new Rect(contentRect.x, curY, contentRect.width, Text.LineHeight);
            Widgets.Label(sizeLabelRect.LeftHalf(), "WB_SizeLabel".Translate());
            var size = Mathf.RoundToInt(selectedFeature.maxDrawSizeInTiles);
            var sizeRect = sizeLabelRect.RightHalf();
            Widgets.TextFieldNumeric(sizeRect, ref size, ref sizeBuffer, min: 1, max: 200);
            if (Mathf.RoundToInt(selectedFeature.maxDrawSizeInTiles) != size)
            {
                selectedFeature.maxDrawSizeInTiles = size;
                Find.WorldFeatures.CreateTextsAndSetPosition();
            }
        }

        private void RemoveFeature(WorldFeature feature)
        {
            if (feature == null) return;
            if (selectedFeature == feature)
            {
                selectedFeature = null;
            }
            MapTextUtility.RemoveFeature(feature);
        }

        private void AddFeature(string labelText, PlanetTile tileId)
        {
            var feature = MapTextUtility.AddFeature(labelText, tileId);
            if (feature == null)
            {
                return;
            }

            MapTextUtility.FocusCameraOn(feature);
            selectedFeature = feature;
        }

        private void SaveChanges(PlanetTile newTileId)
        {
            var oldTileId = GetTileIdForFeature(selectedFeature);
            if (oldTileId != newTileId && newTileId.Valid && Find.WorldGrid.TilesCount > newTileId && Find.WorldGrid[newTileId].feature != selectedFeature)
            {
                foreach (var tile in selectedFeature.Tiles)
                {
                    Find.WorldGrid[tile].feature = null;
                }
                Find.WorldGrid[newTileId].feature = selectedFeature;
                selectedFeature.drawCenter = Find.WorldGrid.GetTileCenter(newTileId);
                Find.WorldFeatures.CreateTextsAndSetPosition();
            }
            else if (oldTileId != newTileId && !newTileId.Valid)
            {
                Vector2 coords = (oldTileId.Valid) ? Find.WorldGrid.LongLatOf(oldTileId) : Vector2.zero;
                xPosBuffer = Mathf.RoundToInt(coords.x).ToString();
                yPosBuffer = Mathf.RoundToInt(coords.y).ToString();
                Messages.Message("WB_MapTextInvalidPosition".Translate(), MessageTypeDefOf.RejectInput);
            }
        }

        public static PlanetTile GetTileIdForFeature(WorldFeature feature)
        {
            if (feature == null) return PlanetTile.Invalid;
            return feature.Tiles.FirstOrDefault();
        }

        private PlanetTile FindTile(int x, int y)
        {
            for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
            {
                var coords = Find.WorldGrid.LongLatOf(i);
                if (Mathf.RoundToInt(coords.x) == x && Mathf.RoundToInt(coords.y) == y)
                {
                    return new PlanetTile(i);
                }
            }
            return PlanetTile.Invalid;
        }
    }
}
