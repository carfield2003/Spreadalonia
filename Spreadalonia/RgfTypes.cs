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
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace Spreadalonia
{
    /// <summary>
    /// The sides of a border, stored as a set of flags.
    /// </summary>
    [Flags]
    internal enum BorderSides
    {
        None = 0,
        Left = 1,
        Right = 2,
        Top = 4,
        Bottom = 8,
        LeftRight = Left | Right,
        TopBottom = Top | Bottom,
        All = Left | Right | Top | Bottom
    }

    /// <summary>
    /// Describes a rectangular background that is applied to a range of cells.
    /// </summary>
    public class CellBackground
    {
        /// <summary>
        /// The range of cells the background is applied to.
        /// </summary>
        public SelectionRange Range { get; set; }

        /// <summary>
        /// The brush used to fill the background.
        /// </summary>
        public IBrush Brush { get; set; }

        /// <summary>
        /// Creates a new <see cref="CellBackground"/>.
        /// </summary>
        public CellBackground() { }

        /// <summary>
        /// Creates a new <see cref="CellBackground"/> covering the specified range.
        /// </summary>
        /// <param name="range">The range of cells covered by the background.</param>
        /// <param name="brush">The brush used to fill the background.</param>
        public CellBackground(SelectionRange range, IBrush brush)
        {
            this.Range = range;
            this.Brush = brush;
        }
    }

    /// <summary>
    /// Describes a single border line segment of a table.
    /// <para>
    /// The model follows the v-borders/h-borders layout used by RGF documents:
    /// vertical borders (v-borders) describe column separator lines, while
    /// horizontal borders (h-borders) describe row separator lines.
    /// </para>
    /// </summary>
    public class CellBorder
    {
        /// <summary>
        /// <see langword="true"/> for a horizontal border (h-border, a row separator line);
        /// <see langword="false"/> for a vertical border (v-border, a column separator line).
        /// </summary>
        public bool IsHorizontal { get; set; }

        /// <summary>
        /// The separator index of the border.
        /// For a vertical border this is the column separator index (0 = left of column 0,
        /// 1 = between column 0 and column 1, etc.).
        /// For a horizontal border this is the row separator index (0 = above row 0, etc.).
        /// </summary>
        public int LineIndex { get; set; }

        /// <summary>
        /// The first cell index covered by the border segment.
        /// For a vertical border this is the first row; for a horizontal border this is the first column.
        /// </summary>
        public int From { get; set; }

        /// <summary>
        /// The last cell index covered by the border segment (inclusive).
        /// For a vertical border this is the last row; for a horizontal border this is the last column.
        /// </summary>
        public int To { get; set; }

        /// <summary>
        /// The brush used to draw the border.
        /// </summary>
        public IBrush Brush { get; set; }

        /// <summary>
        /// The thickness of the border, in pixels.
        /// </summary>
        public double Thickness { get; set; }

        /// <summary>
        /// The dash style used to draw the border, or <see langword="null"/> for a solid line.
        /// <para>For example <c>new DashStyle(new double[] { 4, 2 }, 0)</c> draws a dashed line with
        /// 4-pixel dashes separated by 2-pixel gaps.</para>
        /// </summary>
        public IDashStyle? DashStyle { get; set; }

        /// <summary>
        /// Creates a new <see cref="CellBorder"/>.
        /// </summary>
        public CellBorder() { }

        /// <summary>
        /// Creates a new <see cref="CellBorder"/>.
        /// </summary>
        /// <param name="isHorizontal"><see langword="true"/> for a horizontal (h-border) line, <see langword="false"/> for a vertical (v-border) line.</param>
        /// <param name="lineIndex">The separator index of the border (see <see cref="LineIndex"/>).</param>
        /// <param name="from">The first cell index covered by the border segment.</param>
        /// <param name="to">The last cell index covered by the border segment.</param>
        /// <param name="brush">The brush used to draw the border.</param>
        /// <param name="thickness">The thickness of the border, in pixels.</param>
        public CellBorder(bool isHorizontal, int lineIndex, int from, int to, IBrush brush, double thickness)
            : this(isHorizontal, lineIndex, from, to, brush, thickness, null)
        {
        }

        /// <summary>
        /// Creates a new <see cref="CellBorder"/>.
        /// </summary>
        /// <param name="isHorizontal"><see langword="true"/> for a horizontal (h-border) line, <see langword="false"/> for a vertical (v-border) line.</param>
        /// <param name="lineIndex">The separator index of the border (see <see cref="LineIndex"/>).</param>
        /// <param name="from">The first cell index covered by the border segment.</param>
        /// <param name="to">The last cell index covered by the border segment.</param>
        /// <param name="brush">The brush used to draw the border.</param>
        /// <param name="thickness">The thickness of the border, in pixels.</param>
        /// <param name="dashStyle">The dash style, or <see langword="null"/> for a solid line.</param>
        public CellBorder(bool isHorizontal, int lineIndex, int from, int to, IBrush brush, double thickness, IDashStyle? dashStyle)
        {
            this.IsHorizontal = isHorizontal;
            this.LineIndex = lineIndex;
            this.From = from;
            this.To = to;
            this.Brush = brush;
            this.Thickness = thickness;
            this.DashStyle = dashStyle;
        }
    }

    /// <summary>
    /// The content of a parsed RGF (Report Generator Format) document.
    /// </summary>
    public class RgfData
    {
        /// <summary>
        /// The number of columns in the document.
        /// </summary>
        public int Width { get; internal set; }

        /// <summary>
        /// The number of rows in the document.
        /// </summary>
        public int Height { get; internal set; }

        /// <summary>
        /// The width of each column, indexed by column number.
        /// </summary>
        public Dictionary<int, double> ColumnWidths { get; internal set; } = new Dictionary<int, double>();

        /// <summary>
        /// The height of each row, indexed by row number.
        /// </summary>
        public Dictionary<int, double> RowHeights { get; internal set; } = new Dictionary<int, double>();

        /// <summary>
        /// The merged cell ranges found in the document.
        /// </summary>
        public List<SelectionRange> MergedRanges { get; internal set; } = new List<SelectionRange>();

        /// <summary>
        /// The cell backgrounds found in the document.
        /// </summary>
        public List<CellBackground> CellBackgrounds { get; internal set; } = new List<CellBackground>();

        /// <summary>
        /// The cell borders found in the document.
        /// </summary>
        public List<CellBorder> CellBorders { get; internal set; } = new List<CellBorder>();

        /// <summary>
        /// The cell values, indexed by (column, row).
        /// </summary>
        public Dictionary<(int, int), string> Data { get; internal set; } = new Dictionary<(int, int), string>();

        /// <summary>
        /// The typeface of each cell, indexed by (column, row).
        /// </summary>
        public Dictionary<(int, int), Typeface> CellTypefaces { get; internal set; } = new Dictionary<(int, int), Typeface>();

        /// <summary>
        /// The font size of each cell, indexed by (column, row).
        /// </summary>
        public Dictionary<(int, int), double> CellFontSize { get; internal set; } = new Dictionary<(int, int), double>();

        /// <summary>
        /// The foreground brush of each cell, indexed by (column, row).
        /// </summary>
        public Dictionary<(int, int), IBrush> CellForeground { get; internal set; } = new Dictionary<(int, int), IBrush>();

        /// <summary>
        /// The horizontal text alignment of each cell, indexed by (column, row).
        /// </summary>
        public Dictionary<(int, int), TextAlignment> CellTextAlignment { get; internal set; } = new Dictionary<(int, int), TextAlignment>();

        /// <summary>
        /// The vertical text alignment of each cell, indexed by (column, row).
        /// </summary>
        public Dictionary<(int, int), VerticalAlignment> CellVerticalAlignment { get; internal set; } = new Dictionary<(int, int), VerticalAlignment>();

        /// <summary>
        /// The margin of each cell, indexed by (column, row).
        /// </summary>
        public Dictionary<(int, int), Thickness> CellMargin { get; internal set; } = new Dictionary<(int, int), Thickness>();

        /// <summary>
        /// The custom title cells, indexed by (column, row). When a cell has a title cell, the
        /// title cell is rendered instead of the regular text content.
        /// </summary>
        public Dictionary<(int, int), TitleCell> TitleCells { get; internal set; } = new Dictionary<(int, int), TitleCell>();

        /// <summary>
        /// The clickable link cells, indexed by (column, row). When a cell has a link cell, the
        /// link text is rendered instead of the regular text content.
        /// </summary>
        public Dictionary<(int, int), LinkCell> LinkCells { get; internal set; } = new Dictionary<(int, int), LinkCell>();
    }

    /// <summary>
    /// An internal description of an RGF style, used during parsing.
    /// </summary>
    internal class RgfStyle
    {
        public string FontFamily;
        public double FontSize;
        public FontStyle? FontStyle;
        public FontWeight? FontWeight;
        public TextAlignment? Horizontal;
        public VerticalAlignment? Vertical;
        public IBrush Foreground;
        public IBrush Background;
    }
}
