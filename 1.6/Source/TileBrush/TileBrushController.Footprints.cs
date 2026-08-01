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
        internal void DrawPreview(
            IntVec3 mouseCell,
            bool singleCell)
        {
            if (!mouseCell.InBounds(Map))
            {
                return;
            }

            var center = mouseCell.ToVector3ShiftedWithAltitude(
                AltitudeLayer.MetaOverlays);
            if (singleCell)
            {
                GenDraw.DrawCircleOutline(center, 0.5f);
                return;
            }

            var radius = Mathf.Max(0.5f, settings.Radius + 0.5f);
            switch (settings.Shape)
            {
                case TileBrushShape.Rectangle:
                    DrawRectanglePreview(center, radius);
                    break;
                case TileBrushShape.Rugged:
                    DrawRuggedPreview(
                        mouseCell,
                        center,
                        settings.Radius);
                    break;
                default:
                    GenDraw.DrawCircleOutline(center, radius);
                    break;
            }
        }

        private void DrawRectanglePreview(
            Vector3 center,
            float halfExtent)
        {
            var left = Mathf.Max(0f, center.x - halfExtent);
            var right = Mathf.Min(Map.Size.x, center.x + halfExtent);
            var bottom = Mathf.Max(0f, center.z - halfExtent);
            var top = Mathf.Min(Map.Size.z, center.z + halfExtent);
            var bottomLeft = new Vector3(left, center.y, bottom);
            var topLeft = new Vector3(left, center.y, top);
            var topRight = new Vector3(right, center.y, top);
            var bottomRight = new Vector3(right, center.y, bottom);
            GenDraw.DrawLineBetween(bottomLeft, topLeft);
            GenDraw.DrawLineBetween(topLeft, topRight);
            GenDraw.DrawLineBetween(topRight, bottomRight);
            GenDraw.DrawLineBetween(bottomRight, bottomLeft);
        }

        private void DrawRuggedPreview(
            IntVec3 centerCell,
            Vector3 center,
            int radius)
        {
            if (radius <= 0)
            {
                GenDraw.DrawCircleOutline(
                    center,
                    Mathf.Max(0.5f, radius + 0.5f));
                return;
            }

            var sectorCount = PrepareRuggedEdgeNoise(
                centerCell,
                radius);
            var previous = Vector3.zero;
            for (var sector = 0; sector <= sectorCount; sector++)
            {
                var normalizedAngle =
                    sector / (float)sectorCount;
                var angle =
                    normalizedAngle * 2f * Mathf.PI -
                    Mathf.PI;
                var edgeRadius = RuggedEdgeRadius(
                    radius,
                    sectorCount,
                    normalizedAngle);
                var point = new Vector3(
                    Mathf.Clamp(
                        center.x + Mathf.Cos(angle) * edgeRadius,
                        0f,
                        Map.Size.x),
                    center.y,
                    Mathf.Clamp(
                        center.z + Mathf.Sin(angle) * edgeRadius,
                        0f,
                        Map.Size.z));
                if (sector > 0)
                {
                    GenDraw.DrawLineBetween(previous, point);
                }

                previous = point;
            }
        }

        private void ApplyPathCenter(IntVec3 center)
        {
            CollectFootprint(center, footprintCells);
            ApplyFootprintCells();
        }

        private void ApplyPathCenter(
            IntVec3 center,
            IntVec3 previousCenter)
        {
            CollectIncrementalFootprint(
                center,
                previousCenter,
                footprintCells);
            ApplyFootprintCells();
        }

        private void ApplyFootprintCells()
        {
            foreach (var cell in footprintCells)
            {
                if (!TryMarkProcessed(cell))
                {
                    continue;
                }

                if (relocationDestinations.Contains(cell))
                {
                    activeRecord.Skipped++;
                    continue;
                }

                ApplyCell(cell);
            }
        }

        private void AdvanceProcessedCellGeneration()
        {
            processedCellGeneration++;
            if (processedCellGeneration != 0)
            {
                return;
            }

            Array.Clear(
                processedCellStamps,
                0,
                processedCellStamps.Length);
            processedCellGeneration = 1;
        }

        private bool TryMarkProcessed(IntVec3 cell)
        {
            var index = Map.cellIndices.CellToIndex(cell);
            if (processedCellStamps[index] == processedCellGeneration)
            {
                return false;
            }

            processedCellStamps[index] = processedCellGeneration;
            return true;
        }

        private bool WasProcessed(IntVec3 cell)
        {
            return processedCellStamps[
                       Map.cellIndices.CellToIndex(cell)] ==
                   processedCellGeneration;
        }

        private void CollectIncrementalFootprint(
            IntVec3 center,
            IntVec3 previousCenter,
            List<IntVec3> destination)
        {
            destination.Clear();
            var radius = Mathf.Max(0, settings.Radius);
            switch (settings.Shape)
            {
                case TileBrushShape.Rectangle:
                    AddRectangleFootprintDelta(
                        center,
                        previousCenter,
                        radius,
                        destination);
                    break;
                case TileBrushShape.Rugged:
                    AddRuggedFootprintDelta(
                        center,
                        previousCenter,
                        radius,
                        destination);
                    break;
                default:
                    AddCircleFootprintDelta(
                        center,
                        previousCenter,
                        radius,
                        destination);
                    break;
            }
        }

        private void CollectFootprint(
            IntVec3 center,
            List<IntVec3> destination)
        {
            destination.Clear();
            var radius = Mathf.Max(0, settings.Radius);
            switch (settings.Shape)
            {
                case TileBrushShape.Rectangle:
                    AddRectangleFootprint(
                        center,
                        radius,
                        destination);
                    break;
                case TileBrushShape.Rugged:
                    AddRuggedFootprint(
                        center,
                        radius,
                        destination);
                    break;
                default:
                    AddCircleFootprint(
                        center,
                        radius,
                        destination);
                    break;
            }
        }

        private void AddCircleFootprint(
            IntVec3 center,
            int radius,
            List<IntVec3> destination)
        {
            foreach (var cell in GenRadial.RadialCellsAround(
                         center,
                         radius,
                         useCenter: true))
            {
                if (cell.InBounds(Map))
                {
                    destination.Add(cell);
                }
            }
        }

        private void AddCircleFootprintDelta(
            IntVec3 center,
            IntVec3 previousCenter,
            int radius,
            List<IntVec3> destination)
        {
            var deltaX = center.x - previousCenter.x;
            var deltaZ = center.z - previousCenter.z;
            var stepDistance = Mathf.Sqrt(
                deltaX * deltaX +
                deltaZ * deltaZ);
            var minimumRadius = Mathf.Max(
                0f,
                radius - stepDistance);
            foreach (var cell in GenRadial.RadialCellsAround(
                         center,
                         minimumRadius,
                         radius))
            {
                if (cell.InBounds(Map))
                {
                    destination.Add(cell);
                }
            }
        }

        private void AddRectangleFootprint(
            IntVec3 center,
            int radius,
            List<IntVec3> destination)
        {
            for (var x = center.x - radius; x <= center.x + radius; x++)
            {
                for (var z = center.z - radius; z <= center.z + radius; z++)
                {
                    var cell = new IntVec3(x, 0, z);
                    if (cell.InBounds(Map))
                    {
                        destination.Add(cell);
                    }
                }
            }
        }

        private void AddRectangleFootprintDelta(
            IntVec3 center,
            IntVec3 previousCenter,
            int radius,
            List<IntVec3> destination)
        {
            var minimumX = Mathf.Max(0, center.x - radius);
            var maximumX = Mathf.Min(
                Map.Size.x - 1,
                center.x + radius);
            var minimumZ = Mathf.Max(0, center.z - radius);
            var maximumZ = Mathf.Min(
                Map.Size.z - 1,
                center.z + radius);
            var previousMinimumX = previousCenter.x - radius;
            var previousMaximumX = previousCenter.x + radius;
            var previousMinimumZ = previousCenter.z - radius;
            var previousMaximumZ = previousCenter.z + radius;

            for (var x = minimumX; x <= maximumX; x++)
            {
                if (x < previousMinimumX ||
                    x > previousMaximumX)
                {
                    AddRectangleColumn(
                        x,
                        minimumZ,
                        maximumZ,
                        destination);
                    continue;
                }

                AddRectangleColumn(
                    x,
                    minimumZ,
                    Mathf.Min(
                        maximumZ,
                        previousMinimumZ - 1),
                    destination);
                AddRectangleColumn(
                    x,
                    Mathf.Max(
                        minimumZ,
                        previousMaximumZ + 1),
                    maximumZ,
                    destination);
            }
        }

        private static void AddRectangleColumn(
            int x,
            int minimumZ,
            int maximumZ,
            List<IntVec3> destination)
        {
            for (var z = minimumZ; z <= maximumZ; z++)
            {
                destination.Add(new IntVec3(x, 0, z));
            }
        }

        private void AddRuggedFootprint(
            IntVec3 center,
            int radius,
            List<IntVec3> destination)
        {
            if (radius <= 0)
            {
                AddCircleFootprint(
                    center,
                    radius,
                    destination);
                return;
            }

            var sectorCount = PrepareRuggedEdgeNoise(
                center,
                radius);
            var edgeVariation = Mathf.Clamp(
                radius * 0.42f,
                1.25f,
                8f);
            for (var x = center.x - radius; x <= center.x + radius; x++)
            {
                for (var z = center.z - radius; z <= center.z + radius; z++)
                {
                    var cell = new IntVec3(x, 0, z);
                    if (!cell.InBounds(Map))
                    {
                        continue;
                    }

                    var offsetX = x - center.x;
                    var offsetZ = z - center.z;
                    var distance = Mathf.Sqrt(
                        offsetX * offsetX +
                        offsetZ * offsetZ);
                    if (distance <= radius - edgeVariation)
                    {
                        destination.Add(cell);
                        continue;
                    }

                    var normalizedAngle =
                        (Mathf.Atan2(offsetZ, offsetX) + Mathf.PI) /
                        (2f * Mathf.PI);
                    var edgeRadius = RuggedEdgeRadius(
                        radius,
                        sectorCount,
                        normalizedAngle);
                    if (distance <= edgeRadius)
                    {
                        destination.Add(cell);
                    }
                }
            }
        }

        private void AddRuggedFootprintDelta(
            IntVec3 center,
            IntVec3 previousCenter,
            int radius,
            List<IntVec3> destination)
        {
            if (radius <= 0)
            {
                AddCircleFootprintDelta(
                    center,
                    previousCenter,
                    radius,
                    destination);
                return;
            }

            var sectorCount = PrepareRuggedEdgeNoise(
                center,
                radius);
            var edgeVariation = Mathf.Clamp(
                radius * 0.42f,
                1.25f,
                8f);
            var deltaX = center.x - previousCenter.x;
            var deltaZ = center.z - previousCenter.z;
            var stepDistance = Mathf.Sqrt(
                deltaX * deltaX +
                deltaZ * deltaZ);
            var minimumRadius = Mathf.Max(
                0f,
                radius -
                edgeVariation -
                stepDistance);

            foreach (var cell in GenRadial.RadialCellsAround(
                         center,
                         minimumRadius,
                         radius + 0.35f))
            {
                if (!cell.InBounds(Map))
                {
                    continue;
                }

                var offsetX = cell.x - center.x;
                var offsetZ = cell.z - center.z;
                if (Mathf.Abs(offsetX) > radius ||
                    Mathf.Abs(offsetZ) > radius)
                {
                    continue;
                }

                var distance = Mathf.Sqrt(
                    offsetX * offsetX +
                    offsetZ * offsetZ);
                if (distance <= radius - edgeVariation)
                {
                    destination.Add(cell);
                    continue;
                }

                var normalizedAngle =
                    (Mathf.Atan2(offsetZ, offsetX) + Mathf.PI) /
                    (2f * Mathf.PI);
                if (distance <= RuggedEdgeRadius(
                        radius,
                        sectorCount,
                        normalizedAngle))
                {
                    destination.Add(cell);
                }
            }
        }

        private int PrepareRuggedEdgeNoise(
            IntVec3 center,
            int radius)
        {
            var sectorCount = Mathf.Clamp(
                radius + 6,
                10,
                Mathf.Min(28, MaxRuggedSectors));
            for (var sector = 0; sector < sectorCount; sector++)
            {
                ruggedEdgeNoise[sector] = RuggedEdgeSample(
                    center,
                    radius,
                    sector);
            }

            return sectorCount;
        }

        private float RuggedEdgeRadius(
            int radius,
            int sectorCount,
            float normalizedAngle)
        {
            var sectorPosition = normalizedAngle * sectorCount;
            var firstSector =
                Mathf.FloorToInt(sectorPosition) % sectorCount;
            var secondSector =
                (firstSector + 1) % sectorCount;
            var blend = sectorPosition -
                        Mathf.Floor(sectorPosition);
            blend = blend * blend * (3f - 2f * blend);
            var edgeNoise = Mathf.Lerp(
                ruggedEdgeNoise[firstSector],
                ruggedEdgeNoise[secondSector],
                blend);
            var edgeVariation = Mathf.Clamp(
                radius * 0.42f,
                1.25f,
                8f);
            return radius -
                   edgeVariation +
                   edgeNoise * edgeVariation +
                   0.35f;
        }

        private float RuggedEdgeSample(
            IntVec3 center,
            int radius,
            int sector)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = (hash ^ (uint)Map.uniqueID) * 16777619u;
                hash = (hash ^ (uint)center.x) * 16777619u;
                hash = (hash ^ (uint)center.z) * 16777619u;
                hash = (hash ^ (uint)radius) * 16777619u;
                hash = (hash ^ (uint)sector) * 16777619u;
                hash ^= hash >> 13;
                hash *= 1274126177u;
                return (hash & 0x00FFFFFFu) / 16777216f;
            }
        }

    }
}
