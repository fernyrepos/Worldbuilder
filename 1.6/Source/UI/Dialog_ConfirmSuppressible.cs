using System;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    [HotSwappable]
    public class Dialog_ConfirmSuppressible : Window
    {
        private readonly string title;
        private readonly string text;
        private readonly Action confirmAction;
        private readonly Action suppressAction;
        private bool suppress;

        public override Vector2 InitialSize => new Vector2(460f, 240f);

        public Dialog_ConfirmSuppressible(string text, string title, Action confirmAction, Action suppressAction)
        {
            this.text = text;
            this.title = title;
            this.confirmAction = confirmAction;
            this.suppressAction = suppressAction;

            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            closeOnAccept = false;
            doCloseX = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            float y = inRect.y;
            if (!title.NullOrEmpty())
            {
                Text.Font = GameFont.Medium;
                Widgets.Label(new Rect(inRect.x, y, inRect.width, 32f), title);
                Text.Font = GameFont.Small;
                y += 38f;
            }

            float buttonHeight = 34f;
            float checkboxHeight = 26f;

            var textRect = new Rect(inRect.x, y, inRect.width, inRect.yMax - y - buttonHeight - checkboxHeight - 16f);
            Widgets.Label(textRect, text);

            var checkRect = new Rect(inRect.x, inRect.yMax - buttonHeight - checkboxHeight - 8f, inRect.width, checkboxHeight);
            Widgets.CheckboxLabeled(checkRect, "WB_DontShowAgain".Translate(), ref suppress);

            float half = (inRect.width - 10f) / 2f;
            if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - buttonHeight, half, buttonHeight), "CancelButton".Translate()))
            {
                Close();
            }
            if (Widgets.ButtonText(new Rect(inRect.x + half + 10f, inRect.yMax - buttonHeight, half, buttonHeight),
                "Confirm".Translate(), true, true, ColorLibrary.RedReadable))
            {
                if (suppress)
                {
                    suppressAction?.Invoke();
                }
                Close();
                confirmAction?.Invoke();
            }
        }
    }
}
