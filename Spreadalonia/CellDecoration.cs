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

namespace Spreadalonia
{
    /// <summary>
    /// Lets a host draw on top of a cell, after its text has been painted and before its border and
    /// the selection frame are drawn. A typical use is a marker in the corner of a formula cell or a
    /// highlight on the cells that differ from a stored version.
    /// </summary>
    public interface ICellDecoration
    {
        /// <summary>
        /// Draws the decoration of one cell. The drawing context is already translated so that it
        /// works in the coordinates of the drawn region, and clipped to the cell.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <param name="info">The description of the cell that is being drawn.</param>
        void Draw(DrawingContext context, CellDrawInfo info);
    }

    /// <summary>
    /// Describes the cell that is being drawn, for an <see cref="ICellDecoration"/>.
    /// </summary>
    public class CellDrawInfo
    {
        /// <summary>The column of the cell.</summary>
        public int Column { get; internal set; }

        /// <summary>The row of the cell.</summary>
        public int Row { get; internal set; }

        /// <summary>
        /// The whole cell rectangle (margins included, alignment not applied), in the coordinates of
        /// the drawn region.
        /// </summary>
        public Rect Bounds { get; internal set; }

        /// <summary>
        /// The rectangle the text is actually laid out in (the cell minus its margins, and minus the
        /// indent), in the coordinates of the drawn region.
        /// </summary>
        public Rect ContentBounds { get; internal set; }

        /// <summary>The text that is drawn, after number formatting, truncation or wrapping.</summary>
        public string DisplayText { get; internal set; }

        /// <summary>The raw value stored in the cell (the formula text for a formula cell).</summary>
        public string RawText { get; internal set; }

        /// <summary>The typeface used to draw the cell.</summary>
        public Typeface Typeface { get; internal set; }

        /// <summary>The font size used to draw the cell, in device-independent pixels.</summary>
        public double FontSize { get; internal set; }

        /// <summary>The brush used to draw the text.</summary>
        public IBrush Foreground { get; internal set; }

        /// <summary>Whether the cell is the anchor of a merged range.</summary>
        public bool IsMerged { get; internal set; }

        /// <summary>Whether the cell holds a formula.</summary>
        public bool IsFormula { get; internal set; }
    }
}
