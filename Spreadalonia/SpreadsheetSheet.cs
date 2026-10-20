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
using Spreadalonia.Formula;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Spreadalonia
{
    /// <summary>
    /// Holds the whole state of a single worksheet: its data, its formatting, its column / row
    /// geometry and its view state (selection, scroll offset, frozen and hidden rows / columns).
    /// A <see cref="Spreadsheet"/> owns a list of these objects and renders the one that is
    /// currently selected; the objects that are not current are kept in memory untouched, so
    /// switching between worksheets preserves every setting of each of them.
    /// </summary>
    /// <remarks>
    /// All the coordinate keys used by the collections of this class are (column, row), zero based,
    /// exactly like the pre-existing single-sheet API.
    /// </remarks>
    public sealed class SpreadsheetSheet
    {
        /// <summary>
        /// The <see cref="Spreadsheet"/> that owns this sheet, or <see langword="null"/> while the
        /// sheet has not been added to a spreadsheet yet.
        /// </summary>
        internal Spreadsheet Owner { get; set; }

        /// <summary>
        /// The name shown on the sheet tab. Names are unique within a spreadsheet: a name that is
        /// already taken is made unique by appending a counter (e.g. <c>Sheet1 (2)</c>).
        /// </summary>
        public string Name { get; internal set; } = "Sheet1";

        /// <summary>
        /// The position of this sheet within the sheet collection of its owner, or -1 when the sheet
        /// does not belong to a spreadsheet. The index always follows the order of the sheets of
        /// the source document, hidden sheets included, so that it can be used to look the sheet
        /// up again in the file it was imported from.
        /// </summary>
        public int Index { get; internal set; } = -1;

        /// <summary>
        /// A place for the host application to attach its own data to the sheet (e.g. the index of
        /// the worksheet in the original workbook it was imported from).
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Whether the sheet is hidden. A hidden sheet keeps its <see cref="Index"/> (so that the
        /// indices keep matching the source file) but by default it is not shown on the sheet tab
        /// strip.
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// Whether the whole sheet is read-only: the user cannot edit, paste into or clear its
        /// cells, while the host application can still write them programmatically.
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Whether the user can move the content of a selection by dragging its border. It is
        /// <see langword="true"/> by default; turn it off for the worksheets that are meant to be
        /// browsed only (e.g. an imported document that must not be rearranged by accident).
        /// </summary>
        public bool AllowDragMoveCells { get; set; } = true;

        /// <summary>
        /// Whether the text of a cell that is wider than its column is allowed to spill over the
        /// empty cells that follow it, like Excel does. It is <see langword="false"/> by default,
        /// so that a cell never paints over its neighbours.
        /// </summary>
        public bool AllowTextOverflow { get; set; } = false;

        /// <summary>
        /// How the selected range of the sheet is highlighted. <see cref="SheetSelectionStyle.Fill"/>
        /// (the default) paints a translucent fill inside the selection;
        /// <see cref="SheetSelectionStyle.FocusRect"/> draws the outline only.
        /// </summary>
        public SheetSelectionStyle SelectionStyle { get; set; } = SheetSelectionStyle.Fill;

        /// <summary>
        /// The data contained in the sheet, indexed by (column, row). Please do not change this
        /// directly, or you will mess up the Undo/Redo stack.
        /// </summary>
        public Dictionary<(int, int), string> Data { get; internal set; } = new Dictionary<(int, int), string>();

        /// <summary>
        /// The parsed and evaluated formula data of the cells that contain formulas, indexed by
        /// (column, row).
        /// </summary>
        internal Dictionary<(int, int), CellData> FormulaCells { get; set; } = new Dictionary<(int, int), CellData>();

        /// <summary>
        /// The formula engine used to parse and evaluate the formulas of this sheet. Each sheet has
        /// its own engine, so that the dependencies and the cached results of one sheet never leak
        /// into another one; the custom functions registered through
        /// <see cref="Spreadsheet.FormulaEngine"/> are shared by every sheet.
        /// </summary>
        public FormulaEngine FormulaEngine { get; internal set; }

        /// <summary>
        /// The height of each row, for the rows whose height differs from the default one.
        /// </summary>
        public Dictionary<int, double> RowHeights { get; internal set; } = new Dictionary<int, double>();

        /// <summary>
        /// The width of each column, for the columns whose width differs from the default one.
        /// </summary>
        public Dictionary<int, double> ColumnWidths { get; internal set; } = new Dictionary<int, double>();

        /// <summary>
        /// The typeface of each row, for the rows whose typeface differs from the default one.
        /// </summary>
        public Dictionary<int, Typeface> RowTypefaces { get; internal set; } = new Dictionary<int, Typeface>();

        /// <summary>
        /// The typeface of each column, for the columns whose typeface differs from the default one.
        /// </summary>
        public Dictionary<int, Typeface> ColumnTypefaces { get; internal set; } = new Dictionary<int, Typeface>();

        /// <summary>
        /// The typeface of each cell, indexed by (column, row).
        /// </summary>
        public Dictionary<(int, int), Typeface> CellTypefaces { get; internal set; } = new Dictionary<(int, int), Typeface>();

        /// <summary>
        /// The foreground brush of each row.
        /// </summary>
        public Dictionary<int, IBrush> RowForeground { get; internal set; } = new Dictionary<int, IBrush>();

        /// <summary>
        /// The foreground brush of each column.
        /// </summary>
        public Dictionary<int, IBrush> ColumnForeground { get; internal set; } = new Dictionary<int, IBrush>();

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
        /// The font size of each cell, indexed by (column, row). If a cell is not present in this
        /// dictionary, the font size of the spreadsheet is used.
        /// </summary>
        public Dictionary<(int, int), double> CellFontSize { get; internal set; } = new Dictionary<(int, int), double>();

        /// <summary>
        /// The background fills applied to ranges of cells.
        /// </summary>
        public List<CellBackground> CellBackgrounds { get; internal set; } = new List<CellBackground>();

        /// <summary>
        /// The border line segments drawn on top of the grid lines.
        /// </summary>
        public List<CellBorder> CellBorders { get; internal set; } = new List<CellBorder>();

        /// <summary>
        /// The custom title cells, indexed by (column, row).
        /// </summary>
        public Dictionary<(int, int), TitleCell> TitleCells { get; internal set; } = new Dictionary<(int, int), TitleCell>();

        /// <summary>
        /// The clickable link cells, indexed by (column, row).
        /// </summary>
        public Dictionary<(int, int), LinkCell> LinkCells { get; internal set; } = new Dictionary<(int, int), LinkCell>();

        /// <summary>
        /// The clickable button cells, indexed by (column, row).
        /// </summary>
        public Dictionary<(int, int), ButtonCell> ButtonCells { get; internal set; } = new Dictionary<(int, int), ButtonCell>();

        /// <summary>
        /// The account-tree cells, indexed by (column, row).
        /// </summary>
        public Dictionary<(int, int), TreeGradeCell> TreeCells { get; internal set; } = new Dictionary<(int, int), TreeGradeCell>();

        /// <summary>
        /// The numeric cells, indexed by (column, row).
        /// </summary>
        public Dictionary<(int, int), NumericCell> NumericCells { get; internal set; } = new Dictionary<(int, int), NumericCell>();

        /// <summary>
        /// The rows that are currently hidden (collapsed). While a row is hidden its height is
        /// zero, so it disappears from the sheet without renumbering the remaining rows.
        /// </summary>
        public HashSet<int> HiddenRows { get; } = new HashSet<int>();

        /// <summary>
        /// The columns that are currently hidden. While a column is hidden its width is zero, so it
        /// disappears from the sheet without renumbering the remaining columns.
        /// </summary>
        public HashSet<int> HiddenColumns { get; } = new HashSet<int>();

        /// <summary>
        /// The ranges that are read-only. Cells inside a read-only range cannot be edited, pasted
        /// into or cleared, but they can still be selected and copied.
        /// </summary>
        public List<SelectionRange> ReadonlyRanges { get; } = new List<SelectionRange>();

        /// <summary>
        /// The ranges of cells that are merged together.
        /// </summary>
        public ImmutableList<SelectionRange> MergedRanges { get; internal set; } = ImmutableList.Create<SelectionRange>();

        /// <summary>
        /// The number of columns that are frozen at the left edge of the sheet.
        /// </summary>
        public int FrozenColumnCount { get; internal set; }

        /// <summary>
        /// The number of rows that are frozen at the top edge of the sheet.
        /// </summary>
        public int FrozenRowCount { get; internal set; }

        /// <summary>
        /// The maximum number of columns of the sheet.
        /// </summary>
        public int MaxTableWidth { get; internal set; } = int.MaxValue - 2;

        /// <summary>
        /// The maximum number of rows of the sheet.
        /// </summary>
        public int MaxTableHeight { get; internal set; } = int.MaxValue - 2;

        /// <summary>
        /// The currently selected ranges of the sheet. It is restored when the user comes back to
        /// the sheet.
        /// </summary>
        public ImmutableList<SelectionRange> Selection { get; internal set; } = ImmutableList.Create<SelectionRange>();

        /// <summary>
        /// The scroll offset of the sheet. It is restored when the user comes back to the sheet.
        /// </summary>
        public Vector ScrollOffset { get; internal set; }

        /// <summary>
        /// Whether the sheet maintains <see cref="HiddenRows"/> itself when a
        /// <see cref="TreeGradeCell"/> toggle box is clicked. When this is <see langword="false"/>
        /// the <see cref="Spreadsheet.TreeExpand"/> event is raised but the host is responsible for
        /// calling <see cref="Spreadsheet.SetRowVisible(int, bool)"/>.
        /// </summary>
        public bool AutoManageRows { get; set; } = true;

        /// <summary>
        /// Creates a new, empty sheet.
        /// </summary>
        /// <param name="name">The name of the sheet.</param>
        internal SpreadsheetSheet(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Returns the name of the sheet.
        /// </summary>
        public override string ToString()
        {
            return this.Name;
        }
    }

    /// <summary>
    /// How the selected range of a <see cref="SpreadsheetSheet"/> is highlighted.
    /// </summary>
    public enum SheetSelectionStyle
    {
        /// <summary>
        /// The selection is painted with a translucent fill and a coloured outline, like Excel does.
        /// </summary>
        Fill,

        /// <summary>
        /// Only the outline of the selection is painted, like the focus rectangle of a list control.
        /// </summary>
        FocusRect
    }

    /// <summary>
    /// Provides the data for the worksheet events of the <see cref="Spreadsheet"/> control
    /// (a worksheet being inserted, removed, renamed or moved).
    /// </summary>
    public sealed class SheetEventArgs : EventArgs
    {
        /// <summary>
        /// The worksheet the event is about.
        /// </summary>
        public SpreadsheetSheet Sheet { get; }

        /// <summary>
        /// The index of the worksheet.
        /// </summary>
        public int Index { get; }

        internal SheetEventArgs(SpreadsheetSheet sheet, int index)
        {
            this.Sheet = sheet;
            this.Index = index;
        }
    }

    /// <summary>
    /// Provides the data for the <see cref="Spreadsheet.SheetRenamed"/> event, which is raised when
    /// a worksheet has been given another name.
    /// </summary>
    public sealed class SheetRenamedEventArgs : EventArgs
    {
        /// <summary>
        /// The worksheet that has been renamed.
        /// </summary>
        public SpreadsheetSheet Sheet { get; }

        /// <summary>
        /// The index of the worksheet.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The name the worksheet had before the change.
        /// </summary>
        public string OldName { get; }

        /// <summary>
        /// The name the worksheet has after the change.
        /// </summary>
        public string NewName { get; }

        internal SheetRenamedEventArgs(SpreadsheetSheet sheet, int index, string oldName, string newName)
        {
            this.Sheet = sheet;
            this.Index = index;
            this.OldName = oldName;
            this.NewName = newName;
        }
    }

    /// <summary>
    /// Provides the data for the <see cref="Spreadsheet.SheetMoved"/> event, which is raised when a
    /// worksheet has been moved to another position.
    /// </summary>
    public sealed class SheetMovedEventArgs : EventArgs
    {
        /// <summary>
        /// The worksheet that has been moved.
        /// </summary>
        public SpreadsheetSheet Sheet { get; }

        /// <summary>
        /// The position the worksheet had before the change.
        /// </summary>
        public int OldIndex { get; }

        /// <summary>
        /// The position the worksheet has after the change.
        /// </summary>
        public int NewIndex { get; }

        internal SheetMovedEventArgs(SpreadsheetSheet sheet, int oldIndex, int newIndex)
        {
            this.Sheet = sheet;
            this.OldIndex = oldIndex;
            this.NewIndex = newIndex;
        }
    }

    /// <summary>
    /// Provides the data for the <see cref="Spreadsheet.CurrentSheetChanged"/> event, which is
    /// raised when the worksheet that is being displayed changes.
    /// </summary>
    public sealed class CurrentSheetChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The worksheet that was being displayed before the change, or <see langword="null"/> if
        /// there was none.
        /// </summary>
        public SpreadsheetSheet OldSheet { get; }

        /// <summary>
        /// The worksheet that is being displayed after the change.
        /// </summary>
        public SpreadsheetSheet NewSheet { get; }

        /// <summary>
        /// The index of <see cref="OldSheet"/>, or -1 if there was none.
        /// </summary>
        public int OldIndex { get; }

        /// <summary>
        /// The index of <see cref="NewSheet"/>.
        /// </summary>
        public int NewIndex { get; }

        internal CurrentSheetChangedEventArgs(SpreadsheetSheet oldSheet, SpreadsheetSheet newSheet, int oldIndex, int newIndex)
        {
            this.OldSheet = oldSheet;
            this.NewSheet = newSheet;
            this.OldIndex = oldIndex;
            this.NewIndex = newIndex;
        }
    }
}
