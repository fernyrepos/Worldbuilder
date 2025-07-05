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
        private WorldFeature selectedFeature = null;
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
            float curY = rect.y;
            Rect contentRect = rect.ContractedBy(10f);
            

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(contentRect.x, curY, contentRect.width, Text.LineHeight), "Map text");
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
            
            if (Widgets.ButtonText(buttonRect, "Add map text"))
            {
                AddFeature("New Map Text", Find.WorldGrid.TilesCount / 2); // Placeholder tile for new feature
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
            Widgets.Label(xLabelRect.LeftHalf(), "X-Position");
            int xPos = Mathf.RoundToInt(coords.x);
            Rect xPosRect = xLabelRect.RightHalf();
            Widgets.TextFieldNumeric(xPosRect, ref xPos, ref xPosBuffer, min: int.MinValue, max: int.MaxValue);
            curY += Text.LineHeight;

            Rect yLabelRect = new Rect(contentRect.x, curY, contentRect.width, Text.LineHeight);
            Widgets.Label(yLabelRect.LeftHalf(), "Y-Position");
            int yPos = Mathf.RoundToInt(coords.y);
            Rect yPosRect = yLabelRect.RightHalf();
            Widgets.TextFieldNumeric(yPosRect, ref yPos, ref yPosBuffer, min: int.MinValue, max: int.MaxValue);            
            SaveChanges(FindTile(xPos, yPos));
        }
        
        private void AddFeature(string labelText, int tileId)
        {
            WorldFeature newFeature = new WorldFeature();
            newFeature.def = DefsOf.WB_MapLabelFeature;
            newFeature.uniqueID = Find.UniqueIDsManager.GetNextWorldFeatureID();
            newFeature.name = labelText;
            Find.WorldGrid[tileId].feature = newFeature;
            Find.World.features.features.Add(newFeature);
            Find.WorldFeatures.UpdateFeatures();
        }

        private void SaveChanges(int newTileId)
        {
            int oldTileId = GetTileIdForFeature(selectedFeature);
            if (oldTileId != newTileId && Find.WorldGrid[newTileId].feature != selectedFeature)
            {
                Log.Message(selectedFeature.name + " moved from tile " + oldTileId + " to tile " + newTileId);
                Find.WorldGrid[newTileId].feature = selectedFeature;
            }
            Find.WorldFeatures.UpdateFeatures();
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
