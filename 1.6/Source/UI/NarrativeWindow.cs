using UnityEngine;
using Verse;
using Verse.Sound;

namespace Worldbuilder
{
    [HotSwappable]
    public class NarrativeWindow : Window
    {
        private readonly string title;
        private readonly string narrativeText;
        private readonly int textLength;
        private readonly Texture2D coverIcon;
        private readonly Color coverColor;
        private readonly Thing coverThing;

        private Vector2 scrollPosition = Vector2.zero;
        private FadeBackgroundWindow backgroundWindow;

        private float openedAt;
        private float charsPerSecond;
        private int revealedChars;
        private bool revealComplete;
        private bool followReveal = true;

        private const float MinCharsPerSecond = 300f;
        private const float MaxRevealSeconds = 4f;
        private const float StartDelay = 0.2f;

        private const float FramePadding = 14f;
        private const float TextMargin = 30f;
        private const float IconSize = 34f;
        private const float TitleHeight = 38f;
        private const float FooterHeight = 20f;
        private const float MaxColumnWidth = 580f;

        private static readonly Color FrameColor = new Color(0.13f, 0.11f, 0.09f);
        private static readonly Color PageColor = new Color(0.85f, 0.79f, 0.66f);
        private static readonly Color RuleColor = new Color(0.45f, 0.38f, 0.28f);
        private static readonly Color InkColor = new Color(0.16f, 0.12f, 0.09f);
        private static readonly Color InkFaded = new Color(0.36f, 0.30f, 0.23f);

        public override Vector2 InitialSize => new Vector2(700f, Mathf.Min(780f, UI.screenHeight * 0.86f));
        public override float Margin => 0f;

        public NarrativeWindow(string title, string narrativeText, Texture2D coverIcon = null, Color? coverColor = null, Thing coverThing = null)
        {
            this.title = title;
            this.narrativeText = narrativeText ?? "";
            this.textLength = this.narrativeText.Length;
            this.coverIcon = coverIcon;
            this.coverColor = coverColor ?? Color.white;
            this.coverThing = coverThing;

            this.doCloseX = false;
            this.closeOnClickedOutside = true;
            this.absorbInputAroundWindow = true;
            this.drawShadow = false;
            this.doWindowBackground = false;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            backgroundWindow = new FadeBackgroundWindow();
            backgroundWindow.layer = WindowLayer.GameUI;
            Find.WindowStack.Add(backgroundWindow);
            DefsOf.WB_Narrative.PlayOneShotOnCamera();

            openedAt = Time.realtimeSinceStartup;
            charsPerSecond = Mathf.Max(MinCharsPerSecond, textLength / MaxRevealSeconds);
            revealedChars = 0;
            revealComplete = textLength == 0;
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

        public override void DoWindowContents(Rect inRect)
        {
            AdvanceReveal();

            Widgets.DrawBoxSolid(inRect, FrameColor);
            var page = inRect.ContractedBy(FramePadding);
            Widgets.DrawBoxSolid(page, PageColor);

            var content = page.ContractedBy(TextMargin);
            float y = content.y + 4f;

            if (coverThing != null)
            {
                Widgets.ThingIcon(new Rect(content.center.x - IconSize / 2f, y, IconSize, IconSize), coverThing);
                y += IconSize + 6f;
            }
            else if (coverIcon != null)
            {
                StoryCoverUtility.DrawCoverIcon(new Rect(content.center.x - IconSize / 2f, y, IconSize, IconSize), coverIcon, coverColor, 1f);
                y += IconSize + 6f;
            }

            DrawTitle(new Rect(content.x, y, content.width, TitleHeight));
            y += TitleHeight;

            Widgets.DrawBoxSolid(new Rect(content.center.x - 110f, y, 220f, 1f), RuleColor);
            y += 12f;

            var footerRect = new Rect(content.x, content.yMax - FooterHeight, content.width, FooterHeight);
            DrawBody(new Rect(content.x, y, content.width, footerRect.y - y - 10f));
            DrawFooter(footerRect);
            DrawCloseButton(inRect);

            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private void AdvanceReveal()
        {
            if (revealComplete) return;

            revealedChars = Mathf.FloorToInt((Time.realtimeSinceStartup - openedAt - StartDelay) * charsPerSecond);
            if (revealedChars >= textLength)
            {
                revealedChars = textLength;
                revealComplete = true;
            }
            else if (revealedChars < 0)
            {
                revealedChars = 0;
            }
        }

        private void DrawTitle(Rect rect)
        {
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = InkColor;
            Widgets.Label(rect, title);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private void DrawBody(Rect rect)
        {
            float columnWidth = Mathf.Min(rect.width, MaxColumnWidth);
            var column = new Rect(rect.center.x - columnWidth / 2f, rect.y, columnWidth, rect.height);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;

            float viewWidth = column.width - 20f;
            float fullHeight = Mathf.Max(Text.CalcHeight(narrativeText, viewWidth), column.height);
            var viewRect = new Rect(0f, 0f, viewWidth, fullHeight);

            Widgets.BeginScrollView(column, ref scrollPosition, viewRect);

            string visible = revealComplete ? narrativeText : SafeSubstring(narrativeText, revealedChars);

            GUI.color = InkColor;
            Widgets.Label(new Rect(0f, 0f, viewWidth, fullHeight), visible);
            GUI.color = Color.white;

            Widgets.EndScrollView();

            if (!revealComplete && followReveal)
            {
                scrollPosition.y = Mathf.Max(0f, Text.CalcHeight(visible + "|", viewWidth) - column.height + 24f);
            }

            if (Mouse.IsOver(column) && Event.current.type == EventType.ScrollWheel)
            {
                followReveal = false;
            }

            if (!revealComplete && Widgets.ButtonInvisible(column, doMouseoverSound: false))
            {
                CompleteReveal();
            }
        }

        private void CompleteReveal()
        {
            revealedChars = textLength;
            revealComplete = true;
            followReveal = false;
            scrollPosition.y = 0f;
        }

        private void DrawFooter(Rect rect)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = InkFaded;
            Widgets.Label(rect, revealComplete ? "WB_NarrativeFooter".Translate() : "WB_NarrativeSkipHint".Translate());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }

        private void DrawCloseButton(Rect inRect)
        {
            var closeRect = new Rect(inRect.xMax - 28f, inRect.y + 7f, 20f, 20f);
            GUI.color = Mouse.IsOver(closeRect) ? Color.white : new Color(1f, 1f, 1f, 0.6f);
            if (Widgets.ButtonImage(closeRect, TexButton.CloseXSmall))
            {
                Close();
            }
            GUI.color = Color.white;
        }

        private static string SafeSubstring(string text, int count)
        {
            count = Mathf.Clamp(count, 0, text.Length);
            string slice = text.Substring(0, count);

            int openTag = slice.LastIndexOf('<');
            if (openTag >= 0 && slice.IndexOf('>', openTag) < 0)
            {
                slice = slice.Substring(0, openTag);
            }
            return slice;
        }
    }
}
