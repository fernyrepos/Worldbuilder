using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    public static class MapTextUtility
    {
        public const float MinDrawSize = 1f;
        public const float MaxDrawSize = 200f;

        private const float AlphaScale = 30f;
        private const float TargetScaledSize = 0.2f;
        private const float MaxAltitude = 1100f;

        public static WorldFeature AddFeature(string labelText, PlanetTile tileId)
        {
            if (!tileId.Valid)
            {
                return null;
            }

            var newFeature = new WorldFeature(DefsOf.WB_MapLabelFeature, tileId.Layer);
            newFeature.uniqueID = Find.UniqueIDsManager.GetNextWorldFeatureID();
            newFeature.name = labelText;
            newFeature.drawAngle = 0f;
            newFeature.maxDrawSizeInTiles = 40f;
            Find.WorldGrid[tileId].feature = newFeature;
            newFeature.drawCenter = Find.WorldGrid.GetTileCenter(tileId);
            Find.World.features.features.Add(newFeature);
            Find.WorldFeatures.CreateTextsAndSetPosition();
            return newFeature;
        }

        public static void RemoveFeature(WorldFeature feature)
        {
            if (feature == null)
            {
                return;
            }

            foreach (var tile in feature.Tiles.ToList())
            {
                if (Find.WorldGrid[tile].feature == feature)
                {
                    Find.WorldGrid[tile].feature = null;
                }
            }

            Find.World.features.features.Remove(feature);
            Find.WorldFeatures.CreateTextsAndSetPosition();
        }

        public static bool MoveFeatureToTile(WorldFeature feature, PlanetTile newTileId)
        {
            if (feature == null || !newTileId.Valid || Find.WorldGrid.TilesCount <= newTileId)
            {
                return false;
            }

            if (Find.WorldGrid[newTileId].feature == feature)
            {
                return false;
            }

            foreach (var tile in feature.Tiles.ToList())
            {
                Find.WorldGrid[tile].feature = null;
            }

            Find.WorldGrid[newTileId].feature = feature;
            feature.drawCenter = Find.WorldGrid.GetTileCenter(newTileId);
            Find.WorldFeatures.CreateTextsAndSetPosition();
            return true;
        }

        public static void SetDrawSize(WorldFeature feature, float size)
        {
            size = Mathf.Clamp(size, MinDrawSize, MaxDrawSize);
            if (Mathf.Approximately(feature.maxDrawSizeInTiles, size))
            {
                return;
            }

            feature.maxDrawSizeInTiles = size;
            Find.WorldFeatures.CreateTextsAndSetPosition();
        }

        public static void SetDrawAngle(WorldFeature feature, float angle)
        {
            angle %= 360f;
            if (angle < 0f)
            {
                angle += 360f;
            }

            if (Mathf.Approximately(feature.drawAngle, angle))
            {
                return;
            }

            feature.drawAngle = angle;
            Find.WorldFeatures.CreateTextsAndSetPosition();
        }

        public static void SetName(WorldFeature feature, string name)
        {
            if (feature.name == name)
            {
                return;
            }

            feature.name = name;
            Find.WorldFeatures.CreateTextsAndSetPosition();
        }

        public static float AltitudeToShow(WorldFeature feature)
        {
            float size = Mathf.Max(feature.EffectiveDrawSize, 0.0001f);
            float altitude = AlphaScale * Mathf.Sqrt(size / TargetScaledSize);
            return Mathf.Clamp(altitude, WorldCameraDriver.MinAltitude, MaxAltitude);
        }

        public static void FocusCameraOn(WorldFeature feature)
        {
            if (feature == null)
            {
                return;
            }

            var driver = Find.WorldCameraDriver;
            if (driver == null)
            {
                return;
            }

            PlanetLayer.Selected = feature.layer;
            driver.JumpTo(feature.drawCenter);

            float altitude = AltitudeToShow(feature);
            driver.altitude = altitude;
            driver.desiredAltitude = altitude;
            feature.alpha = 1f;
        }

        public static string RandomName(WorldFeature feature)
        {
            if (feature?.def?.nameMaker != null)
            {
                return NameGenerator.GenerateName(
                    feature.def.nameMaker,
                    Find.WorldFeatures.features.Select(x => x.name),
                    appendNumberIfNameUsed: false,
                    "r_name");
            }

            return NameGenerator.GenerateName(DefsOf.NamerSettlementOutlander);
        }
    }
}
