using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    internal enum WorldPollutionBrushMode
    {
        None,
        Paint,
        Erase
    }

    public partial class Window_MapEditor
    {
        private readonly HashSet<PlanetTile> pollutionProcessedTiles =
            new HashSet<PlanetTile>();
        private readonly List<PlanetTile> pollutionTiles =
            new List<PlanetTile>();
        private readonly List<PlanetTile> pollutionChangedTiles =
            new List<PlanetTile>();
        private WorldPollutionBrushMode pollutionBrushMode;
        private bool pollutionDragging;
        private PlanetTile lastPollutionTile = PlanetTile.Invalid;

        private bool HandlePollutionBrushInput()
        {
            if (pollutionBrushMode == WorldPollutionBrushMode.None)
            {
                return false;
            }

            if (!ModsConfig.BiotechActive)
            {
                ExitPollutionBrush();
                return false;
            }

            var currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 0)
            {
                BeginPollutionStroke();
                PaintPollutionAtMouse();
                currentEvent.Use();
                return true;
            }

            if (currentEvent.type == EventType.MouseUp &&
                currentEvent.button == 0)
            {
                EndPollutionStroke();
                currentEvent.Use();
                return true;
            }

            if (pollutionDragging)
            {
                Find.WorldSelector.dragBox.active = false;
                PaintPollutionAtMouse();
                if (currentEvent.isMouse)
                {
                    currentEvent.Use();
                }
            }

            return true;
        }

        private void ShowPollutionBrushMenu()
        {
            if (!ModsConfig.BiotechActive)
            {
                return;
            }

            Find.WindowStack.Add(
                new FloatMenu(
                    new List<FloatMenuOption>
                    {
                        new FloatMenuOption(
                            "WB_MapEditorPollutionBrush".Translate(),
                            () => EnterPollutionBrush(
                                WorldPollutionBrushMode.Paint)),
                        new FloatMenuOption(
                            "WB_MapEditorPollutionEraser".Translate(),
                            () => EnterPollutionBrush(
                                WorldPollutionBrushMode.Erase))
                    }));
        }

        private void EnterPollutionBrush(
            WorldPollutionBrushMode mode)
        {
            CancelEditing();
            EndPollutionStroke();
            pollutionBrushMode = mode;
            tilesToDraw.Clear();
        }

        private void ExitPollutionBrush()
        {
            if (pollutionBrushMode == WorldPollutionBrushMode.None)
            {
                return;
            }

            pollutionBrushMode = WorldPollutionBrushMode.None;
            EndPollutionStroke();
            ClearPollutionPreview();
        }

        private void BeginPollutionStroke()
        {
            if (pollutionDragging)
            {
                return;
            }

            pollutionDragging = true;
            lastPollutionTile = PlanetTile.Invalid;
            pollutionProcessedTiles.Clear();
            pollutionTiles.Clear();
            pollutionChangedTiles.Clear();
            tilesToDraw.Clear();
            Find.WorldSelector.dragBox.active = false;
        }

        private void EndPollutionStroke()
        {
            pollutionDragging = false;
            lastPollutionTile = PlanetTile.Invalid;
            pollutionProcessedTiles.Clear();
            pollutionTiles.Clear();
            pollutionChangedTiles.Clear();
            ClearPollutionPreview();
        }

        private void PaintPollutionAtMouse()
        {
            var tile = GenWorld.TileAt(UI.MousePositionOnUI);
            if (!RockOverrideService.IsEditableSurfaceTile(
                    Find.World,
                    tile) ||
                lastPollutionTile == tile)
            {
                return;
            }

            lastPollutionTile = tile;
            selectedTileID = tile;
            CollectPollutionTiles(tile);
            pollutionChangedTiles.Clear();
            foreach (var target in pollutionTiles)
            {
                var tileData = Find.WorldGrid[target];
                if (pollutionBrushMode ==
                        WorldPollutionBrushMode.Paint &&
                    tileData.PrimaryBiome?.allowPollution != true)
                {
                    continue;
                }

                tilesToDraw.Add(target);
                var pollution =
                    pollutionBrushMode ==
                    WorldPollutionBrushMode.Paint
                        ? 1f
                        : 0f;
                if (Mathf.Approximately(
                        tileData.pollution,
                        pollution))
                {
                    continue;
                }

                tileData.pollution = pollution;
                pollutionChangedTiles.Add(target);
            }

            RefreshPollutionLayer(
                tile.Layer,
                pollutionChangedTiles);
            Find.World.renderer
                .GetLayer<WorldDrawLayer_SelectedTiles>(
                    tile.Layer)
                ?.RegenerateNow();
        }

        private void CollectPollutionTiles(PlanetTile center)
        {
            pollutionTiles.Clear();
            if (brushSize <= 0)
            {
                if (pollutionProcessedTiles.Add(center))
                {
                    pollutionTiles.Add(center);
                }

                return;
            }

            center.Layer.Filler.FloodFill(
                center,
                _ => true,
                delegate(PlanetTile tile, int distance)
                {
                    if (distance <= brushSize &&
                        pollutionProcessedTiles.Add(tile))
                    {
                        pollutionTiles.Add(tile);
                    }
                },
                brushSize);
        }

        private static void RefreshPollutionLayer(
            PlanetLayer layer,
            IReadOnlyList<PlanetTile> changedTiles)
        {
            if (changedTiles.Count > 500)
            {
                Find.World.renderer
                    .GetLayer<WorldDrawLayer_Pollution>(layer)
                    ?.RegenerateNow();
                return;
            }

            foreach (var changedTile in changedTiles)
            {
                Find.World.renderer
                    .Notify_TilePollutionChanged(changedTile);
            }
        }

        private void ClearPollutionPreview()
        {
            tilesToDraw.Clear();
            if (selectedTileID.Valid)
            {
                Find.World.renderer
                    .GetLayer<WorldDrawLayer_SelectedTiles>(
                        selectedTileID.Layer)
                    ?.RegenerateNow();
            }
        }
    }
}
