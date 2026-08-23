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
        internal readonly Func<T, string> groupKeyGetter;
        internal readonly Func<T, string> groupLabelGetter;
        internal readonly Func<T, int> groupOrderGetter;
        internal readonly Func<T, Texture2D> groupIconGetter;
        internal readonly string groupFilterLabel;
        internal readonly string allGroupsLabel;
        internal readonly bool useGroupFilter;
        internal readonly bool iconAfterLabel;
        internal readonly bool showInfoCard;

        internal DefPickerPresentation(
            Func<T, string> labelGetter = null,
            Func<T, string> tooltipGetter = null,
            Func<T, Texture2D> iconGetter = null,
            Func<T, Color> iconColorGetter = null,
            Func<T, AcceptanceReport> acceptanceGetter = null,
            Func<T, string> groupKeyGetter = null,
            Func<T, string> groupLabelGetter = null,
            Func<T, int> groupOrderGetter = null,
            Func<T, Texture2D> groupIconGetter = null,
            string groupFilterLabel = null,
            string allGroupsLabel = null,
            bool useGroupFilter = false,
            bool iconAfterLabel = false,
            bool showInfoCard = false)
        {
            this.labelGetter = labelGetter;
            this.tooltipGetter = tooltipGetter;
            this.iconGetter = iconGetter;
            this.iconColorGetter = iconColorGetter;
            this.acceptanceGetter = acceptanceGetter;
            this.groupKeyGetter = groupKeyGetter;
            this.groupLabelGetter = groupLabelGetter;
            this.groupOrderGetter = groupOrderGetter;
            this.groupIconGetter = groupIconGetter;
            this.groupFilterLabel = groupFilterLabel;
            this.allGroupsLabel = allGroupsLabel;
            this.useGroupFilter = useGroupFilter;
            this.iconAfterLabel = iconAfterLabel;
            this.showInfoCard = showInfoCard;
        }
    }
}
