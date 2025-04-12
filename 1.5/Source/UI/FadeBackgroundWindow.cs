using Verse;
using UnityEngine;

namespace Worldbuilder
{
    [HotSwappable]
    public class FadeBackgroundWindow : Window
    {
        private float overlayAlpha = 0f;
        private bool isFadingIn = true;
        public FadeBackgroundWindow()
        {
            this.doCloseX = false;
            this.closeOnClickedOutside = false;
            this.preventCameraMotion = false;
            this.draggable = false;
            this.forcePause = false;
            this.doCloseButton = false;
            this.doWindowBackground = false;
            this.drawShadow = false;
        }

        public override Vector2 InitialSize => new Vector2(UI.screenWidth, UI.screenHeight);
        public override float Margin => 0;
        public override void PreOpen()
        {
            base.PreOpen();
            overlayAlpha = 0f;
            isFadingIn = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            if (isFadingIn)
            {
                overlayAlpha = Mathf.Lerp(overlayAlpha, 0.5f, Time.deltaTime * 4f);
                if (overlayAlpha >= 0.5f)
                {
                    isFadingIn = false;
                }
            }
            Color overlayColor = new Color(0f, 0f, 0f, overlayAlpha);
            Widgets.DrawBoxSolid(inRect, overlayColor);
        }
    }
}