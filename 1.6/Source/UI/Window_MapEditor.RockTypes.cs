using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    public partial class Window_MapEditor
    {
        private readonly List<ThingDef> selectedRockTypes = new List<ThingDef>();
        private readonly List<ThingDef> strokeRockTypes = new List<ThingDef>();
        private ThingDef selectedRockTypeEntry;
        private Vector2 rockTypesScrollPosition;
        private bool paintRockTypes;
        private bool rockStrokeActive;
        private static List<ThingDef> cachedNaturalRockTypes;

        private void DrawRockTypesSection(
            ref float curY,
            Rect panelRect)
        {
            var paintRect = new Rect(
                panelRect.x,
                curY,
                panelRect.width - 20f,
                24f);
            Widgets.Checkbox(
                paintRect.x,
                paintRect.y + 3f,
                ref paintRockTypes);
            var paintLabelRect = new Rect(
                paintRect.x + 28f,
                paintRect.y + 5f,
                paintRect.width - 28f,
                paintRect.height);
            Widgets.Label(
                paintLabelRect,
                "WB_MapEditorPaintRockTypes".Translate());
            if (Widgets.ButtonInvisible(paintLabelRect))
            {
                paintRockTypes = !paintRockTypes;
            }

            curY += 30f;
            DrawDefListSection(
                ref curY,
                panelRect,
                selectedRockTypes,
                ref selectedRockTypeEntry,
                ref rockTypesScrollPosition,
                "WB_MapEditorSelectRockType".Translate(),
                (ThingDef rock) => DefDisplayLabel(rock),
                () => NaturalRockTypes(),
                (ThingDef rock) => selectedRockTypes.Add(rock),
                (ThingDef rock) => selectedRockTypes.Remove(rock));
        }

        private void BeginRockStroke()
        {
            if (rockStrokeActive)
            {
                return;
            }

            rockStrokeActive = true;
            strokeRockTypes.Clear();
            strokeRockTypes.AddRange(
                selectedRockTypes
                    .Where(TileRockOverrideRecord.IsValidRock)
                    .Distinct()
                    .OrderBy(rock => rock.defName, StringComparer.Ordinal));
        }

        private void EndRockStroke()
        {
            rockStrokeActive = false;
            strokeRockTypes.Clear();
        }

        private void PaintRockTypes(IReadOnlyList<PlanetTile> tiles)
        {
            if (!paintRockTypes || tiles == null)
            {
                return;
            }

            BeginRockStroke();
            if (strokeRockTypes.Count == 0)
            {
                return;
            }

            foreach (var tile in tiles)
            {
                if (!RockOverrideService.IsEditableSurfaceTile(
                        Find.World,
                        tile))
                {
                    continue;
                }

                RockOverrideService.SetOverride(
                    tile,
                    strokeRockTypes);
            }
        }

        private void CopyRockTypes(PlanetTile tile)
        {
            selectedRockTypes.Clear();
            selectedRockTypeEntry = null;
            if (!RockOverrideService.IsEditableSurfaceTile(
                    Find.World,
                    tile))
            {
                return;
            }

            selectedRockTypes.AddRange(
                Find.World.NaturalRockTypesIn(tile)
                    .Where(TileRockOverrideRecord.IsValidRock)
                    .Distinct());
        }

        private static List<ThingDef> NaturalRockTypes()
        {
            return cachedNaturalRockTypes ??=
                DefDatabase<ThingDef>.AllDefsListForReading
                    .Where(TileRockOverrideRecord.IsValidRock)
                    .OrderBy(
                        rock => rock.LabelCap.ToString(),
                        StringComparer.OrdinalIgnoreCase)
                    .ThenBy(
                        rock => rock.defName,
                        StringComparer.Ordinal)
                    .ToList();
        }

        private static string DefDisplayLabel(Def def)
        {
            var label = def.LabelCap.ToString();
            if (WorldbuilderMod.settings
                    ?.showContentSourceOnScrollWindow != true)
            {
                return label;
            }

            var source = def.modContentPack?.Name;
            return source.NullOrEmpty()
                ? label
                : $"{label} - {source}";
        }
    }
}
