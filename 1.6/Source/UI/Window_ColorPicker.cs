using RimWorld;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    [StaticConstructorOnStartup]
    public class Window_ColorPicker : Window
    {
        private Color color;
        private readonly Color oldColor;
        private readonly Action<Color> onColorSelected;
        private readonly Action<Color> onColorPreview;
        private readonly bool showAlpha;

        private float hue;
        private float saturation;
        private float value;

        private bool svDragging;
        private bool hueDragging;
        private bool alphaDragging;

        private string hexBuffer;
        private Color lastPreviewedColor;
        private int activeSliderId = -1;

        private static readonly Texture2D SaturationGradient;
        private static readonly Texture2D ValueGradient;
        private static readonly Texture2D HueGradient;
        private static readonly Texture2D CheckerTile;
        private static readonly Texture2D RingMarker;

        private const float SvSize = 236f;
        private const float BarWidth = 24f;
        private const float RowHeight = 24f;
        private const float SwatchSize = 24f;

        private static readonly Vector2 ButSize = new Vector2(150f, 38f);
        public override Vector2 InitialSize => new Vector2(760f, showAlpha ? 620f : 580f);

        static Window_ColorPicker()
        {
            SaturationGradient = MakeGradient(128, 1, (u, v) => new Color(1f, 1f, 1f, 1f - u));
            ValueGradient = MakeGradient(1, 128, (u, v) => new Color(0f, 0f, 0f, 1f - v));
            HueGradient = MakeGradient(1, 256, (u, v) => Color.HSVToRGB(1f - v, 1f, 1f));
            CheckerTile = MakeChecker(16);
            RingMarker = MakeRing(32, 3);
        }

        public Window_ColorPicker(Color initialColor, Action<Color> onColorSelected, Action<Color> onColorPreview = null, bool showAlpha = false)
        {
            this.doCloseX = true;
            this.color = initialColor;
            this.oldColor = initialColor;
            this.lastPreviewedColor = initialColor;
            this.onColorSelected = onColorSelected;
            this.onColorPreview = onColorPreview;
            this.showAlpha = showAlpha;

            Color.RGBToHSV(initialColor, out hue, out saturation, out value);
            hexBuffer = ToHex(initialColor, showAlpha);

            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            closeOnAccept = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            using (TextBlock.Default())
            {
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 32f), "ChooseAColor".Translate().CapitalizeFirst());
                Text.Font = GameFont.Small;

                float bodyTop = inRect.y + 40f;
                float bodyBottom = inRect.yMax - ButSize.y - 10f;

                var leftRect = new Rect(inRect.x, bodyTop, SvSize + BarWidth + 10f, bodyBottom - bodyTop);
                var rightRect = new Rect(leftRect.xMax + 20f, bodyTop, inRect.xMax - leftRect.xMax - 20f, bodyBottom - bodyTop);

                DrawPickingArea(leftRect);
                DrawNumericArea(rightRect);

                DrawBottomButtons(inRect);
            }

            PushLivePreview();
        }

        private void DrawPickingArea(Rect rect)
        {
            var svRect = new Rect(rect.x, rect.y, SvSize, SvSize);
            DrawSaturationValueSquare(svRect);

            var hueRect = new Rect(svRect.xMax + 10f, svRect.y, BarWidth, SvSize);
            DrawHueBar(hueRect);

            float y = svRect.yMax + 12f;

            if (showAlpha)
            {
                var alphaRect = new Rect(rect.x, y, SvSize + 10f + BarWidth, BarWidth);
                DrawAlphaBar(alphaRect);
                y = alphaRect.yMax + 12f;
            }

            var swatchArea = new Rect(rect.x, y, rect.width, rect.yMax - y);
            DrawSwatches(swatchArea);
        }

        private void DrawSaturationValueSquare(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, Color.HSVToRGB(hue, 1f, 1f));
            GUI.DrawTexture(rect, SaturationGradient);
            GUI.DrawTexture(rect, ValueGradient);
            Widgets.DrawBox(rect);

            var marker = new Vector2(rect.x + saturation * rect.width, rect.y + (1f - value) * rect.height);
            DrawMarker(marker, 14f);

            if (HandleBarInput(rect, ref svDragging, out Vector2 local))
            {
                saturation = Mathf.Clamp01(local.x / rect.width);
                value = Mathf.Clamp01(1f - local.y / rect.height);
                SyncFromHsv();
            }
        }

        private void DrawHueBar(Rect rect)
        {
            GUI.DrawTexture(rect, HueGradient);
            Widgets.DrawBox(rect);

            float markerY = rect.y + hue * rect.height;
            DrawMarker(new Vector2(rect.center.x, markerY), 14f);

            if (HandleBarInput(rect, ref hueDragging, out Vector2 local))
            {
                hue = Mathf.Clamp01(local.y / rect.height);
                SyncFromHsv();
            }
        }

        private void DrawAlphaBar(Rect rect)
        {
            DrawChecker(rect);
            var opaque = color;
            opaque.a = 1f;
            DrawHorizontalFade(rect, opaque);
            Widgets.DrawBox(rect);

            DrawMarker(new Vector2(rect.x + color.a * rect.width, rect.center.y), 14f);

            if (HandleBarInput(rect, ref alphaDragging, out Vector2 local))
            {
                color.a = Mathf.Clamp01(local.x / rect.width);
                hexBuffer = ToHex(color, showAlpha);
            }
        }

        private static void DrawHorizontalFade(Rect rect, Color opaque)
        {
            int steps = 32;
            float stepWidth = rect.width / steps;
            for (int i = 0; i < steps; i++)
            {
                var c = opaque;
                c.a = (i + 0.5f) / steps;
                Widgets.DrawBoxSolid(new Rect(rect.x + i * stepWidth, rect.y, stepWidth + 1f, rect.height), c);
            }
        }

        private void DrawNumericArea(Rect rect)
        {
            float y = rect.y;

            var previewRect = new Rect(rect.x, y, rect.width, 46f);
            DrawColorReadback(previewRect);
            y = previewRect.yMax + 10f;

            var hexLabelRect = new Rect(rect.x, y, 44f, RowHeight);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(hexLabelRect, "WB_ColorHex".Translate());
            Text.Anchor = TextAnchor.UpperLeft;

            var hexFieldRect = new Rect(hexLabelRect.xMax + 4f, y, 108f, RowHeight);
            string typed = Widgets.TextField(hexFieldRect, hexBuffer);
            if (typed != hexBuffer)
            {
                hexBuffer = typed;
                if (TryGetColorFromHex(typed, out var parsed))
                {
                    if (!showAlpha) parsed.a = color.a;
                    color = parsed;
                    SyncFromRgb(updateHex: false);
                }
            }

            var copyRect = new Rect(hexFieldRect.xMax + 6f, y, 62f, RowHeight);
            if (Widgets.ButtonText(copyRect, "WB_Copy".Translate()))
            {
                GUIUtility.systemCopyBuffer = ToHex(color, showAlpha);
            }

            var pasteRect = new Rect(copyRect.xMax + 6f, y, 62f, RowHeight);
            if (Widgets.ButtonText(pasteRect, "WB_Paste".Translate()))
            {
                if (TryGetColorFromHex(GUIUtility.systemCopyBuffer, out var pasted))
                {
                    if (!showAlpha) pasted.a = color.a;
                    color = pasted;
                    SyncFromRgb();
                }
                else
                {
                    Messages.Message("WB_ColorHexInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
                }
            }
            y += RowHeight + 14f;

            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, y, rect.width, 18f), "WB_ColorChannelsRGB".Translate());
            Text.Font = GameFont.Small;
            y += 18f;

            float r = color.r, g = color.g, b = color.b;
            bool rgbChanged = false;
            y = DrawChannelSlider(rect, y, 0, "R", ref r, v => (v * 255f).ToString("0"), ref rgbChanged);
            y = DrawChannelSlider(rect, y, 1, "G", ref g, v => (v * 255f).ToString("0"), ref rgbChanged);
            y = DrawChannelSlider(rect, y, 2, "B", ref b, v => (v * 255f).ToString("0"), ref rgbChanged);
            if (rgbChanged)
            {
                color = new Color(r, g, b, color.a);
                SyncFromRgb();
            }

            y += 6f;
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, y, rect.width, 18f), "WB_ColorChannelsHSV".Translate());
            Text.Font = GameFont.Small;
            y += 18f;

            float h2 = hue, s2 = saturation, v2 = value;
            bool hsvChanged = false;
            y = DrawChannelSlider(rect, y, 3, "H", ref h2, v => (v * 360f).ToString("0") + "°", ref hsvChanged);
            y = DrawChannelSlider(rect, y, 4, "S", ref s2, v => (v * 100f).ToString("0") + "%", ref hsvChanged);
            y = DrawChannelSlider(rect, y, 5, "V", ref v2, v => (v * 100f).ToString("0") + "%", ref hsvChanged);
            if (hsvChanged)
            {
                hue = h2;
                saturation = s2;
                value = v2;
                SyncFromHsv();
            }

            if (showAlpha)
            {
                y += 6f;
                float a2 = color.a;
                bool alphaChanged = false;
                y = DrawChannelSlider(rect, y, 6, "A", ref a2, v => (v * 100f).ToString("0") + "%", ref alphaChanged);
                if (alphaChanged)
                {
                    color.a = a2;
                    hexBuffer = ToHex(color, showAlpha);
                }
            }

            y += 10f;
            DrawRecentColors(new Rect(rect.x, y, rect.width, rect.yMax - y));
        }

        private float DrawChannelSlider(Rect rect, float y, int id, string label, ref float channel, Func<float, string> readout, ref bool changed)
        {
            var rowRect = new Rect(rect.x, y, rect.width, RowHeight);

            var sliderRect = new Rect(rowRect.x + 20f, rowRect.y + 3f, rowRect.width - 20f - 48f, 18f);
            channel = SliderUtility.Draw(sliderRect, ref activeSliderId, id, channel, 0f, 1f, 1f / 255f, 0f, out bool sliderChanged);
            changed |= sliderChanged;

            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(rowRect.x, rowRect.y, 18f, RowHeight), label);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(new Rect(rowRect.xMax - 44f, rowRect.y, 44f, RowHeight), readout(channel));
            Text.Anchor = TextAnchor.UpperLeft;

            return y + RowHeight + 2f;
        }

        private void DrawColorReadback(Rect rect)
        {
            rect.SplitVertically(rect.width / 2f, out var left, out var right);
            left.xMax -= 4f;

            DrawSwatchBox(left, color);
            DrawSwatchBox(right, oldColor);

            var leftLabel = new Rect(left.x + 1f, left.yMax - 17f, left.width - 2f, 16f);
            var rightLabel = new Rect(right.x + 1f, right.yMax - 17f, right.width - 2f, 16f);
            var labelBackdrop = new Color(0f, 0f, 0f, 0.55f);
            Widgets.DrawBoxSolid(leftLabel, labelBackdrop);
            Widgets.DrawBoxSolid(rightLabel, labelBackdrop);

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            Widgets.Label(leftLabel, "CurrentColor".Translate().CapitalizeFirst());
            Widgets.Label(rightLabel, "OldColor".Translate().CapitalizeFirst());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;

            TooltipHandler.TipRegion(right, "WB_ColorRestoreOld".Translate());
            if (Widgets.ButtonInvisible(right))
            {
                color = oldColor;
                SyncFromRgb();
            }
        }

        private void DrawSwatchBox(Rect rect, Color swatch)
        {
            if (swatch.a < 1f)
            {
                DrawChecker(rect);
            }
            Widgets.DrawBoxSolid(rect, swatch);
            Widgets.DrawBox(rect);
        }

        private void DrawSwatches(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 18f), "WB_ColorPalette".Translate());
            Text.Font = GameFont.Small;

            var gridRect = new Rect(rect.x, rect.y + 18f, rect.width, rect.height - 18f);
            DrawSwatchGrid(gridRect, PaletteColors);
        }

        private void DrawRecentColors(Rect rect)
        {
            var recents = WorldbuilderMod.settings?.recentColors;
            if (recents == null || recents.Count == 0) return;

            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, 18f), "WB_ColorRecent".Translate());
            Text.Font = GameFont.Small;

            DrawSwatchGrid(new Rect(rect.x, rect.y + 18f, rect.width, rect.height - 18f), recents);
        }

        private void DrawSwatchGrid(Rect rect, List<Color> colors)
        {
            int perRow = Mathf.Max(1, Mathf.FloorToInt(rect.width / (SwatchSize + 4f)));
            for (int i = 0; i < colors.Count; i++)
            {
                int row = i / perRow;
                int col = i % perRow;
                var swatchRect = new Rect(rect.x + col * (SwatchSize + 4f), rect.y + row * (SwatchSize + 4f), SwatchSize, SwatchSize);
                if (swatchRect.yMax > rect.yMax) break;

                Widgets.DrawBoxSolid(swatchRect, colors[i]);
                if (Mouse.IsOver(swatchRect))
                {
                    Widgets.DrawBox(swatchRect, 2);
                }
                else
                {
                    Widgets.DrawBox(swatchRect);
                }

                if (Widgets.ButtonInvisible(swatchRect, doMouseoverSound: false))
                {
                    var picked = colors[i];
                    if (!showAlpha) picked.a = color.a;
                    color = picked;
                    SyncFromRgb();
                }
            }
        }

        private void DrawBottomButtons(Rect inRect)
        {
            float y = inRect.yMax - ButSize.y;
            if (Widgets.ButtonText(new Rect(inRect.x, y, ButSize.x, ButSize.y), "Cancel".Translate()))
            {
                CancelAndClose();
            }
            if (Widgets.ButtonText(new Rect(inRect.center.x - ButSize.x / 2f, y, ButSize.x, ButSize.y), "WB_ColorResetToOld".Translate()))
            {
                color = oldColor;
                SyncFromRgb();
            }
            if (Widgets.ButtonText(new Rect(inRect.xMax - ButSize.x, y, ButSize.x, ButSize.y), "Accept".Translate()))
            {
                RememberColor(color);
                onColorSelected?.Invoke(color);
                accepted = true;
                Close();
            }
        }

        private bool accepted;
        private bool closing;

        public override void Close(bool doCloseSound = true)
        {
            if (!accepted)
            {
                onColorPreview?.Invoke(oldColor);
            }
            closing = true;
            base.Close(doCloseSound);
        }

        private void CancelAndClose()
        {
            onColorPreview?.Invoke(oldColor);
            accepted = true;
            Close();
        }

        private void PushLivePreview()
        {
            if (onColorPreview == null || closing) return;
            if (Event.current.type != EventType.Repaint) return;
            if (svDragging || hueDragging || alphaDragging || Input.GetMouseButton(0)) return;
            if (color == lastPreviewedColor) return;

            lastPreviewedColor = color;
            onColorPreview(color);
        }

        private void SyncFromHsv()
        {
            var rgb = Color.HSVToRGB(hue, saturation, value);
            rgb.a = color.a;
            color = rgb;
            hexBuffer = ToHex(color, showAlpha);
        }

        private void SyncFromRgb(bool updateHex = true)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            hue = h;
            saturation = s;
            value = v;
            if (updateHex)
            {
                hexBuffer = ToHex(color, showAlpha);
            }
        }

        private static bool HandleBarInput(Rect rect, ref bool dragging, out Vector2 local)
        {
            local = Vector2.zero;
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && Mouse.IsOver(rect))
            {
                dragging = true;
                local = e.mousePosition - rect.position;
                e.Use();
                return true;
            }
            if (e.type == EventType.MouseDrag && dragging)
            {
                local = e.mousePosition - rect.position;
                e.Use();
                return true;
            }
            if (e.rawType == EventType.MouseUp && dragging)
            {
                dragging = false;
                e.Use();
            }
            return false;
        }

        private static void DrawMarker(Vector2 center, float size)
        {
            var markerRect = new Rect(center.x - size / 2f, center.y - size / 2f, size, size);
            GUI.color = Color.black;
            GUI.DrawTexture(markerRect.ExpandedBy(1f), RingMarker);
            GUI.color = Color.white;
            GUI.DrawTexture(markerRect, RingMarker);
        }

        private static void DrawChecker(Rect rect)
        {
            GUI.DrawTextureWithTexCoords(rect, CheckerTile, new Rect(0f, 0f, rect.width / 16f, rect.height / 16f));
        }

        private static List<Color> paletteColors;
        private static List<Color> PaletteColors
        {
            get
            {
                if (paletteColors == null)
                {
                    paletteColors = new List<Color>();
                    paletteColors.Add(Color.white);
                    paletteColors.Add(new Color(0.75f, 0.75f, 0.75f));
                    paletteColors.Add(new Color(0.5f, 0.5f, 0.5f));
                    paletteColors.Add(new Color(0.25f, 0.25f, 0.25f));
                    paletteColors.Add(Color.black);
                    paletteColors.Add(DarklightUtility.DefaultDarklight);
                    foreach (var c in Dialog_GlowerColorPicker.colors)
                    {
                        if (!paletteColors.Any(x => x.IndistinguishableFrom(c)))
                        {
                            paletteColors.Add(c);
                        }
                    }
                    foreach (var colorDef in DefDatabase<ColorDef>.AllDefsListForReading)
                    {
                        if (!paletteColors.Any(x => x.IndistinguishableFrom(colorDef.color)))
                        {
                            paletteColors.Add(colorDef.color);
                        }
                    }
                }
                return paletteColors;
            }
        }

        private static void RememberColor(Color c)
        {
            var settings = WorldbuilderMod.settings;
            if (settings == null) return;

            settings.recentColors.RemoveAll(x => x.IndistinguishableFrom(c));
            settings.recentColors.Insert(0, c);
            while (settings.recentColors.Count > 16)
            {
                settings.recentColors.RemoveAt(settings.recentColors.Count - 1);
            }
            settings.Write();
        }

        public static string ToHex(Color c, bool includeAlpha = false)
        {
            var c32 = (Color32)c;
            string hex = "#" + c32.r.ToString("X2") + c32.g.ToString("X2") + c32.b.ToString("X2");
            if (includeAlpha)
            {
                hex += c32.a.ToString("X2");
            }
            return hex;
        }

        public static bool TryGetColorFromHex(string hex, out Color color)
        {
            color = Color.white;
            if (hex.NullOrEmpty())
            {
                return false;
            }
            hex = hex.Trim();
            if (hex.StartsWith("#"))
            {
                hex = hex.Substring(1);
            }

            if (hex.Length != 6 && hex.Length != 8)
            {
                return false;
            }

            try
            {
                int r = int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                int g = int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                int b = int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                int a = 255;
                if (hex.Length == 8)
                {
                    a = int.Parse(hex.Substring(6, 2), NumberStyles.HexNumber);
                }
                color = GenColor.FromBytes(r, g, b, a);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static Texture2D MakeGradient(int width, int height, Func<float, float, Color> pixel)
        {
            var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < height; y++)
            {
                float v = height == 1 ? 0f : (float)y / (height - 1);
                for (int x = 0; x < width; x++)
                {
                    float u = width == 1 ? 0f : (float)x / (width - 1);
                    texture.SetPixel(x, y, pixel(u, v));
                }
            }
            texture.Apply();
            return texture;
        }

        private static Texture2D MakeChecker(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Repeat;
            var light = new Color(0.62f, 0.62f, 0.62f);
            var dark = new Color(0.42f, 0.42f, 0.42f);
            int half = size / 2;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool even = (x < half) ^ (y < half);
                    texture.SetPixel(x, y, even ? light : dark);
                }
            }
            texture.Apply();
            return texture;
        }

        private static Texture2D MakeRing(int resolution, float thickness)
        {
            var texture = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            var center = new Vector2(resolution / 2f, resolution / 2f);
            float outer = resolution / 2f - 1f;
            float inner = outer - thickness;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = Mathf.Clamp01(outer - distance) * Mathf.Clamp01(distance - inner);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();
            return texture;
        }
    }
}
