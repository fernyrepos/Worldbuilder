using System;
using UnityEngine;
using Verse;

namespace Worldbuilder
{
    internal sealed class DefPickerPresentation<T> where T : Def
    {
        internal readonly Func<T, string> labelGetter;
        internal readonly Func<T, string> tooltipGetter;
        internal readonly Func<T, Texture2D> iconGetter;
        internal readonly Func<T, Color> iconColorGetter;
        internal readonly Func<T, AcceptanceReport> acceptanceGetter;
        internal readonly bool iconAfterLabel;
        internal readonly bool showInfoCard;

        internal DefPickerPresentation(
            Func<T, string> labelGetter = null,
            Func<T, string> tooltipGetter = null,
            Func<T, Texture2D> iconGetter = null,
            Func<T, Color> iconColorGetter = null,
            Func<T, AcceptanceReport> acceptanceGetter = null,
            bool iconAfterLabel = false,
            bool showInfoCard = false)
        {
            this.labelGetter = labelGetter;
            this.tooltipGetter = tooltipGetter;
            this.iconGetter = iconGetter;
            this.iconColorGetter = iconColorGetter;
            this.acceptanceGetter = acceptanceGetter;
            this.iconAfterLabel = iconAfterLabel;
            this.showInfoCard = showInfoCard;
        }
    }
}
