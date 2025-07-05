using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using System.Collections.Generic;
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
                    int tileId = GetTileIdForFeature(value);
                    Vector2 coords = (tileId != -1) ? Find.WorldGrid.LongLatOf(tileId) : Vector2.zero;
                    xPosBuffer = Mathf.RoundToInt(coords.x).ToString();
                    yPosBuffer = Mathf.RoundToInt(coords.y).ToString();
                }
            }
        }
        private WorldFeature _selectedFeature = null;
        private string xPosBuffer = "";
        private string yPosBuffer = "";
        public override Vector2 InitialSize => new Vector2(600f, 500f);
        public Window_MapTextEditor()
        {
            forcePause = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            draggable = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            selectedFeature = Find.World.features.features.FirstOrDefault();
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect leftRect = new Rect(inRect.x, inRect.y, inRect.width * 0.4f - 10f, inRect.height);
            Rect rightRect = new Rect(leftRect.xMax + 10f, inRect.y, inRect.width * 0.6f, inRect.height);

            DrawLabelList(leftRect);
            DrawEditPanel(rightRect);
        }

        private void DrawLabelList(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            float curY = rect.y + 10;
            Rect contentRect = rect.ContractedBy(10f);


            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(contentRect.x, curY, contentRect.width, Text.LineHeight), "WB_GizmoEditMapTextLabel".Translate());
            curY += Text.LineHeight;
            Text.Font = GameFont.Small;

            float buttonHeight = 30f;
            float buttonSpacing = 10f;
            float scrollViewHeight = contentRect.height - (curY - contentRect.y) - buttonHeight - buttonSpacing;

            Rect scrollRectOuter = new Rect(contentRect.x, curY, contentRect.width, scrollViewHeight);
            var labelFeatures = Find.World.features.features.ToList();
            var featureHeight = 30;
            float viewHeight = labelFeatures.Count * featureHeight;
            Rect viewRect = new Rect(0f, 0f, scrollRectOuter.width - 16f, viewHeight);

            Widgets.BeginScrollView(scrollRectOuter, ref scrollPositionLabels, viewRect);
            float currentY = 0f;
            foreach (var feature in labelFeatures)
            {
                Rect rowRect = new Rect(0f, currentY, viewRect.width, featureHeight);
                string label = feature.name;

                if (selectedFeature == feature)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }
                else if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }

                if (Widgets.ButtonInvisible(rowRect))
                {
                    selectedFeature = feature;
                }
                Widgets.Label(rowRect.ContractedBy(2f), label);
                currentY += featureHeight;
            }
            Widgets.EndScrollView();

            Rect buttonRect = new Rect(contentRect.x, scrollRectOuter.yMax + buttonSpacing, contentRect.width, buttonHeight);

            if (Widgets.ButtonText(buttonRect, "WB_AddMapTextButton".Translate()))
            {
                AddFeature("WB_NewMapTextLabel".Translate(), Find.WorldSelector.SelectedTile);
            }
        }

        private void DrawEditPanel(Rect rect)
        {
            Widgets.DrawMenuSection(rect);
            Rect contentRect = rect.ContractedBy(15f);
            float curY = contentRect.y;

            Rect labelTextRect = new Rect(contentRect.x, curY, contentRect.width, Text.LineHeight);
            selectedFeature.name = Widgets.TextField(labelTextRect, selectedFeature.name);
            curY += Text.LineHeight + 10f;

            int tileId = GetTileIdForFeature(selectedFeature);
            Vector2 coords = (tileId != -1) ? Find.WorldGrid.LongLatOf(tileId) : Vector2.zero;

            Rect xLabelRect = new Rect(contentRect.x, curY, contentRect.width, Text.LineHeight);
            Widgets.Label(xLabelRect.LeftHalf(), "WB_XPositionLabel".Translate());
            int xPos = Mathf.RoundToInt(coords.x);
            Rect xPosRect = xLabelRect.RightHalf();
            Widgets.TextFieldNumeric(xPosRect, ref xPos, ref xPosBuffer, min: int.MinValue, max: int.MaxValue);
            curY += Text.LineHeight;

            Rect yLabelRect = new Rect(contentRect.x, curY, contentRect.width, Text.LineHeight);
            Widgets.Label(yLabelRect.LeftHalf(), "WB_YPositionLabel".Translate());
            int yPos = Mathf.RoundToInt(coords.y);
            Rect yPosRect = yLabelRect.RightHalf();
            Widgets.TextFieldNumeric(yPosRect, ref yPos, ref yPosBuffer, min: int.MinValue, max: int.MaxValue);
            SaveChanges(FindTile(xPos, yPos));
        }

        private void AddFeature(string labelText, PlanetTile tileId)
        {
            WorldFeature newFeature = new WorldFeature(DefsOf.WB_MapLabelFeature, tileId.Layer);
            newFeature.uniqueID = Find.UniqueIDsManager.GetNextWorldFeatureID();
            newFeature.name = labelText;
            Find.WorldGrid[tileId].feature = newFeature;
            newFeature.drawCenter = Find.WorldGrid.GetTileCenter(tileId);
            Find.World.features.features.Add(newFeature);
            Find.WorldFeatures.CreateTextsAndSetPosition();
        }

        private void SaveChanges(int newTileId)
        {
            int oldTileId = GetTileIdForFeature(selectedFeature);
            if (oldTileId != newTileId && Find.WorldGrid[newTileId].feature != selectedFeature)
            {
                foreach (var tile in selectedFeature.Tiles)
                {
                    Find.WorldGrid[tile].feature = null;
                }
                Find.WorldGrid[newTileId].feature = selectedFeature;
            }
            selectedFeature.drawCenter = Find.WorldGrid.GetTileCenter(newTileId);
            Find.WorldFeatures.CreateTextsAndSetPosition();
        }

        private int GetTileIdForFeature(WorldFeature feature)
        {
            if (feature == null) return -1;
            return feature.Tiles.FirstOrDefault();
        }

        private int FindTile(int x, int y)
        {
            for (int i = 0; i < Find.WorldGrid.TilesCount; i++)
            {
                Vector2 coords = Find.WorldGrid.LongLatOf(i);
                if (Mathf.RoundToInt(coords.x) == x && Mathf.RoundToInt(coords.y) == y)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
