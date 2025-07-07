using Verse;
using UnityEngine;
using Verse.Sound;

namespace Worldbuilder
{
    [HotSwappable]
    public class NarrativeWindow : Window
    {
        private string title;
        private string narrativeText;
        public override Vector2 InitialSize => new Vector2(700f, 768);
        public NarrativeWindow(string title, string narrativeText)
        {
            this.title = title;
            this.narrativeText = narrativeText;
            this.doCloseX = true;
            this.closeOnClickedOutside = true;
        }

        private FadeBackgroundWindow backgroundWindow;

        public override void PreOpen()
        {
            base.PreOpen();
            backgroundWindow = new FadeBackgroundWindow();
            backgroundWindow.layer = WindowLayer.GameUI;
            Find.WindowStack.Add(backgroundWindow);
            DefsOf.WB_Narrative.PlayOneShotOnCamera();
        }

        public override float Margin => 0;

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.DrawBoxSolid(inRect, new ColorInt(94, 85, 72).ToColor);
            Text.Font = GameFont.Medium;
            var closeRect = inRect.ContractedBy(5);
            closeRect.y += 3;
            if (windowDrawing.DoClostButtonSmall(closeRect))
            {
                Close();
            }

            float titleHeight = 24;
            Rect titleRect = new Rect(inRect.x, inRect.y + 10, inRect.width, titleHeight);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(titleRect, title);
            Text.Anchor = TextAnchor.UpperLeft;

            Rect narrativeAreaRect = new Rect(
                inRect.x + 15f,
                titleRect.yMax + 5f,
                inRect.width - 30f,
                inRect.height - titleHeight - 30f
            );

            Widgets.DrawWindowBackground(narrativeAreaRect);

            Widgets.LabelScrollable(narrativeAreaRect.ContractedBy(15), narrativeText, ref scrollPosition);
            Text.Font = GameFont.Small;
        }
        public override void PostClose()
        {
            base.PostClose();
            if (backgroundWindow != null)
            {
                backgroundWindow.Close();
                backgroundWindow = null;
            }
        }
        private Vector2 scrollPosition = Vector2.zero;
    }
}
