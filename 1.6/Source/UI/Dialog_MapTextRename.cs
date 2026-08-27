using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public class Dialog_MapTextRename : Window
    {
        private const string ControlName = "WB_MapTextRenameField";
        private const int MaxNameLength = 64;

        private readonly WorldFeature feature;
        private readonly Rect? anchor;
        private string curName;
        private bool focused;

        public override Vector2 InitialSize => new Vector2(360f, 172f);

        public Dialog_MapTextRename(WorldFeature feature, Rect? anchor = null)
        {
            this.feature = feature;
            this.anchor = anchor;
            curName = feature?.name ?? string.Empty;

            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
            closeOnAccept = false;
            doCloseX = true;
            preventCameraMotion = true;
        }

        public override void SetInitialSizeAndPosition()
        {
            if (anchor.HasValue is false)
            {
                base.SetInitialSizeAndPosition();
                return;
            }

            var size = InitialSize;
            float x = anchor.Value.center.x - size.x / 2f;
            float y = anchor.Value.yMax + 14f;
            if (y + size.y > UI.screenHeight - 8f)
            {
                y = anchor.Value.yMin - 14f - size.y;
            }

            x = Mathf.Clamp(x, 8f, UI.screenWidth - size.x - 8f);
            y = Mathf.Clamp(y, 8f, UI.screenHeight - size.y - 8f);
            windowRect = new Rect(x, y, size.x, size.y).Rounded();
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (feature == null || Find.World.features.features.Contains(feature) is false)
            {
                Close(false);
                return;
            }

            var e = Event.current;
            bool accept = false;
            if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                accept = true;
                e.Use();
            }

            Text.Font = GameFont.Medium;
            var titleRect = new Rect(inRect.x, inRect.y, inRect.width, Text.LineHeight + 6f);
            Widgets.Label(titleRect, "Rename".Translate());
            Text.Font = GameFont.Small;

            var fieldRect = new Rect(inRect.x, titleRect.yMax + 6f, inRect.width, 32f);
            GUI.SetNextControlName(ControlName);
            string text = Widgets.TextField(fieldRect, curName);
            if (text.Length <= MaxNameLength)
            {
                curName = text;
            }

            if (focused is false)
            {
                UI.FocusControl(ControlName, this);
                focused = true;
            }

            float buttonHeight = 34f;
            float buttonY = inRect.yMax - buttonHeight;
            float half = (inRect.width - 8f) / 2f;

            if (Widgets.ButtonText(new Rect(inRect.x, buttonY, half, buttonHeight), "WB_MapTextEditorRandomize".Translate()))
            {
                curName = MapTextUtility.RandomName(feature);
                focused = false;
            }

            bool ok = Widgets.ButtonText(new Rect(inRect.x + half + 8f, buttonY, half, buttonHeight), "OK".Translate(), active: curName.NullOrEmpty() is false);
            if (ok || accept)
            {
                Accept();
            }
        }

        private void Accept()
        {
            if (curName.NullOrEmpty())
            {
                return;
            }

            MapTextUtility.SetName(feature, curName);
            Close();
        }
    }
}
