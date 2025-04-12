using Verse;
using UnityEngine;

namespace Worldbuilder
{
    [HotSwappable]
    public class NarrativeWindow : Window
    {
        private string narrativeText;
        public override Vector2 InitialSize => new Vector2(700f, 600f);
        public NarrativeWindow(string narrativeText)
        {
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
        }

        public override float Margin => 0;

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.DrawBoxSolid(inRect, Color.grey);
            Text.Font = GameFont.Medium;
            if (windowDrawing.DoClostButtonSmall(inRect))
            {
                Close();
            }
            Rect textRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height).ContractedBy(30);
            textRect.x += 15;
            textRect.width -= 30f;
            Widgets.DrawWindowBackground(textRect);

            Widgets.LabelScrollable(textRect.ContractedBy(15), narrativeText, ref scrollPosition);
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