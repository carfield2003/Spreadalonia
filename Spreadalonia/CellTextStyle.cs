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

using System;

namespace Spreadalonia
{
    /// <summary>
    /// The text decorations that can be drawn on a cell (underline, strikethrough or both).
    /// </summary>
    [Flags]
    public enum CellTextDecoration
    {
        /// <summary>No decoration.</summary>
        None = 0,

        /// <summary>A line under the text.</summary>
        Underline = 1,

        /// <summary>A line through the text.</summary>
        Strikethrough = 2
    }

    /// <summary>
    /// The text layout options of a single cell: the decorations (underline / strikethrough), whether
    /// the text wraps onto several lines, and the horizontal indentation.
    /// <para>
    /// The three options live in one object on purpose: the cell-keyed collections of a worksheet are
    /// shifted, undone and re-done as a whole, and every one of them costs a set of row / column
    /// shift overloads plus an undo / redo stack pair. Keeping the text layout of a cell in a single
    /// entry means that adding an option never multiplies those.
    /// </para>
    /// <para>
    /// An entry that leaves every property at its default value draws exactly like a cell without an
    /// entry, so the object can be written and removed freely without changing the appearance of a
    /// sheet.
    /// </para>
    /// </summary>
    public class CellTextStyle
    {
        /// <summary>
        /// The decorations drawn over the text. <see cref="CellTextDecoration.None"/> (the default)
        /// draws nothing.
        /// </summary>
        public CellTextDecoration Decoration { get; set; } = CellTextDecoration.None;

        /// <summary>
        /// Whether the text wraps onto several lines when it is wider than the cell. The default
        /// (<see langword="false"/>) is the behaviour before this property existed: a text that does
        /// not fit is clipped (or truncated with an ellipsis, for numeric cells).
        /// </summary>
        public bool Wrap { get; set; }

        /// <summary>
        /// The horizontal indentation, in device-independent pixels. It only moves a left aligned
        /// text (and the clip rectangle of the cell); centred and right aligned texts are not
        /// affected, like in Excel. The default is zero.
        /// </summary>
        public double Indent { get; set; }

        /// <summary>
        /// Creates a new <see cref="CellTextStyle"/> that draws like a cell without an entry.
        /// </summary>
        public CellTextStyle()
        {
        }

        /// <summary>
        /// Creates a new <see cref="CellTextStyle"/> with the specified decoration.
        /// </summary>
        /// <param name="decoration">The decorations drawn over the text.</param>
        public CellTextStyle(CellTextDecoration decoration)
        {
            this.Decoration = decoration;
        }

        /// <summary>
        /// Creates a new <see cref="CellTextStyle"/> with the specified options.
        /// </summary>
        /// <param name="decoration">The decorations drawn over the text.</param>
        /// <param name="wrap">Whether the text wraps onto several lines.</param>
        /// <param name="indent">The horizontal indentation, in device-independent pixels.</param>
        public CellTextStyle(CellTextDecoration decoration, bool wrap, double indent)
        {
            this.Decoration = decoration;
            this.Wrap = wrap;
            this.Indent = indent;
        }

        /// <summary>
        /// Whether this style draws exactly like a cell without an entry.
        /// </summary>
        internal bool IsDefault => this.Decoration == CellTextDecoration.None && !this.Wrap && this.Indent <= 0;

        /// <summary>
        /// Returns a copy of this style, so that a snapshot taken for an undo step cannot be changed
        /// by a later edit of the same cell.
        /// </summary>
        internal CellTextStyle Clone()
        {
            return new CellTextStyle(this.Decoration, this.Wrap, this.Indent);
        }
    }
}
