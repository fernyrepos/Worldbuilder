using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Worldbuilder
{
    [HotSwappable]
    public static class SliderUtility
    {
        private static float lastDragSoundTime = -1f;

        public static float Draw(Rect rect, ref int activeSliderId, int id, float value, float min, float max,
            float roundTo, float shiftSnap, out bool changed)
        {
            changed = false;
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && Mouse.IsOver(rect))
            {
                activeSliderId = id;
            }
            else if (activeSliderId == id && (e.rawType == EventType.MouseUp || (e.type == EventType.Repaint && !Input.GetMouseButton(0))))
            {
                activeSliderId = -1;
            }

            var sliderRect = rect;
            sliderRect.y += Mathf.Round((rect.height - 10f) / 2f);
            float raw = GUI.HorizontalSlider(sliderRect, value, min, max);

            if (activeSliderId != id)
            {
                return value;
            }

            float step = (shiftSnap > 0f && e.shift) ? shiftSnap : roundTo;
            float snapped = step > 0f ? Mathf.Clamp(Mathf.Round(raw / step) * step, min, max) : raw;
            if (snapped == value)
            {
                return value;
            }

            changed = true;
            PlayDragSound();
            return snapped;
        }

        private static void PlayDragSound()
        {
            if (Time.realtimeSinceStartup - lastDragSoundTime < 0.075f)
            {
                return;
            }
            lastDragSoundTime = Time.realtimeSinceStartup;
            SoundDefOf.DragSlider.PlayOneShotOnCamera();
        }
    }
}
