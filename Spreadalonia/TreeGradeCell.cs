/*
    Spreadalonia - A spreadsheet control for Avalonia
    Copyright (C) 2023  Giorgio Bianchini, University of Bristol

    This library is free software; you can redistribute it and/or
    modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    This library is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
    Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with this library; if not, see <https://www.gnu.org/licenses/>.
*/

using Avalonia;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
using System.Globalization;

namespace Spreadalonia
{
    /// <summary>
    /// A cell body that renders an expandable / collapsible account-tree node: an indented caption,
    /// a dotted guide line for every ancestor level and a +/- toggle box for nodes that have
    /// children.
    /// </summary>
    public class TreeGradeCell
    {
        /// <summary>Length of the strokes that make up the +/- sign.</summary>
        internal const double LineLength = 6;

        /// <summary>Side of the square toggle box.</summary>
        internal const double RectLength = 11;

        /// <summary>Indentation added by every level.</summary>
        internal const double MarginUnit = 4;

        private static readonly DashStyle DotDash = new DashStyle(new double[] { 0, 2 }, 0);

        /// <summary>
        /// The text displayed in the cell (typically the account code, optionally followed by the name).
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// The depth of the node, starting at 1 (corresponds to the WPF <c>grade</c>).
        /// </summary>
        public int Level { get; set; } = 1;

        /// <summary>
        /// Whether the node is a leaf: no +/- box is drawn, only the connector line.
        /// </summary>
        public bool IsLeaf { get; set; }

        /// <summary>
        /// Whether this node is the last one of its level. When set, the guide lines are only
        /// drawn down to the vertical middle of the row. Only used when no
        /// <c>hasNextSiblingAtLevel</c> callback is supplied to <see cref="Render"/>; otherwise
        /// the callback decides where the lines turn into corners.
        /// </summary>
        public bool IsLastNode { get; set; }

        /// <summary>
        /// Whether the node is currently expanded.
        /// </summary>
        public bool IsExpanded { get; set; } = true;

        /// <summary>
        /// The background of the whole row (parent rows <c>#D9FFF5</c>, leaf rows <c>#B9F5D8</c> in
        /// the reference application).
        /// </summary>
        public IBrush BackColor { get; set; }

        /// <summary>
        /// The brush used to draw the text. Defaults to black.
        /// </summary>
        public IBrush ForeColor { get; set; }

        /// <summary>
        /// The brush used to draw the dotted guide lines. Defaults to black.
        /// </summary>
        public IBrush LineColor { get; set; }

        /// <summary>
        /// The pen used to draw the +/- box. Defaults to a 1 pixel black pen.
        /// </summary>
        public Pen BoxPen { get; set; }

        /// <summary>
        /// The absolute (unscrolled) bounds of the +/- box, updated every time the cell is drawn.
        /// Used for hit testing.
        /// </summary>
        internal Rect BoxHitRect { get; set; }

        /// <summary>
        /// Whether the cell draws a +/- box (i.e. it is not a leaf).
        /// </summary>
        internal bool HasBox => !IsLeaf;

        /// <summary>
        /// The horizontal offset of the toggle box, relative to the left edge of the cell.
        /// </summary>
        internal double BoxOffset => Level * MarginUnit + (Level - 1) * RectLength;

        /// <summary>
        /// The horizontal offset of the text, relative to the left edge of the cell.
        /// </summary>
        internal double TextOffset => (Level + 1) * MarginUnit + Level * RectLength;

        /// <summary>
        /// Creates a new empty <see cref="TreeGradeCell"/> at level 1.
        /// </summary>
        public TreeGradeCell()
        {
        }

        /// <summary>
        /// Creates a new <see cref="TreeGradeCell"/>.
        /// </summary>
        /// <param name="text">The text displayed in the cell.</param>
        /// <param name="level">The depth of the node, starting at 1.</param>
        /// <param name="isLeaf">Whether the node is a leaf.</param>
        public TreeGradeCell(string text, int level, bool isLeaf) : this(text, level, isLeaf, false)
        {
        }

        /// <summary>
        /// Creates a new <see cref="TreeGradeCell"/>.
        /// </summary>
        /// <param name="text">The text displayed in the cell.</param>
        /// <param name="level">The depth of the node, starting at 1.</param>
        /// <param name="isLeaf">Whether the node is a leaf.</param>
        /// <param name="isLastNode">Whether this node is the last one of its level.</param>
        public TreeGradeCell(string text, int level, bool isLeaf, bool isLastNode)
        {
            Text = text;
            Level = level < 1 ? 1 : level;
            IsLeaf = isLeaf;
            IsLastNode = isLastNode;
        }

        /// <summary>
        /// Renders the tree cell into the specified drawing context and updates the internal
        /// hit-test rectangle of the +/- box.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <param name="bounds">The bounds of the cell.</param>
        /// <param name="rowWidth">The width of the whole (visible part of the) row, used to paint <see cref="BackColor"/>.</param>
        /// <param name="visual">The visual used to snap coordinates to device pixels.</param>
        /// <param name="typeface">The typeface used to draw the text.</param>
        /// <param name="fontSize">The font size used to draw the text.</param>
        /// <param name="defaultForeground">The brush used when <see cref="ForeColor"/> is <see langword="null"/>.</param>
        /// <param name="hasNextSiblingAtLevel">
        /// Returns, for a guide-line level, whether that line continues below this row (i.e. a
        /// further direct child at the next level follows). May be <see langword="null"/>, in
        /// which case <see cref="IsLastNode"/> alone decides whether the guide lines are drawn
        /// full height.
        /// </param>
        internal void Render(DrawingContext context, Rect bounds, double rowWidth, Visual visual, Typeface typeface, double fontSize, IBrush defaultForeground, Func<int, bool> hasNextSiblingAtLevel)
        {
            double height = bounds.Height;
            double width = bounds.Width;

            if (height <= 0 || width <= 0)
            {
                BoxHitRect = default;
                return;
            }

            if (BackColor != null)
            {
                // The reference application paints the whole row, not just the tree column.
                context.FillRectangle(BackColor, new Rect(0, bounds.Y, Math.Max(rowWidth, width), height));
            }

            Pen dotPen = new Pen(LineColor ?? Brushes.Black, 1, DotDash, PenLineCap.Round);
            Pen boxPen = BoxPen ?? new Pen(Brushes.Black, 1);

            double mid = height * 0.5;

            for (int i = 1; i <= Level; i++)
            {
                double xi = i * MarginUnit + (i - 1) * RectLength;
                double lineX = bounds.X + xi + 5;

                if (i < Level)
                {
                    // Ancestor guide line: it runs the full height of the row while further
                    // direct children (level i + 1) follow below. It turns into a corner in the
                    // middle of the last child's row and is not drawn at all on rows that sit
                    // deeper than i + 1 past the last child.
                    bool hasNext = hasNextSiblingAtLevel != null ? hasNextSiblingAtLevel(i) : !IsLastNode;

                    if (hasNext)
                    {
                        context.DrawLine(dotPen, new Point(lineX, bounds.Y).SnapToDevicePixels(visual), new Point(lineX, bounds.Y + height).SnapToDevicePixels(visual));
                    }
                    else if (Level == i + 1)
                    {
                        context.DrawLine(dotPen, new Point(lineX, bounds.Y).SnapToDevicePixels(visual), new Point(lineX, bounds.Y + mid).SnapToDevicePixels(visual));
                    }
                }
                else if (IsLeaf)
                {
                    // A leaf only gets the horizontal connector coming from its parent; it never
                    // draws a vertical line of its own. The connector runs up to the caption so
                    // that only a small gap remains before the text, matching the reference
                    // look.
                    if (Level > 1)
                    {
                        double prevX = bounds.X + (i - 1) * MarginUnit + (i - 2) * RectLength + 5;
                        double endX = bounds.X + TextOffset - MarginUnit;
                        context.DrawLine(dotPen, new Point(prevX, bounds.Y + mid).SnapToDevicePixels(visual), new Point(endX, bounds.Y + mid).SnapToDevicePixels(visual));
                    }
                }
                else
                {
                    double boxTop = bounds.Y + (height - RectLength) * 0.5;

                    if (Level > 1)
                    {
                        // Elbow from the parent guide line to the box. No vertical stub is
                        // drawn above the box.
                        double prevX = bounds.X + (i - 1) * MarginUnit + (i - 2) * RectLength + 5;
                        context.DrawLine(dotPen, new Point(prevX, bounds.Y + mid).SnapToDevicePixels(visual), new Point(bounds.X + xi, bounds.Y + mid).SnapToDevicePixels(visual));
                    }

                    // Below the box the line only continues when the node is expanded ("-") and
                    // further visible rows of its subtree follow, so that it joins the guide
                    // lines of the child rows below. A collapsed ("+") node draws nothing below
                    // the box.
                    bool hasChildrenBelow = hasNextSiblingAtLevel != null
                        ? (IsExpanded && hasNextSiblingAtLevel(i))
                        : (IsExpanded && !IsLastNode);

                    if (hasChildrenBelow)
                    {
                        context.DrawLine(dotPen, new Point(lineX, boxTop + RectLength).SnapToDevicePixels(visual), new Point(lineX, bounds.Y + height).SnapToDevicePixels(visual));
                    }
                }
            }

            if (HasBox)
            {
                double boxX = bounds.X + BoxOffset;
                double boxY = bounds.Y + (height - RectLength) * 0.5;

                BoxHitRect = new Rect(boxX, boxY, RectLength, RectLength);

                Rect boxRect = new Rect(new Point(boxX, boxY).SnapToDevicePixels(visual), new Point(boxX + RectLength, boxY + RectLength).SnapToDevicePixels(visual));

                context.DrawRectangle(Brushes.White, boxPen, boxRect);

                // The horizontal bar is always drawn; the vertical bar turns "-" into "+".
                context.DrawLine(boxPen,
                    new Point(boxX + 2, bounds.Y + mid).SnapToDevicePixels(visual),
                    new Point(boxX + 2 + LineLength, bounds.Y + mid).SnapToDevicePixels(visual));

                if (!IsExpanded)
                {
                    double vTop = bounds.Y + (height - LineLength) * 0.5;

                    context.DrawLine(boxPen,
                        new Point(boxX + 5, vTop).SnapToDevicePixels(visual),
                        new Point(boxX + 5, vTop + LineLength).SnapToDevicePixels(visual));
                }
            }
            else
            {
                BoxHitRect = default;
            }

            string text = Text;

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            try
            {
                Typeface safeFace = typeface;

                if (safeFace.FontFamily == null)
                {
                    safeFace = new Typeface(FontFamily.Default, safeFace.Style, safeFace.Weight);
                }

                FormattedText fmtText = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, safeFace, fontSize, ForeColor ?? defaultForeground ?? Brushes.Black);

                double textX = bounds.X + TextOffset;
                double textWidth = Math.Max(0, width - TextOffset);
                double textY = bounds.Y + (height - fmtText.Height) * 0.5;

                using (context.PushClip(new Rect(textX, bounds.Y, textWidth, height)))
                {
                    context.DrawText(fmtText, new Point(textX, textY));
                }
            }
            catch
            {
                // An unresolvable font must never break rendering.
            }
        }
    }

    /// <summary>
    /// Carries the details of a +/- box click on a <see cref="TreeGradeCell"/>.
    /// </summary>
    public class TreeCellExpandedEventArgs : EventArgs
    {
        /// <summary>
        /// The column of the tree cell.
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// The row of the tree cell.
        /// </summary>
        public int Row { get; }

        /// <summary>
        /// The tree cell that was toggled.
        /// </summary>
        public TreeGradeCell Cell { get; }

        /// <summary>
        /// The expansion state after the toggle (<see langword="true"/> = expanded).
        /// </summary>
        public bool IsExpanded { get; }

        /// <summary>
        /// Creates a new <see cref="TreeCellExpandedEventArgs"/>.
        /// </summary>
        /// <param name="column">The column of the tree cell.</param>
        /// <param name="row">The row of the tree cell.</param>
        /// <param name="cell">The tree cell that was toggled.</param>
        /// <param name="isExpanded">The expansion state after the toggle.</param>
        public TreeCellExpandedEventArgs(int column, int row, TreeGradeCell cell, bool isExpanded)
        {
            Column = column;
            Row = row;
            Cell = cell;
            IsExpanded = isExpanded;
        }
    }
}
