using System;
using UnityEngine;

namespace Worldbuilder
{
    internal readonly struct ResponsiveOptionGrid
    {
        private const float MinimumColumnWidth = 280f;
        private const float ColumnGap = 6f;
        private const float ScrollBarWidth = 16f;

        private readonly float columnWidth;
        private readonly float rowHeight;

        private ResponsiveOptionGrid(
            Rect outRect,
            int itemCount,
            float rowHeight,
            float? contentHeight = null)
        {
            var viewWidth = Math.Max(
                1f,
                outRect.width - ScrollBarWidth);

            ColumnCount = Math.Max(
                1,
                Mathf.FloorToInt(
                    (viewWidth + ColumnGap) /
                    (MinimumColumnWidth + ColumnGap)));

            columnWidth = Math.Max(
                1f,
                (viewWidth - (ColumnCount - 1) * ColumnGap) /
                ColumnCount);

            this.rowHeight = rowHeight;

            var rowCount = itemCount <= 0
                ? 0
                : (itemCount + ColumnCount - 1) / ColumnCount;

            var viewHeight = Math.Max(
                Math.Max(1f, outRect.height),
                contentHeight ?? rowCount * rowHeight);

            ViewRect = new Rect(0f, 0f, viewWidth, viewHeight);
            MaximumScrollY = Math.Max(
                0f,
                viewHeight - Math.Max(0f, outRect.height));
        }

        internal int ColumnCount { get; }

        internal Rect ViewRect { get; }

        private float MaximumScrollY { get; }

        internal static ResponsiveOptionGrid Create(
            Rect outRect,
            int itemCount,
            float rowHeight)
        {
            return new ResponsiveOptionGrid(
                outRect,
                itemCount,
                rowHeight);
        }

        internal static ResponsiveOptionGrid CreateForContentHeight(
            Rect outRect,
            float contentHeight,
            float rowHeight)
        {
            return new ResponsiveOptionGrid(
                outRect,
                0,
                rowHeight,
                contentHeight);
        }

        internal Rect RowRect(int itemIndex)
        {
            var rowIndex = itemIndex / ColumnCount;
            var columnIndex = itemIndex % ColumnCount;

            return new Rect(
                columnIndex * (columnWidth + ColumnGap),
                rowIndex * rowHeight,
                columnWidth,
                rowHeight - 2f);
        }

        internal bool IsAlternatingRow(int itemIndex)
        {
            return itemIndex / ColumnCount % 2 == 1;
        }

        internal float RowsHeight(int itemCount)
        {
            var rowCount = itemCount <= 0
                ? 0
                : (itemCount + ColumnCount - 1) / ColumnCount;
            return rowCount * rowHeight;
        }

        internal void ClampScrollPosition(ref Vector2 scrollPosition)
        {
            scrollPosition.x = 0f;
            scrollPosition.y = Math.Max(
                0f,
                Math.Min(scrollPosition.y, MaximumScrollY));
        }
    }
}
