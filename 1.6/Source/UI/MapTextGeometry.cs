using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    public static class MapTextGeometry
    {
        public const float HandleSize = 10f;
        public const float RotateHandleOffset = 26f;
        private const float MinEditableAlpha = 0.5f;
        private const float MinRectSize = 22f;
        private const float RectPadding = 4f;

        public static bool IsEditable(int index, WorldFeature feature)
        {
            var texts = WorldFeatures.texts;
            if (feature == null || index < 0 || index >= texts.Count)
            {
                return false;
            }

            var mesh = texts[index];
            if (mesh == null || !mesh.Active)
            {
                return false;
            }

            if (feature.alpha < MinEditableAlpha)
            {
                return false;
            }

            return Find.WorldFeatures.GoodCameraAltitudeFor(feature);
        }

        public static bool TryGetScreenRect(int index, WorldFeature feature, out Rect rect)
        {
            rect = default;
            if (!IsEditable(index, feature))
            {
                return false;
            }

            var mesh = WorldFeatures.texts[index];
            if (mesh is WorldFeatureTextMesh_TextMeshPro tmp && tmp.textMesh != null)
            {
                var bounds = tmp.textMesh.bounds;
                if (bounds.size.x > 0.0001f && bounds.size.y > 0.0001f &&
                    TryProjectBounds(tmp.textMesh.transform.localToWorldMatrix, bounds, out rect))
                {
                    return true;
                }
            }

            return TryProjectFallback(mesh, feature, out rect);
        }

        public static Vector2 RotateHandleFor(Rect rect)
        {
            return new Vector2(rect.center.x, rect.yMin - RotateHandleOffset);
        }

        public static Vector2 CornerOf(Rect rect, int index)
        {
            switch (index)
            {
                case 0: return new Vector2(rect.xMin, rect.yMin);
                case 1: return new Vector2(rect.xMax, rect.yMin);
                case 2: return new Vector2(rect.xMax, rect.yMax);
                default: return new Vector2(rect.xMin, rect.yMax);
            }
        }

        public static Quaternion RotationFor(WorldFeature feature, float drawAngle)
        {
            var normalized = feature.drawCenter.normalized;
            var rotation = Quaternion.LookRotation(Vector3.Cross(normalized, Vector3.up), normalized);
            rotation *= Quaternion.Euler(Vector3.right * 90f);
            rotation *= Quaternion.Euler(Vector3.forward * (90f - drawAngle));
            return rotation;
        }

        public static float ScreenAngleOfRightAxis(WorldFeature feature, float drawAngle)
        {
            var rotation = RotationFor(feature, drawAngle);
            float unit = Find.WorldGrid.AverageTileSize;
            if (!TryProject(feature.drawCenter, out var origin) ||
                !TryProject(feature.drawCenter + rotation * (Vector3.right * unit), out var tip))
            {
                return 0f;
            }

            var axis = tip - origin;
            return Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg;
        }

        private static bool TryProjectBounds(Matrix4x4 matrix, Bounds bounds, out Rect rect)
        {
            rect = default;
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            var extents = bounds.extents;

            for (var i = 0; i < 8; i++)
            {
                var corner = bounds.center + new Vector3(
                    (i & 1) == 0 ? -extents.x : extents.x,
                    (i & 2) == 0 ? -extents.y : extents.y,
                    (i & 4) == 0 ? -extents.z : extents.z);

                if (!TryProject(matrix.MultiplyPoint3x4(corner), out var screen))
                {
                    return false;
                }

                min = Vector2.Min(min, screen);
                max = Vector2.Max(max, screen);
            }

            rect = Finalize(new Rect(min.x, min.y, max.x - min.x, max.y - min.y));
            return true;
        }

        private static bool TryProjectFallback(WorldFeatureTextMesh mesh, WorldFeature feature, out Rect rect)
        {
            rect = default;
            if (!TryProject(mesh.Position, out var center))
            {
                return false;
            }

            float worldSize = feature.EffectiveDrawSize * Find.WorldGrid.AverageTileSize;
            if (!TryProject(mesh.Position + mesh.Rotation * new Vector3(worldSize, 0f, 0f), out var edge))
            {
                return false;
            }

            float unit = (edge - center).magnitude;
            var lines = (feature.name ?? string.Empty).Split('\n');
            int longest = 1;
            foreach (var line in lines)
            {
                longest = Mathf.Max(longest, line.Length);
            }

            float halfWidth = unit * 0.28f * longest;
            float halfHeight = unit * 0.6f * lines.Length;
            rect = Finalize(new Rect(center.x - halfWidth, center.y - halfHeight, halfWidth * 2f, halfHeight * 2f));
            return true;
        }

        private static Rect Finalize(Rect rect)
        {
            rect = rect.ExpandedBy(RectPadding);
            if (rect.width < MinRectSize)
            {
                rect.x -= (MinRectSize - rect.width) / 2f;
                rect.width = MinRectSize;
            }

            if (rect.height < MinRectSize)
            {
                rect.y -= (MinRectSize - rect.height) / 2f;
                rect.height = MinRectSize;
            }

            return rect;
        }

        private static bool TryProject(Vector3 worldPosition, out Vector2 screen)
        {
            var point = Find.WorldCamera.WorldToScreenPoint(worldPosition);
            if (point.z <= 0f)
            {
                screen = default;
                return false;
            }

            point /= Prefs.UIScale;
            screen = new Vector2(point.x, UI.screenHeight - point.y);
            return true;
        }
    }
}
