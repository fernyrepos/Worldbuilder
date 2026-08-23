using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public static class StoryCoverUtility
    {
        public static void DrawCover(Rect rect, Texture2D icon, Color color, float iconFraction = 0.62f)
        {
            Widgets.DrawMenuSection(rect);
            DrawCoverIcon(rect, icon, color, iconFraction);
        }

        public static void DrawCoverIcon(Rect rect, Texture2D icon, Color color, float iconFraction = 0.62f)
        {
            if (icon == null) return;

            float size = Mathf.Min(rect.width, rect.height) * iconFraction;
            var iconRect = new Rect(rect.center.x - size / 2f, rect.center.y - size / 2f, size, size);

            GUI.color = color;
            Widgets.DrawTextureFitted(iconRect, icon, 1f);
            GUI.color = Color.white;
        }
    }
}
