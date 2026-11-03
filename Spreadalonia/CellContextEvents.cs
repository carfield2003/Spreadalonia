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

using Avalonia.Input;
using System;
using System.Collections.Generic;

namespace Spreadalonia
{
    /// <summary>
    /// The data of the <see cref="Spreadsheet.CellRightTapped"/> event: the cell that was right
    /// clicked, the modifiers that were held, and whether the host has already handled the click.
    /// </summary>
    public class CellRightTappedEventArgs : EventArgs
    {
        /// <summary>
        /// The column of the cell that was right clicked (already corrected for a merged range, so
        /// it is the anchor cell of the merge).
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// The row of the cell that was right clicked (already corrected for a merged range).
        /// </summary>
        public int Row { get; }

        /// <summary>
        /// The modifier keys that were held when the right button was pressed.
        /// </summary>
        public KeyModifiers Modifiers { get; }

        /// <summary>
        /// Set to <see langword="true"/> to tell the control that the host has handled the click: the
        /// built-in context menu is then not opened, which is how a host that provides its own menu
        /// takes over.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// Creates a new <see cref="CellRightTappedEventArgs"/>.
        /// </summary>
        /// <param name="column">The column of the cell.</param>
        /// <param name="row">The row of the cell.</param>
        /// <param name="modifiers">The modifiers held when the right button was pressed.</param>
        public CellRightTappedEventArgs(int column, int row, KeyModifiers modifiers)
        {
            this.Column = column;
            this.Row = row;
            this.Modifiers = modifiers;
        }
    }

    /// <summary>
    /// The data of the <see cref="Spreadsheet.ContextMenuOpening"/> event: it lets the host cancel the
    /// built-in menu of the spreadsheet and / or add its own entries to it.
    /// </summary>
    public class ContextMenuOpeningEventArgs : EventArgs
    {
        /// <summary>
        /// The first column of the selection, so that the host can enable or disable its entries
        /// according to where the menu is being opened.
        /// </summary>
        public int Left { get; }

        /// <summary>
        /// The first row of the selection.
        /// </summary>
        public int Top { get; }

        /// <summary>
        /// Set to <see langword="true"/> to prevent the built-in menu from being shown at all.
        /// </summary>
        public bool Cancel { get; set; }

        /// <summary>
        /// Entries the host wants to append to the built-in menu. They are added just before the menu
        /// is shown and removed again when it closes, so the same host item is never added twice.
        /// The control does not take ownership of them.
        /// </summary>
        public IList<object> ExtraItems { get; }

        /// <summary>
        /// Creates a new <see cref="ContextMenuOpeningEventArgs"/>.
        /// </summary>
        /// <param name="left">The first column of the selection.</param>
        /// <param name="top">The first row of the selection.</param>
        public ContextMenuOpeningEventArgs(int left, int top)
        {
            this.Left = left;
            this.Top = top;
            this.ExtraItems = new List<object>();
        }
    }
}
