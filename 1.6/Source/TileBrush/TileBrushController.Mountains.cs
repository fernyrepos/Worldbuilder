using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    internal sealed partial class TileBrushController
    {
        internal void MarkMountainCellForFog(IntVec3 cell)
        {
            if (StrokeActive && cell.InBounds(Map))
            {
                mountainFogCells.Add(cell);
            }
        }

        private void TrackErasedMountainEdges(Thing rock)
        {
            foreach (var occupiedCell in rock.OccupiedRect())
            {
                foreach (var offset in GenAdj.AdjacentCells)
                {
                    var neighbor = occupiedCell + offset;
                    if (neighbor.InBounds(Map))
                    {
                        mountainRevealCells.Add(neighbor);
                    }
                }
            }
        }

        private void RevealErasedMountainEdges()
        {
            foreach (var cell in mountainRevealCells)
            {
                if (!Map.fogGrid.IsFogged(cell))
                {
                    continue;
                }

                var edifice = cell.GetEdifice(Map);
                if (!IsNaturalRockStructure(edifice) ||
                    !edifice.def.MakeFog ||
                    !HasUnfoggedExteriorNeighbor(cell))
                {
                    continue;
                }

                Map.fogGrid.Unfog(cell);
            }
        }

        private void RebuildMountainFog()
        {
            if (mountainFogCells.Count == 0)
            {
                return;
            }

            var paintedMountainCells = mountainFogCells.ToList();
            foreach (var cell in paintedMountainCells)
            {
                foreach (var offset in GenAdj.AdjacentCells)
                {
                    var neighbor = cell + offset;
                    if (neighbor.InBounds(Map) &&
                        IsFogBlockingRock(neighbor))
                    {
                        mountainFogCells.Add(neighbor);
                    }
                }
            }

            var fogBlockingRocks = mountainFogCells
                .Where(IsFogBlockingRock)
                .ToList();
            var recordedCells = new HashSet<IntVec3>(
                activeRecord.Changes.Select(change => change.Cell));
            var neighboringRockBefore = fogBlockingRocks
                .Where(cell => !recordedCells.Contains(cell))
                .ToDictionary(
                    cell => cell,
                    cell => CellSnapshot.Capture(Map, cell));

            foreach (var cell in fogBlockingRocks)
            {
                Map.fogGrid.Refog(CellRect.SingleCell(cell));
            }

            foreach (var cell in fogBlockingRocks)
            {
                if (HasUnfoggedExteriorNeighbor(cell))
                {
                    Map.fogGrid.Unfog(cell);
                }
            }

            foreach (var pair in neighboringRockBefore)
            {
                if (pair.Value.Fogged ==
                    Map.fogGrid.IsFogged(pair.Key))
                {
                    continue;
                }

                activeRecord.Changes.Add(
                    new CellChange(
                        pair.Key,
                        pair.Value,
                        CellSnapshot.Capture(Map, pair.Key),
                        irreversible: false,
                        fogOnly: true));
            }
        }

        private bool IsFogBlockingRock(IntVec3 cell)
        {
            var edifice = cell.GetEdifice(Map);
            return IsNaturalRockStructure(edifice) &&
                   edifice.def.MakeFog;
        }

        private bool HasUnfoggedExteriorNeighbor(IntVec3 cell)
        {
            foreach (var offset in GenAdj.AdjacentCells)
            {
                var neighbor = cell + offset;
                if (!neighbor.InBounds(Map) ||
                    Map.fogGrid.IsFogged(neighbor))
                {
                    continue;
                }

                var edifice = neighbor.GetEdifice(Map);
                if (edifice == null || !edifice.def.MakeFog)
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshMountainAfterSnapshots()
        {
            if (mountainFogCells.Count == 0)
            {
                return;
            }

            for (var i = 0; i < activeRecord.Changes.Count; i++)
            {
                var change = activeRecord.Changes[i];
                if (mountainFogCells.Contains(change.Cell))
                {
                    change.SetAfter(
                        CellSnapshot.Capture(Map, change.Cell));
                }
            }
        }

    }
}
