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
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Spreadalonia.Formula;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Avalonia.Interactivity;

namespace Spreadalonia
{
    internal class Table : Control
    {
        public static readonly StyledProperty<Spreadsheet> ContainerProperty = AvaloniaProperty.Register<Table, Spreadsheet>(nameof(Container));
        public Spreadsheet Container
        {
            get { return GetValue(ContainerProperty); }
            set { SetValue(ContainerProperty, value); }
        }

        public static readonly StyledProperty<double> DefaultColumnWidthProperty = AvaloniaProperty.Register<Table, double>(nameof(DefaultColumnWidth), 65);
        public double DefaultColumnWidth
        {
            get { return GetValue(DefaultColumnWidthProperty); }
            set { SetValue(DefaultColumnWidthProperty, value); }
        }

        public static readonly StyledProperty<double> DefaultRowHeightProperty = AvaloniaProperty.Register<Table, double>(nameof(DefaultRowHeight), 23);
        public double DefaultRowHeight
        {
            get { return GetValue(DefaultRowHeightProperty); }
            set { SetValue(DefaultRowHeightProperty, value); }
        }

        public static readonly StyledProperty<Vector> OffsetProperty = AvaloniaProperty.Register<Table, Vector>(nameof(Offset), new Vector(0, 0));
        public Vector Offset
        {
            get { return GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }

        public static readonly StyledProperty<Color> GridColorProperty = AvaloniaProperty.Register<Table, Color>(nameof(GridColor), Color.FromRgb(220, 220, 220));
        public Color GridColor
        {
            get { return GetValue(GridColorProperty); }
            set { SetValue(GridColorProperty, value); }
        }

        public static readonly StyledProperty<ImmutableList<SelectionRange>> SelectionProperty = AvaloniaProperty.Register<Table, ImmutableList<SelectionRange>>(nameof(Selection), ImmutableList.Create<SelectionRange>());
        public ImmutableList<SelectionRange> Selection
        {
            get { return GetValue(SelectionProperty); }
            set { SetValue(SelectionProperty, value); }
        }

        public static readonly StyledProperty<IBrush> ForegroundProperty = UserControl.ForegroundProperty.AddOwner<Table>();
        public IBrush Foreground
        {
            get { return GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }

        public static readonly StyledProperty<SolidColorBrush> BackgroundProperty = AvaloniaProperty.Register<Table, SolidColorBrush>(nameof(Background), new SolidColorBrush(Colors.White));
        public SolidColorBrush Background
        {
            get { return GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        public static readonly StyledProperty<IBrush> SelectionAccentProperty = AvaloniaProperty.Register<Table, IBrush>(nameof(Selection), new SolidColorBrush(Color.FromRgb(0, 114, 176)));
        public IBrush SelectionAccent
        {
            get { return GetValue(SelectionAccentProperty); }
            set { SetValue(SelectionAccentProperty, value); }
        }

        public static readonly StyledProperty<FontFamily> FontFamilyProperty = UserControl.FontFamilyProperty.AddOwner<Table>();
        public FontFamily FontFamily
        {
            get { return GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        public static readonly StyledProperty<double> FontSizeProperty = UserControl.FontSizeProperty.AddOwner<Table>();
        public double FontSize
        {
            get { return GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public static readonly StyledProperty<FontStyle> FontStyleProperty = UserControl.FontStyleProperty.AddOwner<Table>();
        public FontStyle FontStyle
        {
            get { return GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        public static readonly StyledProperty<FontWeight> FontWeightProperty = UserControl.FontWeightProperty.AddOwner<Table>();
        public FontWeight FontWeight
        {
            get { return GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }


        /// <summary>
        /// The worksheet whose state is currently rendered by this table. The table itself holds no
        /// data: every state property forwards to this object, so that switching worksheet is a
        /// matter of replacing it (see <see cref="SetSheet(SpreadsheetSheet)"/>).
        /// </summary>
        internal SpreadsheetSheet Sheet { get; private set; } = new SpreadsheetSheet("Sheet1");

        /// <summary>
        /// Makes the specified worksheet the one that is rendered by this table. Every state
        /// property of the table forwards to the worksheet, so this is all that is needed to
        /// display another worksheet of the same spreadsheet.
        /// </summary>
        /// <param name="sheet">The worksheet to render.</param>
        internal void SetSheet(SpreadsheetSheet sheet)
        {
            this.Sheet = sheet ?? new SpreadsheetSheet("Sheet1");
            this.InvalidateVisual();
            this.InvalidateHeaders();
        }

        public int MaxTableHeight
        {
            get { return this.Sheet.MaxTableHeight; }
            set { this.Sheet.MaxTableHeight = value; this.InvalidateVisual(); }
        }

        public int MaxTableWidth
        {
            get { return this.Sheet.MaxTableWidth; }
            set { this.Sheet.MaxTableWidth = value; this.InvalidateVisual(); }
        }


        public Dictionary<(int, int), string> Data
        {
            get { return this.Sheet.Data; }
            internal set { this.Sheet.Data = value ?? new Dictionary<(int, int), string>(); }
        }

        /// <summary>
        /// Stores the parsed and evaluated formula data for cells that contain formulas.
        /// Key: (col, row), Value: CellData with formula info and cached value.
        /// </summary>
        public Dictionary<(int, int), CellData> FormulaCells
        {
            get { return this.Sheet.FormulaCells; }
            internal set { this.Sheet.FormulaCells = value ?? new Dictionary<(int, int), CellData>(); }
        }

        /// <summary>
        /// Reference to the formula engine for evaluation.
        /// </summary>
        public FormulaEngine FormulaEngine
        {
            get { return this.Sheet.FormulaEngine; }
            set { this.Sheet.FormulaEngine = value; }
        }

        public Dictionary<int, double> RowHeights
        {
            get { return this.Sheet.RowHeights; }
            internal set { this.Sheet.RowHeights = value ?? new Dictionary<int, double>(); }
        }

        public Dictionary<int, double> ColumnWidths
        {
            get { return this.Sheet.ColumnWidths; }
            internal set { this.Sheet.ColumnWidths = value ?? new Dictionary<int, double>(); }
        }

        public Dictionary<int, Typeface> RowTypefaces
        {
            get { return this.Sheet.RowTypefaces; }
            internal set { this.Sheet.RowTypefaces = value ?? new Dictionary<int, Typeface>(); }
        }

        public Dictionary<int, Typeface> ColumnTypefaces
        {
            get { return this.Sheet.ColumnTypefaces; }
            internal set { this.Sheet.ColumnTypefaces = value ?? new Dictionary<int, Typeface>(); }
        }

        public Dictionary<(int, int), Typeface> CellTypefaces
        {
            get { return this.Sheet.CellTypefaces; }
            internal set { this.Sheet.CellTypefaces = value ?? new Dictionary<(int, int), Typeface>(); }
        }

        public Dictionary<int, IBrush> RowForeground
        {
            get { return this.Sheet.RowForeground; }
            internal set { this.Sheet.RowForeground = value ?? new Dictionary<int, IBrush>(); }
        }

        public Dictionary<int, IBrush> ColumnForeground
        {
            get { return this.Sheet.ColumnForeground; }
            internal set { this.Sheet.ColumnForeground = value ?? new Dictionary<int, IBrush>(); }
        }

        public Dictionary<(int, int), IBrush> CellForeground
        {
            get { return this.Sheet.CellForeground; }
            internal set { this.Sheet.CellForeground = value ?? new Dictionary<(int, int), IBrush>(); }
        }

        public Dictionary<(int, int), TextAlignment> CellTextAlignment
        {
            get { return this.Sheet.CellTextAlignment; }
            internal set { this.Sheet.CellTextAlignment = value ?? new Dictionary<(int, int), TextAlignment>(); }
        }

        public Dictionary<(int, int), VerticalAlignment> CellVerticalAlignment
        {
            get { return this.Sheet.CellVerticalAlignment; }
            internal set { this.Sheet.CellVerticalAlignment = value ?? new Dictionary<(int, int), VerticalAlignment>(); }
        }

        public Dictionary<(int, int), Thickness> CellMargin
        {
            get { return this.Sheet.CellMargin; }
            internal set { this.Sheet.CellMargin = value ?? new Dictionary<(int, int), Thickness>(); }
        }

        /// <summary>
        /// The font size of each cell, indexed by (column, row). If a cell is not present in this
        /// dictionary, the <see cref="Table.FontSize"/> value is used.
        /// </summary>
        public Dictionary<(int, int), double> CellFontSize
        {
            get { return this.Sheet.CellFontSize; }
            internal set { this.Sheet.CellFontSize = value ?? new Dictionary<(int, int), double>(); }
        }

        /// <summary>
        /// The custom title cells, indexed by (column, row). When a cell has a title cell, the
        /// title cell is rendered instead of the regular text content. This is used for report
        /// captions that contain multiple formatted text chunks (e.g. a title and subtitle).
        /// </summary>
        public Dictionary<(int, int), TitleCell> TitleCells
        {
            get { return this.Sheet.TitleCells; }
            internal set { this.Sheet.TitleCells = value ?? new Dictionary<(int, int), TitleCell>(); }
        }

        /// <summary>
        /// The clickable link cells, indexed by (column, row). When a cell has a link cell, the
        /// link text is rendered instead of the regular text content, hovering over the link
        /// shows a hand cursor and clicking it raises the <see cref="Spreadsheet.LinkClick"/> event.
        /// </summary>
        public Dictionary<(int, int), LinkCell> LinkCells
        {
            get { return this.Sheet.LinkCells; }
            internal set { this.Sheet.LinkCells = value ?? new Dictionary<(int, int), LinkCell>(); }
        }

        /// <summary>
        /// The clickable button cells, indexed by (column, row). A button cell renders a button
        /// (or an underlined link) instead of the regular text content; hovering over it shows a
        /// hand cursor and clicking it raises the <see cref="Spreadsheet.CellTapped"/> event, which
        /// the host uses to open its own selection dialog (e.g. to pick an auxiliary accounting
        /// item) instead of editing the text.
        /// </summary>
        public Dictionary<(int, int), ButtonCell> ButtonCells
        {
            get { return this.Sheet.ButtonCells; }
            internal set { this.Sheet.ButtonCells = value ?? new Dictionary<(int, int), ButtonCell>(); }
        }

        /// <summary>
        /// The account-tree cells, indexed by (column, row). A tree cell renders an indented
        /// caption with dotted guide lines and a +/- toggle box; clicking the box raises the
        /// <see cref="Spreadsheet.TreeExpand"/> event and (in automatic mode) collapses or expands
        /// the rows below.
        /// </summary>
        public Dictionary<(int, int), TreeGradeCell> TreeCells
        {
            get { return this.Sheet.TreeCells; }
            internal set { this.Sheet.TreeCells = value ?? new Dictionary<(int, int), TreeGradeCell>(); }
        }

        /// <summary>
        /// The numeric cells, indexed by (column, row). A numeric cell renders its raw content
        /// formatted with thousand separators and a fixed number of decimal digits, and only
        /// accepts numeric input while editing.
        /// </summary>
        public Dictionary<(int, int), NumericCell> NumericCells
        {
            get { return this.Sheet.NumericCells; }
            internal set { this.Sheet.NumericCells = value ?? new Dictionary<(int, int), NumericCell>(); }
        }

        /// <summary>
        /// The rows that are currently hidden (collapsed). While a row is hidden its height is
        /// zero, so it disappears from the sheet without renumbering the remaining rows.
        /// </summary>
        public HashSet<int> HiddenRows { get { return this.Sheet.HiddenRows; } }

        /// <summary>
        /// The columns that are currently hidden. While a column is hidden its width is zero, so
        /// it disappears from the sheet without renumbering the remaining columns: the column
        /// header letter, the header caption, the grid lines and the cell content of a hidden
        /// column are all skipped, and the neighbouring columns are drawn seamlessly next to each
        /// other.
        /// </summary>
        public HashSet<int> HiddenColumns { get { return this.Sheet.HiddenColumns; } }

        /// <summary>
        /// The ranges that are read-only. Cells inside a read-only range cannot be edited, pasted
        /// into or cleared, but they can still be selected and copied.
        /// </summary>
        public List<SelectionRange> ReadonlyRanges { get { return this.Sheet.ReadonlyRanges; } }

        /// <summary>
        /// Whether the table maintains <see cref="HiddenRows"/> itself when a <see cref="TreeGradeCell"/>
        /// toggle box is clicked. When this is <see langword="false"/> the <see cref="Spreadsheet.TreeExpand"/>
        /// event is raised but the host is responsible for calling <see cref="SetRowVisible"/>.
        /// </summary>
        public bool AutoManageRows
        {
            get { return this.Sheet.AutoManageRows; }
            set { this.Sheet.AutoManageRows = value; }
        }

        private string _emptyNumericLinkText = "…";

        /// <summary>
        /// The placeholder drawn by a <see cref="NumericLinkCell"/> whose value is zero, empty or
        /// not a number, so that such a cell keeps a clickable target. Defaults to "…"; set it to
        /// <see langword="null"/> or "" to switch the placeholder off for the whole table (a
        /// single cell can still opt in through <see cref="LinkCell.EmptyText"/>, and a cell can
        /// opt out through <see cref="NumericLinkCell.ResponseEmpty"/>).
        /// The placeholder is display only: exports, the clipboard and printed output still see
        /// the empty value.
        /// </summary>
        public string EmptyNumericLinkText
        {
            get { return _emptyNumericLinkText; }
            set { _emptyNumericLinkText = value; this.InvalidateVisual(); }
        }

        public List<CellBackground> CellBackgrounds
        {
            get { return this.Sheet.CellBackgrounds; }
            set { this.Sheet.CellBackgrounds = value ?? new List<CellBackground>(); InvalidateVisual(); }
        }

        public List<CellBorder> CellBorders
        {
            get { return this.Sheet.CellBorders; }
            set { this.Sheet.CellBorders = value ?? new List<CellBorder>(); InvalidateVisual(); }
        }

        /// <summary>
        /// Adds a background to a range of cells and schedules a redraw.
        /// </summary>
        /// <param name="range">The range of cells the background is applied to.</param>
        /// <param name="brush">The brush used to fill the background.</param>
        public void AddCellBackground(SelectionRange range, IBrush brush)
        {
            this.Sheet.CellBackgrounds.Add(new CellBackground(range, brush));
            InvalidateVisual();
        }

        /// <summary>
        /// Removes all cell backgrounds and schedules a redraw.
        /// </summary>
        public void ClearCellBackgrounds()
        {
            this.Sheet.CellBackgrounds.Clear();
            InvalidateVisual();
        }

        /// <summary>
        /// Adds a border line segment and schedules a redraw.
        /// </summary>
        /// <param name="border">The border to add.</param>
        public void AddCellBorder(CellBorder border)
        {
            if (border != null)
            {
                this.Sheet.CellBorders.Add(border);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Removes all border line segments and schedules a redraw.
        /// </summary>
        public void ClearCellBorders()
        {
            this.Sheet.CellBorders.Clear();
            InvalidateVisual();
        }

        public static readonly StyledProperty<TextAlignment> DefaultTextAlignmentProperty = AvaloniaProperty.Register<Table, TextAlignment>(nameof(DefaultTextAlignment), TextAlignment.Left);

        public TextAlignment DefaultTextAlignment
        {
            get { return GetValue(DefaultTextAlignmentProperty); }
            set { SetValue(DefaultTextAlignmentProperty, value); }
        }

        public static readonly StyledProperty<VerticalAlignment> DefaultVerticalAlignmentProperty = AvaloniaProperty.Register<Table, VerticalAlignment>(nameof(DefaultVerticalAlignment), VerticalAlignment.Center);

        public VerticalAlignment DefaultVerticalAlignment
        {
            get { return GetValue(DefaultVerticalAlignmentProperty); }
            set { SetValue(DefaultVerticalAlignmentProperty, value); }
        }


        public static readonly StyledProperty<Thickness> DefaultMarginProperty = AvaloniaProperty.Register<Table, Thickness>(nameof(DefaultMargin), new Thickness(3));

        public Thickness DefaultMargin
        {
            get { return GetValue(DefaultMarginProperty); }
            set { SetValue(DefaultMarginProperty, value); }
        }

        /// <summary>
        /// The ranges of cells that are merged together. The content of the top-left cell of each
        /// range is drawn centred over the whole merged range, and grid lines inside the merged
        /// range are suppressed.
        /// </summary>
        public ImmutableList<SelectionRange> MergedRanges
        {
            get { return this.Sheet.MergedRanges; }
            set { this.Sheet.MergedRanges = value ?? ImmutableList.Create<SelectionRange>(); this.InvalidateVisual(); }
        }

        static Table()
        {
            AffectsRender<Table>(OffsetProperty, DefaultColumnWidthProperty, DefaultRowHeightProperty, GridColorProperty, SelectionProperty, ForegroundProperty, BackgroundProperty, FontFamilyProperty, FontSizeProperty, FontStyleProperty, FontWeightProperty, SelectionAccentProperty, IsFocusedProperty, DefaultMarginProperty);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == DefaultColumnWidthProperty)
            {
                this.Container.FindControl<ScrollBar>("HorizontalScrollBar").SmallChange = this.DefaultColumnWidth;
                this.Container.FindControl<ScrollBar>("HorizontalScrollBar").LargeChange = this.DefaultColumnWidth * 3;
            }
            else if (change.Property == DefaultRowHeightProperty)
            {
                this.Container.FindControl<ScrollBar>("VerticalScrollBar").SmallChange = this.DefaultRowHeight;
                this.Container.FindControl<ScrollBar>("VerticalScrollBar").LargeChange = this.DefaultRowHeight * 3;
            }
        }

        public Table()
        {
            Data = new Dictionary<(int, int), string>();
            FormulaCells = new Dictionary<(int, int), CellData>();

            RowHeights = new Dictionary<int, double>();
            ColumnWidths = new Dictionary<int, double>();

            CellTypefaces = new Dictionary<(int, int), Typeface>();
            RowTypefaces = new Dictionary<int, Typeface>();
            ColumnTypefaces = new Dictionary<int, Typeface>();

            CellForeground = new Dictionary<(int, int), IBrush>();
            RowForeground = new Dictionary<int, IBrush>();
            ColumnForeground = new Dictionary<int, IBrush>();

            CellTextAlignment = new Dictionary<(int, int), TextAlignment>();
            CellVerticalAlignment = new Dictionary<(int, int), VerticalAlignment>();
            CellMargin = new Dictionary<(int, int), Thickness>();
            TitleCells = new Dictionary<(int, int), TitleCell>();
            LinkCells = new Dictionary<(int, int), LinkCell>();

            this.Transitions = CachedTransitions;
        }

        private Transitions CachedTransitions = new Avalonia.Animation.Transitions() { new VectorTransition() { Property = OffsetProperty, Duration = TimeSpan.FromMilliseconds(100) } };

        public void PauseTransitions()
        {
            this.Transitions = null;
        }

        public void StopTransitions()
        {
            this.Transitions = null;
            this.CachedTransitions = null;
        }

        public void ResumeTransitions()
        {
            this.Transitions = CachedTransitions;
        }



        public (int left, int top) GetCell(double x, double y)
        {
            int leftIndex = 0;
            double currWidth = 0;

            // Hidden columns have a width of zero, so currWidth does not advance for them; the
            // MaxTableWidth guard makes sure the loop can never run away.
            while (currWidth < x && leftIndex < MaxTableWidth)
            {
                currWidth += GetWidth(leftIndex);
                leftIndex++;
            }

            int topIndex = 0;
            double currHeight = 0;

            while (currHeight < y && topIndex < MaxTableHeight)
            {
                currHeight += GetHeight(topIndex);
                topIndex++;
            }

            return (leftIndex, topIndex);
        }

        public (int left, double leftDelta, int width, double actualWidth, double startWidth) GetRangeX(double x, double width)
        {
            int leftIndex = 0;
            double currWidth = 0;
            double leftDelta = -x;

            double nextWidth = GetWidth(leftIndex);

            // Hidden columns have a width of zero, so currWidth does not advance for them; the
            // MaxTableWidth guard makes sure the loop can never run away (GetRangeY has the
            // equivalent guard for hidden rows).
            while (currWidth + nextWidth < x && leftIndex < MaxTableWidth)
            {
                currWidth += nextWidth;
                leftDelta = currWidth - x;
                leftIndex++;
                nextWidth = GetWidth(leftIndex);
            }

            double startWidth = currWidth;

            int rightIndex = leftIndex;
            while (currWidth < x + width && rightIndex < MaxTableWidth)
            {
                currWidth += GetWidth(rightIndex);
                rightIndex++;
            }

            // The rendering loops iterate x = 0..width (inclusive), so width is the
            // index of the last visible column. rightIndex is one past the last column
            // whose width has been accumulated.
            return (leftIndex, leftDelta, Math.Max(0, rightIndex - leftIndex - 1), currWidth - startWidth, startWidth);
        }

        public (int top, double topDelta, int height, double actualHeight, double startHeight) GetRangeY(double y, double height)
        {
            int topIndex = 0;
            double currHeight = 0;
            double topDelta = -y;

            double nextHeight = GetHeight(topIndex);

            // Hidden rows have a height of zero, so currHeight does not advance for them; the
            // MaxTableHeight guard makes sure the loop can never run away.
            while (currHeight + nextHeight < y && topIndex < MaxTableHeight)
            {
                currHeight += nextHeight;
                topDelta = currHeight - y;
                topIndex++;
                nextHeight = GetHeight(topIndex);
            }

            double startHeight = currHeight;

            int bottomIndex = topIndex;
            while (currHeight < y + height && bottomIndex < MaxTableHeight)
            {
                currHeight += GetHeight(bottomIndex);
                bottomIndex++;
            }

            // The rendering loops iterate y = 0..height (inclusive), so height is the
            // index of the last visible row. bottomIndex is one past the last row
            // whose height has been accumulated.
            return (topIndex, topDelta, Math.Max(0, bottomIndex - topIndex - 1), currHeight - startHeight, startHeight);
        }

        public (int left, double leftDelta, int top, double topDelta, int width, double actualWidth, double startWidth, int height, double actualHeight, double startHeight) GetRange(double x, double y, double width, double height)
        {
            (int leftIndex, double leftDelta, int intWidth, double actualWidth, double startWidth) = GetRangeX(x, width);
            (int topIndex, double topDelta, int intHeight, double actualHeight, double startHeight) = GetRangeY(y, height);

            return (leftIndex, leftDelta, topIndex, topDelta, intWidth, actualWidth, startWidth, intHeight, actualHeight, startHeight);
        }

        internal double GetWidth(int columnIndex)
        {
            // Hidden columns have a width of zero: they keep their index, so all the existing
            // column-indexed dictionaries stay valid, but they occupy no horizontal space and
            // are skipped by the rendering and hit testing code.
            if (IsColumnHidden(columnIndex))
            {
                return 0;
            }

            if (!ColumnWidths.TryGetValue(columnIndex, out double h))
            {
                return DefaultColumnWidth;
            }
            else
            {
                return h;
            }
        }

        internal double GetHeight(int rowIndex)
        {
            // Hidden (collapsed) rows have a height of zero: they keep their index, so all the
            // existing row-indexed dictionaries stay valid, but they occupy no vertical space and
            // are skipped by the hit testing code (their interval in lastDrawnYs is empty).
            if (IsRowHidden(rowIndex))
            {
                return 0;
            }

            // A row that is growing / shrinking as part of a tree expand / collapse animation uses
            // the interpolated height; this is the single source of row heights, so the whole
            // rendering / hit testing / navigation pipeline follows the animation automatically.
            if (animatedRowHeights.Count > 0 && animatedRowHeights.TryGetValue(rowIndex, out double animatedHeight))
            {
                return animatedHeight;
            }

            if (!RowHeights.TryGetValue(rowIndex, out double h))
            {
                return DefaultRowHeight;
            }
            else
            {
                return h;
            }
        }

        internal double GetWidth(int columnIndex, out bool wasDefault)
        {
            if (!ColumnWidths.TryGetValue(columnIndex, out double h))
            {
                wasDefault = true;
                return DefaultColumnWidth;
            }
            else
            {
                wasDefault = false;
                return h;
            }
        }

        internal double GetHeight(int rowIndex, out bool wasDefault)
        {
            if (!RowHeights.TryGetValue(rowIndex, out double h))
            {
                wasDefault = true;
                return DefaultRowHeight;
            }
            else
            {
                wasDefault = false;
                return h;
            }
        }


        internal Rect GetCoordinates(int x, int y)
        {
            double currX = 0;
            double currY = 0;

            for (int i = 0; i < x; i++)
            {
                currX += GetWidth(i);
            }

            for (int i = 0; i < y; i++)
            {
                currY += GetHeight(i);
            }

            return new Rect(currX, currY, GetWidth(x), GetHeight(y));
        }

        /// <summary>
        /// Checks whether the specified row is currently hidden (collapsed).
        /// </summary>
        /// <param name="row">The row index.</param>
        /// <returns><see langword="true"/> if the row is hidden.</returns>
        public bool IsRowHidden(int row)
        {
            return HiddenRows != null && HiddenRows.Contains(row);
        }

        /// <summary>
        /// Checks whether the specified row is currently visible (not collapsed).
        /// </summary>
        /// <param name="row">The row index.</param>
        /// <returns><see langword="true"/> if the row is visible.</returns>
        public bool IsRowVisible(int row)
        {
            return !IsRowHidden(row);
        }

        /// <summary>
        /// Shows or hides a row.
        /// </summary>
        /// <param name="row">The row index.</param>
        /// <param name="visible"><see langword="true"/> to show the row, <see langword="false"/> to hide it.</param>
        public void SetRowVisible(int row, bool visible)
        {
            if (visible)
            {
                if (HiddenRows.Remove(row))
                {
                    InvalidateTreeCache();
                    this.InvalidateVisual();
                }
            }
            else
            {
                if (HiddenRows.Add(row))
                {
                    InvalidateTreeCache();
                    this.InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Shows or hides a set of rows.
        /// </summary>
        /// <param name="rows">The row indices.</param>
        /// <param name="visible"><see langword="true"/> to show the rows, <see langword="false"/> to hide them.</param>
        public void SetRowsVisible(IEnumerable<int> rows, bool visible)
        {
            if (rows == null)
            {
                return;
            }

            foreach (int row in rows)
            {
                if (visible)
                {
                    HiddenRows.Remove(row);
                }
                else
                {
                    HiddenRows.Add(row);
                }
            }

            InvalidateTreeCache();
            this.InvalidateVisual();
        }

        /// <summary>
        /// Removes every entry from <see cref="HiddenRows"/>, showing all rows again.
        /// </summary>
        public void ShowAllRows()
        {
            CancelTreeRowAnimation();

            if (HiddenRows.Count > 0)
            {
                HiddenRows.Clear();
                InvalidateTreeCache();
                this.InvalidateVisual();
            }
        }

        /// <summary>
        /// Enables or disables the smooth expand / collapse animation of tree rows. When enabled
        /// (the default), the child rows of an expanded or collapsed <see cref="TreeGradeCell"/>
        /// grow from zero height to their actual height (or shrink to zero and fade out) over
        /// <see cref="TreeAnimationDuration"/>. The animation is skipped automatically when more
        /// than <see cref="TreeAnimationMaxRows"/> rows would be animated at once.
        /// </summary>
        public bool IsTreeAnimationEnabled { get; set; } = true;

        /// <summary>
        /// Duration of the tree expand / collapse animation.
        /// </summary>
        public TimeSpan TreeAnimationDuration { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// Maximum number of rows that are animated at the same time; when an expand / collapse
        /// affects more rows than this, the final state is applied immediately instead.
        /// </summary>
        public int TreeAnimationMaxRows { get; set; } = 1000;

        /// <summary>
        /// A single row that is currently growing (expand) or shrinking (collapse).
        /// </summary>
        private sealed class TreeRowAnimation
        {
            public int Row;
            public double NaturalHeight;
            public long StartTicks;
            public bool Collapsing;
        }

        private readonly List<TreeRowAnimation> treeRowAnimations = new List<TreeRowAnimation>();

        /// <summary>The height currently interpolated for each animating row (overrides <see cref="GetHeight"/>).</summary>
        private readonly Dictionary<int, double> animatedRowHeights = new Dictionary<int, double>();

        /// <summary>The opacity currently interpolated for each animating row (used to fade the content in / out).</summary>
        private readonly Dictionary<int, double> animatedRowOpacity = new Dictionary<int, double>();

        private Avalonia.Threading.DispatcherTimer treeAnimationTimer;
        private System.Diagnostics.Stopwatch treeAnimationStopwatch;

        /// <summary>
        /// Starts a height animation for the specified rows. Collapsing rows keep their index and
        /// stay visible until they have fully shrunk; only then are they added to
        /// <see cref="HiddenRows"/>, so that they shrink smoothly instead of disappearing at once.
        /// </summary>
        /// <param name="rows">The rows to animate.</param>
        /// <param name="collapsing"><see langword="true"/> to shrink the rows to zero height, <see langword="false"/> to grow them from zero height.</param>
        private void StartTreeRowAnimation(List<int> rows, bool collapsing)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            // Animating thousands of rows would only waste CPU: apply the final state immediately.
            if (rows.Count > Math.Max(0, TreeAnimationMaxRows))
            {
                if (collapsing)
                {
                    foreach (int row in rows)
                    {
                        HiddenRows.Add(row);
                    }

                    InvalidateTreeCache();
                }

                return;
            }

            long startTicks = GetTreeAnimationStopwatch().ElapsedMilliseconds;

            foreach (int row in rows)
            {
                double naturalHeight = GetNaturalRowHeight(row);

                treeRowAnimations.RemoveAll(a => a.Row == row);

                animatedRowHeights[row] = collapsing ? naturalHeight : 0;
                animatedRowOpacity[row] = collapsing ? 1 : 0;

                treeRowAnimations.Add(new TreeRowAnimation() { Row = row, NaturalHeight = naturalHeight, StartTicks = startTicks, Collapsing = collapsing });
            }

            if (treeAnimationTimer == null)
            {
                treeAnimationTimer = new Avalonia.Threading.DispatcherTimer(TimeSpan.FromMilliseconds(16), Avalonia.Threading.DispatcherPriority.Render, TreeAnimationTick);
            }

            treeAnimationTimer.Start();

            this.InvalidateVisual();
            this.InvalidateHeaders();
        }

        /// <summary>
        /// Lazily creates the shared stopwatch used to measure the animation progress.
        /// </summary>
        private System.Diagnostics.Stopwatch GetTreeAnimationStopwatch()
        {
            if (treeAnimationStopwatch == null)
            {
                treeAnimationStopwatch = System.Diagnostics.Stopwatch.StartNew();
            }

            return treeAnimationStopwatch;
        }

        /// <summary>
        /// Advances every running animation by one frame and repaints the table and the headers.
        /// </summary>
        private void TreeAnimationTick(object sender, EventArgs e)
        {
            if (treeRowAnimations.Count == 0)
            {
                treeAnimationTimer.Stop();
                return;
            }

            double durationMs = Math.Max(1, TreeAnimationDuration.TotalMilliseconds);
            long nowMs = GetTreeAnimationStopwatch().ElapsedMilliseconds;

            for (int i = treeRowAnimations.Count - 1; i >= 0; i--)
            {
                TreeRowAnimation animation = treeRowAnimations[i];
                double t = (nowMs - animation.StartTicks) / durationMs;

                if (t >= 1)
                {
                    FinishTreeRowAnimation(animation);
                    continue;
                }

                // Ease-out quad: fast at the beginning, slowing down towards the end.
                double eased = 1 - (1 - t) * (1 - t);

                animatedRowHeights[animation.Row] = animation.NaturalHeight * eased;
                animatedRowOpacity[animation.Row] = eased;
            }

            this.InvalidateVisual();
            this.InvalidateHeaders();

            if (treeRowAnimations.Count == 0)
            {
                treeAnimationTimer.Stop();
            }
        }

        /// <summary>
        /// Applies the final state of one animation: shrinking rows are added to
        /// <see cref="HiddenRows"/> only now, after they have fully collapsed.
        /// </summary>
        private void FinishTreeRowAnimation(TreeRowAnimation animation)
        {
            treeRowAnimations.Remove(animation);
            animatedRowHeights.Remove(animation.Row);
            animatedRowOpacity.Remove(animation.Row);

            if (animation.Collapsing)
            {
                HiddenRows.Add(animation.Row);
                InvalidateTreeCache();
            }
        }

        /// <summary>
        /// Instantly completes every running animation, applying their final states. Used when a
        /// new expand / collapse starts while another one is still running, so that the effects
        /// never fight over the same rows.
        /// </summary>
        private void CompleteTreeRowAnimations()
        {
            if (treeRowAnimations.Count == 0)
            {
                return;
            }

            for (int i = treeRowAnimations.Count - 1; i >= 0; i--)
            {
                FinishTreeRowAnimation(treeRowAnimations[i]);
            }

            animatedRowHeights.Clear();
            animatedRowOpacity.Clear();
            treeAnimationTimer?.Stop();
        }

        /// <summary>
        /// Discards every running animation without applying their final states (used when all rows
        /// are shown again, which must not re-hide the rows that were still collapsing).
        /// </summary>
        private void CancelTreeRowAnimation()
        {
            treeRowAnimations.Clear();
            animatedRowHeights.Clear();
            animatedRowOpacity.Clear();
            treeAnimationTimer?.Stop();
        }

        /// <summary>
        /// Returns the height of the specified row ignoring any running animation.
        /// </summary>
        private double GetNaturalRowHeight(int row)
        {
            if (RowHeights.TryGetValue(row, out double h))
            {
                return h;
            }

            return DefaultRowHeight;
        }

        /// <summary>
        /// Checks whether the specified column is currently hidden.
        /// </summary>
        /// <param name="column">The column index.</param>
        /// <returns><see langword="true"/> if the column is hidden.</returns>
        public bool IsColumnHidden(int column)
        {
            return HiddenColumns != null && HiddenColumns.Contains(column);
        }

        /// <summary>
        /// Checks whether the specified column is currently visible (not hidden).
        /// </summary>
        /// <param name="column">The column index.</param>
        /// <returns><see langword="true"/> if the column is visible.</returns>
        public bool IsColumnVisible(int column)
        {
            return !IsColumnHidden(column);
        }

        /// <summary>
        /// Shows or hides a column. A hidden column has a width of zero and is skipped by the
        /// rendering, hit testing, navigation, copy and export code; it keeps its index, so every
        /// (column, row) key of the cell dictionaries stays valid.
        /// </summary>
        /// <param name="column">The column index.</param>
        /// <param name="visible"><see langword="true"/> to show the column, <see langword="false"/> to hide it.</param>
        public void SetColumnVisible(int column, bool visible)
        {
            bool changed = visible ? HiddenColumns.Remove(column) : HiddenColumns.Add(column);

            if (changed)
            {
                this.InvalidateVisual();
                this.InvalidateHeaders();
            }
        }

        /// <summary>
        /// Shows or hides a set of columns.
        /// </summary>
        /// <param name="columns">The column indices.</param>
        /// <param name="visible"><see langword="true"/> to show the columns, <see langword="false"/> to hide them.</param>
        public void SetColumnsVisible(IEnumerable<int> columns, bool visible)
        {
            if (columns == null)
            {
                return;
            }

            foreach (int column in columns)
            {
                if (visible)
                {
                    HiddenColumns.Remove(column);
                }
                else
                {
                    HiddenColumns.Add(column);
                }
            }

            this.InvalidateVisual();
            this.InvalidateHeaders();
        }

        /// <summary>
        /// Removes every entry from <see cref="HiddenColumns"/>, showing all columns again.
        /// </summary>
        public void ShowAllColumns()
        {
            if (HiddenColumns.Count > 0)
            {
                HiddenColumns.Clear();
                this.InvalidateVisual();
                this.InvalidateHeaders();
            }
        }

        /// <summary>
        /// Returns the next column that is not hidden, starting from <paramref name="column"/> and
        /// moving in the specified direction. Returns <paramref name="column"/> itself when there
        /// is no such column, so that keyboard navigation never lands on an invisible cell.
        /// </summary>
        /// <param name="column">The column to start from (exclusive).</param>
        /// <param name="direction">+1 to search rightwards, -1 to search leftwards.</param>
        /// <returns>The next visible column, or <paramref name="column"/> when there is none.</returns>
        public int NextVisibleColumn(int column, int direction)
        {
            int step = direction >= 0 ? 1 : -1;
            int limit = Math.Min(MaxTableWidth, GetLastUsedColumn() + 1);
            int candidate = column + step;

            while (candidate >= 0 && candidate <= limit)
            {
                if (IsColumnVisible(candidate))
                {
                    return candidate;
                }

                candidate += step;
            }

            return column;
        }

        /// <summary>
        /// Returns the closest visible column to the left of <paramref name="column"/>, or
        /// <paramref name="column"/> itself when there is none. Unlike
        /// <see cref="NextVisibleColumn"/> the search is not bounded by the columns that are
        /// referenced by the data: resizing a column works over the whole sheet, because the right
        /// border of a column belongs to the previous visible column even when that column carries
        /// no data at all (the sheet always has more empty columns to the right of the last one
        /// that contains something).
        /// </summary>
        /// <param name="column">The column to start from (exclusive).</param>
        /// <returns>The previous visible column, or <paramref name="column"/> when there is none.</returns>
        internal int PreviousVisibleColumn(int column)
        {
            int candidate = column - 1;

            while (candidate >= 0)
            {
                if (IsColumnVisible(candidate))
                {
                    return candidate;
                }

                candidate--;
            }

            return column;
        }

        /// <summary>
        /// Returns the highest column index that is referenced by the cell data, the tree cells or
        /// the column widths. Used to bound the search performed by <see cref="NextVisibleColumn"/>.
        /// </summary>
        internal int GetLastUsedColumn()
        {
            int max = -1;

            foreach (KeyValuePair<(int, int), string> kvp in Data)
            {
                if (kvp.Key.Item1 > max)
                {
                    max = kvp.Key.Item1;
                }
            }

            if (TreeCells != null)
            {
                foreach (KeyValuePair<(int, int), TreeGradeCell> kvp in TreeCells)
                {
                    if (kvp.Key.Item1 > max)
                    {
                        max = kvp.Key.Item1;
                    }
                }
            }

            if (ColumnWidths != null)
            {
                foreach (KeyValuePair<int, double> kvp in ColumnWidths)
                {
                    if (kvp.Key > max)
                    {
                        max = kvp.Key;
                    }
                }
            }

            return max;
        }

        /// <summary>
        /// The smallest amount of scrollable space (in pixels) that is kept visible next to a frozen
        /// strip: the frozen area is never allowed to cover the whole viewport.
        /// </summary>
        private const double MinimumScrollableArea = 40;

        // The frozen row / column counts live in the current worksheet (Sheet.FrozenRowCount and
        // Sheet.FrozenColumnCount), so that every worksheet keeps its own frozen panes.

        private double frozenStripWidth = 0;
        private double frozenStripHeight = 0;

        /// <summary>
        /// Gets the width of the frozen column strip as it was last drawn. The value is clamped so
        /// that a scrollable area always remains visible; it is 0 when no column is frozen.
        /// </summary>
        internal double FrozenStripWidth => frozenStripWidth;

        /// <summary>
        /// Gets the height of the frozen row strip as it was last drawn. The value is clamped so
        /// that a scrollable area always remains visible; it is 0 when no row is frozen.
        /// </summary>
        internal double FrozenStripHeight => frozenStripHeight;

        /// <summary>
        /// Gets or sets the number of columns that are frozen at the left edge of the table. Frozen
        /// columns are always the first <c>n</c> columns: they keep their position while the rest of
        /// the table scrolls horizontally, and they are separated from the scrollable area by a
        /// thicker line. Setting this to 0 unfreezes every column.
        /// </summary>
        public int FrozenColumnCount
        {
            get { return this.Sheet.FrozenColumnCount; }
            set
            {
                int newValue = Math.Max(0, value);

                if (this.Sheet.FrozenColumnCount != newValue)
                {
                    this.Sheet.FrozenColumnCount = newValue;
                    this.InvalidateVisual();
                    this.InvalidateHeaders();
                }
            }
        }

        /// <summary>
        /// Gets or sets the number of rows that are frozen at the top edge of the table. Frozen rows
        /// are always the first <c>n</c> rows: they keep their position while the rest of the table
        /// scrolls vertically, and they are separated from the scrollable area by a thicker line.
        /// Setting this to 0 unfreezes every row.
        /// </summary>
        public int FrozenRowCount
        {
            get { return this.Sheet.FrozenRowCount; }
            set
            {
                int newValue = Math.Max(0, value);

                if (this.Sheet.FrozenRowCount != newValue)
                {
                    this.Sheet.FrozenRowCount = newValue;
                    this.InvalidateVisual();
                    this.InvalidateHeaders();
                }
            }
        }

        /// <summary>
        /// Computes the total width of the frozen columns (0 when no column is frozen).
        /// </summary>
        private double ComputeFrozenWidth()
        {
            if (this.Sheet.FrozenColumnCount <= 0)
            {
                return 0;
            }

            double width = 0;

            for (int i = 0; i < this.Sheet.FrozenColumnCount && i < MaxTableWidth; i++)
            {
                width += GetWidth(i);
            }

            return width;
        }

        /// <summary>
        /// Computes the total height of the frozen rows (0 when no row is frozen).
        /// </summary>
        private double ComputeFrozenHeight()
        {
            if (this.Sheet.FrozenRowCount <= 0)
            {
                return 0;
            }

            double height = 0;

            for (int i = 0; i < this.Sheet.FrozenRowCount && i < MaxTableHeight; i++)
            {
                height += GetHeight(i);
            }

            return height;
        }

        /// <summary>
        /// Returns the width of the frozen column strip, clamped so that a scrollable area of at
        /// least <see cref="MinimumScrollableArea"/> pixels always remains visible.
        /// </summary>
        /// <param name="viewWidth">The width of the viewport.</param>
        internal double GetClampedFrozenWidth(double viewWidth)
        {
            double width = ComputeFrozenWidth();
            double max = viewWidth - MinimumScrollableArea;

            if (max <= 0)
            {
                return 0;
            }

            return width > max ? max : width;
        }

        /// <summary>
        /// Returns the height of the frozen row strip, clamped so that a scrollable area of at least
        /// <see cref="MinimumScrollableArea"/> pixels always remains visible.
        /// </summary>
        /// <param name="viewHeight">The height of the viewport.</param>
        internal double GetClampedFrozenHeight(double viewHeight)
        {
            double height = ComputeFrozenHeight();
            double max = viewHeight - MinimumScrollableArea;

            if (max <= 0)
            {
                return 0;
            }

            return height > max ? max : height;
        }

        /// <summary>
        /// Checks whether the specified column belongs to the frozen strip.
        /// </summary>
        internal bool IsColumnFrozen(int column)
        {
            return frozenStripWidth > 0 && column >= 0 && column < this.Sheet.FrozenColumnCount;
        }

        /// <summary>
        /// Checks whether the specified row belongs to the frozen strip.
        /// </summary>
        internal bool IsRowFrozen(int row)
        {
            return frozenStripHeight > 0 && row >= 0 && row < this.Sheet.FrozenRowCount;
        }

        /// <summary>
        /// Checks whether a screen (viewport) coordinate lies over the frozen column strip.
        /// </summary>
        internal bool IsOverFrozenColumns(double screenX)
        {
            return frozenStripWidth > 0 && screenX >= 0 && screenX < frozenStripWidth;
        }

        /// <summary>
        /// Checks whether a screen (viewport) coordinate lies over the frozen row strip.
        /// </summary>
        internal bool IsOverFrozenRows(double screenY)
        {
            return frozenStripHeight > 0 && screenY >= 0 && screenY < frozenStripHeight;
        }

        /// <summary>
        /// Converts a screen (viewport) coordinate into the horizontal drawing coordinate of the
        /// region that is currently under it. The frozen strip uses its own (unscrolled) origin, so
        /// a pointer position over it has to be translated differently.
        /// </summary>
        internal double GetDrawnX(double screenX)
        {
            return IsOverFrozenColumns(screenX) ? screenX + 1 : screenX - lastDrawnDeltaX;
        }

        /// <summary>
        /// Converts a screen (viewport) coordinate into the vertical drawing coordinate of the region
        /// that is currently under it.
        /// </summary>
        internal double GetDrawnY(double screenY)
        {
            return IsOverFrozenRows(screenY) ? screenY + 1 : screenY - lastDrawnDeltaY;
        }

        /// <summary>
        /// Returns the index of the frozen column that contains the specified screen coordinate.
        /// </summary>
        internal int GetFrozenColumnIndex(double screenX)
        {
            double x = screenX + 1;
            double curr = 0;

            for (int i = 0; i < this.Sheet.FrozenColumnCount; i++)
            {
                // A hidden frozen column has a width of zero, so it can never contain the pointer.
                curr += GetWidth(i);

                if (x < curr)
                {
                    return i;
                }
            }

            return Math.Max(0, this.Sheet.FrozenColumnCount - 1);
        }

        /// <summary>
        /// Returns the index of the frozen row that contains the specified screen coordinate.
        /// </summary>
        internal int GetFrozenRowIndex(double screenY)
        {
            double y = screenY + 1;
            double curr = 0;

            for (int i = 0; i < this.Sheet.FrozenRowCount; i++)
            {
                curr += GetHeight(i);

                if (y < curr)
                {
                    return i;
                }
            }

            return Math.Max(0, this.Sheet.FrozenRowCount - 1);
        }

        /// <summary>
        /// Returns the table coordinate of the left edge of the specified column.
        /// </summary>
        internal double GetColumnStart(int column)
        {
            double x = 0;

            for (int i = 0; i < column; i++)
            {
                x += GetWidth(i);
            }

            return x;
        }

        /// <summary>
        /// Returns the table coordinate of the top edge of the specified row.
        /// </summary>
        internal double GetRowStart(int row)
        {
            double y = 0;

            for (int i = 0; i < row; i++)
            {
                y += GetHeight(i);
            }

            return y;
        }

        /// <summary>
        /// Returns the screen (viewport) coordinate of the left edge of the specified column, taking
        /// the frozen strip into account.
        /// </summary>
        internal double GetColumnScreenLeft(int column)
        {
            double start = GetColumnStart(column);

            return (IsColumnFrozen(column) ? start : start - this.Offset.X) - 1;
        }

        /// <summary>
        /// Returns the screen (viewport) coordinate of the top edge of the specified row, taking the
        /// frozen strip into account.
        /// </summary>
        internal double GetRowScreenTop(int row)
        {
            double start = GetRowStart(row);

            return (IsRowFrozen(row) ? start : start - this.Offset.Y) - 1;
        }

        /// <summary>
        /// Returns the left or right edge of the specified column in drawing coordinates, or
        /// <see cref="double.NegativeInfinity"/> / <see cref="double.PositiveInfinity"/> when the
        /// column has been scrolled out of the scrollable region. Frozen columns are resolved from
        /// the frozen strip, which uses its own (unscrolled) coordinate origin.
        /// </summary>
        /// <param name="column">The column index.</param>
        /// <param name="leftEdge"><see langword="true"/> to return the left edge, <see langword="false"/> for the right edge.</param>
        internal double GetSelectionColumnEdge(int column, bool leftEdge)
        {
            // A frozen column is never part of the scrollable region, hence its edges are the same
            // as in the frozen strip: they do not move with the scroll offset.
            if (IsColumnFrozen(column))
            {
                double frozenStart = GetColumnStart(column);

                return leftEdge ? frozenStart : frozenStart + GetWidth(column);
            }

            if (column >= lastDrawnLeft && column <= lastDrawnLeft + lastDrawnWidth)
            {
                if (leftEdge)
                {
                    return column == lastDrawnLeft ? 0 : lastDrawnXs[column - lastDrawnLeft - 1];
                }
                else
                {
                    return lastDrawnXs[column - lastDrawnLeft];
                }
            }

            return column < lastDrawnLeft ? double.NegativeInfinity : double.PositiveInfinity;
        }

        /// <summary>
        /// Returns the top or bottom edge of the specified row in drawing coordinates, or
        /// <see cref="double.NegativeInfinity"/> / <see cref="double.PositiveInfinity"/> when the row
        /// has been scrolled out of the scrollable region. Frozen rows are resolved from the frozen
        /// strip, which uses its own (unscrolled) coordinate origin.
        /// </summary>
        /// <param name="row">The row index.</param>
        /// <param name="topEdge"><see langword="true"/> to return the top edge, <see langword="false"/> for the bottom edge.</param>
        internal double GetSelectionRowEdge(int row, bool topEdge)
        {
            if (IsRowFrozen(row))
            {
                double frozenStart = GetRowStart(row);

                return topEdge ? frozenStart : frozenStart + GetHeight(row);
            }

            if (row >= lastDrawnTop && row <= lastDrawnTop + lastDrawnHeight)
            {
                if (topEdge)
                {
                    return row == lastDrawnTop ? 0 : lastDrawnYs[row - lastDrawnTop - 1];
                }
                else
                {
                    return lastDrawnYs[row - lastDrawnTop];
                }
            }

            return row < lastDrawnTop ? double.NegativeInfinity : double.PositiveInfinity;
        }

        /// <summary>
        /// Asks the owning <see cref="Spreadsheet"/> to repaint the column and row headers, which
        /// draw the header letters and numbers independently of the content area.
        /// </summary>
        private void InvalidateHeaders()
        {
            this.Container?.InvalidateHeaders();
        }

        /// <summary>
        /// Returns the highest row index that is referenced by the cell data, the tree cells or the
        /// row heights. Used to bound the search performed by <see cref="NextVisibleRow"/>.
        /// </summary>
        internal int GetLastUsedRow()
        {
            int max = -1;

            foreach (KeyValuePair<(int, int), string> kvp in Data)
            {
                if (kvp.Key.Item2 > max)
                {
                    max = kvp.Key.Item2;
                }
            }

            if (TreeCells != null)
            {
                foreach (KeyValuePair<(int, int), TreeGradeCell> kvp in TreeCells)
                {
                    if (kvp.Key.Item2 > max)
                    {
                        max = kvp.Key.Item2;
                    }
                }
            }

            if (RowHeights != null)
            {
                foreach (KeyValuePair<int, double> kvp in RowHeights)
                {
                    if (kvp.Key > max)
                    {
                        max = kvp.Key;
                    }
                }
            }

            return max;
        }

        /// <summary>
        /// Returns the next row that is not hidden, starting from <paramref name="row"/> and moving
        /// in the specified direction. Returns <paramref name="row"/> itself when there is no such
        /// row, so that keyboard navigation never lands on an invisible cell.
        /// </summary>
        /// <param name="row">The row to start from (exclusive).</param>
        /// <param name="direction">+1 to search downwards, -1 to search upwards.</param>
        /// <returns>The next visible row, or <paramref name="row"/> when there is none.</returns>
        public int NextVisibleRow(int row, int direction)
        {
            int step = direction >= 0 ? 1 : -1;
            int limit = Math.Min(MaxTableHeight, GetLastUsedRow() + 1);
            int candidate = row + step;

            while (candidate >= 0 && candidate <= limit)
            {
                if (IsRowVisible(candidate))
                {
                    return candidate;
                }

                candidate += step;
            }

            return row;
        }

        /// <summary>
        /// Returns the closest visible row above <paramref name="row"/>, or <paramref name="row"/>
        /// itself when there is none. Unlike <see cref="NextVisibleRow"/> the search is not bounded
        /// by the rows that are referenced by the data: resizing a row works over the whole sheet,
        /// because the bottom border of a row belongs to the previous visible row even when that row
        /// carries no data at all.
        /// </summary>
        /// <param name="row">The row to start from (exclusive).</param>
        /// <returns>The previous visible row, or <paramref name="row"/> when there is none.</returns>
        internal int PreviousVisibleRow(int row)
        {
            int candidate = row - 1;

            while (candidate >= 0)
            {
                if (IsRowVisible(candidate))
                {
                    return candidate;
                }

                candidate--;
            }

            return row;
        }

        /// <summary>
        /// Moves <paramref name="count"/> visible rows away from <paramref name="row"/>, skipping
        /// every hidden row on the way.
        /// </summary>
        /// <param name="row">The row to start from.</param>
        /// <param name="count">The number of visible rows to move (negative to move upwards).</param>
        /// <returns>The row reached, or the last visible row when the end of the sheet is hit.</returns>
        public int MoveVisibleRows(int row, int count)
        {
            int step = count >= 0 ? 1 : -1;
            int total = Math.Abs(count);
            int current = row;

            for (int i = 0; i < total; i++)
            {
                int next = NextVisibleRow(current, step);

                if (next == current)
                {
                    break;
                }

                current = next;
            }

            return current;
        }

        /// <summary>
        /// Returns the right edge the text of a cell may be drawn up to when the worksheet lets long
        /// texts spill over the empty cells that follow them: the right edge of the last empty
        /// neighbour, or the right edge of the cell itself when the next one carries content.
        /// </summary>
        /// <param name="left">The index of the first drawn column of the region.</param>
        /// <param name="row">The row of the cell.</param>
        /// <param name="x">The index of the cell within the region.</param>
        /// <param name="width">The number of drawn columns of the region.</param>
        /// <param name="xs">The right edge of every drawn column, in region coordinates.</param>
        private double GetOverflowRightEdge(int left, int row, int x, int width, double[] xs)
        {
            double right = xs[x];

            for (int neighbour = x + 1; neighbour <= width && neighbour < xs.Length; neighbour++)
            {
                if (!IsCellAvailableForOverflow(left + neighbour, row))
                {
                    break;
                }

                right = xs[neighbour];
            }

            return right;
        }

        /// <summary>
        /// Checks whether the text of a cell may spill into the specified cell: only a cell that is
        /// empty (no data, no custom cell, not merged) and visible can be painted over.
        /// </summary>
        /// <param name="column">The column to paint over.</param>
        /// <param name="row">The row to paint over.</param>
        private bool IsCellAvailableForOverflow(int column, int row)
        {
            if (IsColumnHidden(column) || IsRowHidden(row))
            {
                return false;
            }

            if (Data.ContainsKey((column, row)) || FormulaCells.ContainsKey((column, row)) ||
                TitleCells.ContainsKey((column, row)) || LinkCells.ContainsKey((column, row)) ||
                ButtonCells.ContainsKey((column, row)) || TreeCells.ContainsKey((column, row)) ||
                NumericCells.ContainsKey((column, row)))
            {
                return false;
            }

            if (MergedRanges != null)
            {
                foreach (SelectionRange mergedRange in MergedRanges)
                {
                    if (mergedRange != null && column >= mergedRange.Left && column <= mergedRange.Right && row >= mergedRange.Top && row <= mergedRange.Bottom)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether a cell is part of one of the <see cref="ReadonlyRanges"/>, or belongs to a
        /// worksheet that is read-only as a whole (<see cref="SpreadsheetSheet.IsReadOnly"/>).
        /// </summary>
        /// <param name="column">The column of the cell.</param>
        /// <param name="row">The row of the cell.</param>
        /// <returns><see langword="true"/> if the cell cannot be edited.</returns>
        public bool IsCellReadonly(int column, int row)
        {
            // A worksheet that is read-only as a whole protects every one of its cells: the user
            // cannot edit, paste into, cut or clear them, while the host application can still
            // write them programmatically.
            if (this.Sheet != null && this.Sheet.IsReadOnly)
            {
                return true;
            }

            if (ReadonlyRanges == null || ReadonlyRanges.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < ReadonlyRanges.Count; i++)
            {
                if (ReadonlyRanges[i] != null && ReadonlyRanges[i].Contains(column, row))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Marks a range as read-only. Read-only cells cannot be edited, pasted into or cleared,
        /// but they can still be selected and copied.
        /// </summary>
        /// <param name="range">The range to protect.</param>
        /// <param name="readOnly"><see langword="true"/> to make the range read-only, <see langword="false"/> to make it editable again.</param>
        public void SetRangeReadonly(SelectionRange range, bool readOnly)
        {
            if (readOnly)
            {
                for (int i = 0; i < ReadonlyRanges.Count; i++)
                {
                    if (ReadonlyRanges[i].Equals(range))
                    {
                        return;
                    }
                }

                ReadonlyRanges.Add(range);
            }
            else
            {
                for (int i = ReadonlyRanges.Count - 1; i >= 0; i--)
                {
                    if (ReadonlyRanges[i].Equals(range))
                    {
                        ReadonlyRanges.RemoveAt(i);
                    }
                }
            }

            this.InvalidateVisual();
        }

        /// <summary>
        /// Removes every entry from <see cref="ReadonlyRanges"/>.
        /// </summary>
        public void ClearReadonlyRanges()
        {
            if (ReadonlyRanges.Count > 0)
            {
                ReadonlyRanges.Clear();
                this.InvalidateVisual();
            }
        }

        private Dictionary<(int, int), bool> ancestorSiblingCache = new Dictionary<(int, int), bool>();

        /// <summary>
        /// Discards the cached "does another node of this level follow?" answers used to draw the
        /// dotted guide lines of <see cref="TreeGradeCell"/>s.
        /// </summary>
        internal void InvalidateTreeCache()
        {
            ancestorSiblingCache.Clear();
        }

        /// <summary>
        /// Determines whether the guide line of the specified level continues below
        /// <paramref name="row"/>, i.e. whether a further direct child (a visible row at level
        /// <paramref name="level"/> + 1) follows within the subtree. The answer is used to decide
        /// whether a guide line is drawn full height, turns into a corner in the middle of the
        /// row, or is not drawn at all.
        /// </summary>
        /// <param name="row">The row of the node.</param>
        /// <param name="level">The guide-line (ancestor) level to test.</param>
        /// <returns><see langword="true"/> when a later visible row sits at exactly level <paramref name="level"/> + 1.</returns>
        internal bool HasNextSiblingAtLevel(int row, int level)
        {
            (int, int) key = (row, level);

            if (ancestorSiblingCache.TryGetValue(key, out bool cached))
            {
                return cached;
            }

            bool result = false;
            int limit = Math.Min(MaxTableHeight, GetLastUsedRow() + 1);
            int childLevel = level + 1;

            for (int r = row + 1; r <= limit; r++)
            {
                // Hidden rows have zero height; they can neither continue nor terminate a
                // visible guide line.
                if (IsRowHidden(r))
                {
                    continue;
                }

                if (!TreeCells.TryGetValue((TreeColumn(r), r), out TreeGradeCell cell) || cell == null)
                {
                    break;
                }

                if (cell.Level == childLevel)
                {
                    result = true;
                    break;
                }

                if (cell.Level <= level)
                {
                    // The subtree of the level-"level" ancestor has ended.
                    break;
                }

                // Deeper rows (grandchildren and below) neither continue nor terminate the
                // level-"level" guide line; a direct child may still follow further down.
            }

            ancestorSiblingCache[key] = result;
            return result;
        }

        /// <summary>
        /// Returns the column that holds the <see cref="TreeGradeCell"/> of the specified row, or 0
        /// when the row has no tree cell.
        /// </summary>
        /// <param name="row">The row index.</param>
        /// <returns>The column of the tree cell, or 0.</returns>
        internal int TreeColumn(int row)
        {
            for (int c = 0; c < 8; c++)
            {
                if (TreeCells.ContainsKey((c, row)))
                {
                    return c;
                }
            }

            return 0;
        }

        /// <summary>
        /// Applies the collapse / expand rules of the account tree to <see cref="HiddenRows"/>.
        /// Collapsing a row hides every following row whose level is greater than the level of the
        /// clicked row; expanding restores each sub-group to its own expansion state.
        /// </summary>
        /// <param name="row">The row that was toggled.</param>
        /// <param name="expanded">The new expansion state of the row.</param>
        internal void ApplyTreeExpansion(int row, bool expanded)
        {
            if (TreeCells == null || !TreeCells.TryGetValue((TreeColumn(row), row), out TreeGradeCell cell) || cell == null)
            {
                return;
            }

            // A new expand / collapse supersedes one that is still running: snap the previous one
            // to its final state first, so that both never fight over the same rows.
            CompleteTreeRowAnimations();

            int level = cell.Level;
            int limit = Math.Min(MaxTableHeight, GetLastUsedRow() + 1);

            if (!expanded)
            {
                List<int> animatedRows = null;

                for (int r = row + 1; r <= limit; r++)
                {
                    if (!TreeCells.TryGetValue((TreeColumn(r), r), out TreeGradeCell child) || child == null || child.Level <= level)
                    {
                        break;
                    }

                    if (IsRowHidden(r))
                    {
                        // Already hidden (a collapsed deeper node): there is nothing to animate.
                        continue;
                    }

                    if (IsTreeAnimationEnabled)
                    {
                        animatedRows ??= new List<int>();
                        animatedRows.Add(r);
                    }
                    else
                    {
                        HiddenRows.Add(r);
                    }
                }

                if (animatedRows != null)
                {
                    StartTreeRowAnimation(animatedRows, true);
                }
            }
            else
            {
                List<int> animatedRows = null;
                bool childExpanded = true;

                for (int r = row + 1; r <= limit; r++)
                {
                    if (!TreeCells.TryGetValue((TreeColumn(r), r), out TreeGradeCell child) || child == null || child.Level <= level)
                    {
                        break;
                    }

                    if (child.Level == level + 1)
                    {
                        ShowTreeRowAnimated(r, ref animatedRows);
                        childExpanded = child.IsExpanded;
                    }
                    else if (childExpanded)
                    {
                        ShowTreeRowAnimated(r, ref animatedRows);
                    }
                }

                if (animatedRows != null)
                {
                    StartTreeRowAnimation(animatedRows, false);
                }
            }

            InvalidateTreeCache();
            this.InvalidateVisual();
        }

        /// <summary>
        /// Shows a single row as part of a tree expand: if it was hidden and the animation is
        /// enabled it grows from zero height instead of appearing at once.
        /// </summary>
        /// <param name="row">The row to show.</param>
        /// <param name="animatedRows">The list collecting the rows to animate (lazily created).</param>
        private void ShowTreeRowAnimated(int row, ref List<int> animatedRows)
        {
            bool wasHidden = HiddenRows.Contains(row);
            HiddenRows.Remove(row);

            if (wasHidden && IsTreeAnimationEnabled)
            {
                animatedRows ??= new List<int>();
                animatedRows.Add(row);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                mouseMoveShiftPressed = true;
                this.InvalidateVisual();
            }
            else if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl
                || e.Key == Key.LWin || e.Key == Key.RWin)
            {
                if (pointerPressedAction == 2 || this.Cursor == Cursors.MoveCursor((this.VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1))
                {
                    this.Cursor = this.Cursor = Cursors.MoveCopyCursor((this.VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1);
                }

                this.InvalidateVisual();
            }
            else if (e.Key == Key.Space && !this.Container.IsEditing)
            {
                // Pressing Space on a selected single link cell starts a link click (activated on Space-up).
                if (this.Selection.Count == 1 && this.Selection[0].Width == 0 && this.Selection[0].Height == 0 &&
                    this.LinkCells.TryGetValue((this.Selection[0].Left, this.Selection[0].Top), out LinkCell keyboardLink))
                {
                    pressedLinkCell = keyboardLink;
                    pressedLinkCellKey = (this.Selection[0].Left, this.Selection[0].Top);
                    keyboardLink.IsPressed = true;
                    this.InvalidateVisual();
                }
            }
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                mouseMoveShiftPressed = false;
                this.InvalidateVisual();
            }
            else if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl
                || e.Key == Key.LWin || e.Key == Key.RWin)
            {
                if (pointerPressedAction == 2 || this.Cursor == Cursors.MoveCopyCursor((this.VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1))
                {
                    this.Cursor = this.Cursor = Cursors.MoveCursor((this.VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1);
                }

                this.InvalidateVisual();
            }
            else if (e.Key == Key.Space && pressedLinkCell != null)
            {
                // Activate the link pressed with Space.
                LinkCell keyboardLink = pressedLinkCell;
                (int, int) keyboardLinkKey = pressedLinkCellKey;
                pressedLinkCell = null;
                keyboardLink.IsPressed = false;
                keyboardLink.IsVisited = true;
                this.Container.RaiseLinkClick(keyboardLinkKey.Item1, keyboardLinkKey.Item2, keyboardLink.LinkPara);
                this.InvalidateVisual();
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            PointerPoint currentPoint = e.GetCurrentPoint(this);
            // The frozen strips are drawn with their own (unscrolled) origin, so a pointer position
            // over them has to be translated using that origin instead of the scroll offset.
            double xPos = GetDrawnX(currentPoint.Position.X);
            double yPos = GetDrawnY(currentPoint.Position.Y);


            if (!currentPoint.Properties.IsLeftButtonPressed && !currentPoint.Properties.IsRightButtonPressed)
            {
                // Track the hover state of button cells: in icon mode the icon at the right edge
                // of the cell is highlighted while the pointer is over it (RefCell behaviour).
                // This runs before the early returns below so that the state stays consistent.
                if (TryHitButtonCell(xPos, yPos, out ButtonCell hoveredButton, out _))
                {
                    this.Cursor = HandCursor;

                    if (!ReferenceEquals(hoveredButtonCell, hoveredButton))
                    {
                        if (hoveredButtonCell != null)
                        {
                            hoveredButtonCell.IsHovered = false;
                        }

                        hoveredButtonCell = hoveredButton;
                        hoveredButtonCell.IsHovered = true;
                        this.InvalidateVisual();
                    }

                    return;
                }

                if (hoveredButtonCell != null)
                {
                    hoveredButtonCell.IsHovered = false;
                    hoveredButtonCell = null;
                    this.InvalidateVisual();
                }

                // Hovering over the +/- box of a tree cell shows a hand cursor.
                if (TryHitTreeBox(xPos, yPos, out TreeGradeCell hoveredTree, out _))
                {
                    this.Cursor = HandCursor;
                    return;
                }

                // Hovering over a link cell shows a hand cursor.
                if (TryHitLinkCell(xPos, yPos, out LinkCell hoveredLink, out _))
                {
                    this.Cursor = HandCursor;
                    return;
                }


                if (Selection.Count == 1)
                {
                    // Dragging the border of a selection moves its content, unless the worksheet
                    // forbids it: the border is then simply part of the selection.
                    bool canMove = this.Sheet == null || this.Sheet.AllowDragMoveCells;

                    double selectionLeft = 0;
                    double selectionTop = 0;
                    double selectionRight = 0;
                    double selectionBottom = 0;

                    // The frozen strips are drawn with their own origin, so the edges of a frozen
                    // column or row have to be resolved from there instead of the scrollable region.
                    selectionLeft = GetSelectionColumnEdge(Selection[0].Left, true);
                    selectionRight = GetSelectionColumnEdge(Selection[0].Right, false);
                    selectionTop = GetSelectionRowEdge(Selection[0].Top, true);
                    selectionBottom = GetSelectionRowEdge(Selection[0].Bottom, false);

                    bool found = canMove && ((Math.Abs(xPos - selectionLeft) <= 3 && yPos >= selectionTop && yPos <= selectionBottom) || (Math.Abs(xPos - selectionRight) <= 3 && yPos >= selectionTop && yPos <= selectionBottom) ||
                        (Math.Abs(yPos - selectionTop) <= 3 && xPos >= selectionLeft && xPos <= selectionRight) || (Math.Abs(yPos - selectionBottom) <= 3 && xPos >= selectionLeft && xPos <= selectionRight));

                    bool isRows = Selection[0].IsRows(this);
                    bool isColumns = Selection[0].IsColumns(this);

                    bool fill = false;

                    if (!isRows && !isColumns)
                    {
                        fill = Math.Abs(xPos - selectionRight) <= 3 && Math.Abs(yPos - selectionBottom) <= 3;
                    }
                    else if (isRows && !isColumns)
                    {
                        fill = Math.Abs(xPos - selectionLeft) <= 3 && Math.Abs(yPos - selectionBottom) <= 3;
                    }
                    else if (!isRows && isColumns)
                    {
                        fill = Math.Abs(xPos - selectionRight) <= 3 && Math.Abs(yPos - selectionTop) <= 3;
                    }

                    if (fill)
                    {
                        this.Cursor = Cursors.FillCursor((this.VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1);
                    }
                    else if (found)
                    {
                        if (e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier))
                        {
                            this.Cursor = Cursors.MoveCopyCursor((this.VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1);
                        }
                        else
                        {
                            this.Cursor = Cursors.MoveCursor((this.VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1);
                        }
                    }
                    else
                    {
                        this.Cursor = Cursors.CrossCursor((this.VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1);
                    }
                }
                else
                {
                    this.Cursor = Cursors.CrossCursor((this.VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1);
                }
            }
            else if (currentPoint.Properties.IsLeftButtonPressed)
            {
                int x = int.MinValue;
                int y = int.MinValue;

                for (int i = 0; i < lastDrawnXs.Length; i++)
                {
                    if (lastDrawnXs[i] > xPos && xPos >= (i == 0 ? 0 : lastDrawnXs[i - 1]))
                    {
                        x = i;
                        break;
                    }
                }

                for (int i = 0; i < lastDrawnYs.Length; i++)
                {
                    if (lastDrawnYs[i] > yPos && yPos >= (i == 0 ? 0 : lastDrawnYs[i - 1]))
                    {
                        y = i;
                        break;
                    }
                }

                if (x == int.MinValue)
                {
                    if (xPos < 0)
                    {
                        x = 0;
                    }
                    else
                    {
                        x = lastDrawnWidth;
                    }
                }

                if (y == int.MinValue)
                {
                    if (yPos < 0)
                    {
                        y = 0;
                    }
                    else
                    {
                        y = lastDrawnHeight;
                    }
                }

                // The frozen strips are drawn with their own coordinate origin, so the slot indices
                // computed above do not describe them: resolve the absolute cell indices instead.
                int columnIndex = x + lastDrawnLeft;
                int rowIndex = y + lastDrawnTop;

                if (IsOverFrozenColumns(currentPoint.Position.X))
                {
                    columnIndex = GetFrozenColumnIndex(currentPoint.Position.X);
                }

                if (IsOverFrozenRows(currentPoint.Position.Y))
                {
                    rowIndex = GetFrozenRowIndex(currentPoint.Position.Y);
                }

                bool shiftPressed = e.KeyModifiers.HasFlag(KeyModifiers.Shift);

                if (shiftPressed != mouseMoveShiftPressed)
                {
                    mouseMoveShiftPressed = shiftPressed;
                    this.InvalidateVisual();
                }

                if (pointerPressedAction == 1)
                {
                    this.Cursor = Cursors.CrossCursor((this.VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1);
                }
                else if (pointerPressedAction == 2)
                {
                    if (e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier))
                    {
                        this.Cursor = Cursors.MoveCopyCursor((this.VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1);
                    }
                    else
                    {
                        this.Cursor = Cursors.MoveCursor((this.VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1);
                    }
                }
                else if (pointerPressedAction == 3)
                {
                    this.Cursor = Cursors.FillCursor((this.VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1);
                }

                if (x >= 0 && y >= 0)
                {
                    if (pointerPressedAction == 1)
                    {
                        if (selectionMode == 0)
                        {
                            this.Container.Selection = this.Container.Selection.SetItem(this.Container.Selection.Count - 1, new SelectionRange(Math.Min(columnIndex, selectionStart.Item1), Math.Min(rowIndex, selectionStart.Item2), Math.Max(columnIndex, selectionStart.Item1), Math.Max(rowIndex, selectionStart.Item2)));
                        }
                        else if (selectionMode == 1)
                        {
                            this.Container.Selection = previousSelection.Difference(new SelectionRange(Math.Min(columnIndex, selectionStart.Item1), Math.Min(rowIndex, selectionStart.Item2), Math.Max(columnIndex, selectionStart.Item1), Math.Max(rowIndex, selectionStart.Item2)));
                        }
                    }
                    else if (pointerPressedAction == 2)
                    {
                        if (selectionBeingMoved.IsFinite(this))
                        {
                            (int, int) newSelectionMoveDelta = (Math.Max(-selectionStart.Item1 + columnIndex, -selectionBeingMoved.Left), Math.Max(-selectionStart.Item2 + rowIndex, -selectionBeingMoved.Top));

                            if (newSelectionMoveDelta != selectionMoveDelta)
                            {
                                selectionMoveDelta = newSelectionMoveDelta;
                                this.InvalidateVisual();
                            }
                        }
                        else if (selectionBeingMoved.IsRows(this) && !selectionBeingMoved.IsColumns(this))
                        {
                            (int, int) newSelectionMoveDelta = (0, Math.Max(-selectionStart.Item2 + rowIndex, -selectionBeingMoved.Top));

                            if (newSelectionMoveDelta != selectionMoveDelta)
                            {
                                selectionMoveDelta = newSelectionMoveDelta;
                                this.InvalidateVisual();
                            }
                        }
                        else if (selectionBeingMoved.IsColumns(this) && !selectionBeingMoved.IsRows(this))
                        {
                            (int, int) newSelectionMoveDelta = (Math.Max(-selectionStart.Item1 + columnIndex, -selectionBeingMoved.Left), 0);

                            if (newSelectionMoveDelta != selectionMoveDelta)
                            {
                                selectionMoveDelta = newSelectionMoveDelta;
                                this.InvalidateVisual();
                            }
                        }
                    }
                    else if (pointerPressedAction == 3)
                    {
                        if (selectionBeingMoved.IsFinite(this))
                        {
                            (int, int) newSelectionMoveDelta = (Math.Max(-selectionStart.Item1 + columnIndex, -selectionBeingMoved.Left - selectionBeingMoved.Width), Math.Max(-selectionStart.Item2 + rowIndex, -selectionBeingMoved.Top - selectionBeingMoved.Height));

                            if (newSelectionMoveDelta != selectionMoveDelta)
                            {
                                selectionMoveDelta = newSelectionMoveDelta;
                                this.InvalidateVisual();
                            }
                        }
                        else if (selectionBeingMoved.IsRows(this) && !selectionBeingMoved.IsColumns(this))
                        {
                            (int, int) newSelectionMoveDelta = (0, Math.Max(-selectionStart.Item2 + rowIndex, -selectionBeingMoved.Top - selectionBeingMoved.Height));

                            if (newSelectionMoveDelta != selectionMoveDelta)
                            {
                                selectionMoveDelta = newSelectionMoveDelta;
                                this.InvalidateVisual();
                            }
                        }
                        else if (selectionBeingMoved.IsColumns(this) && !selectionBeingMoved.IsRows(this))
                        {
                            (int, int) newSelectionMoveDelta = (Math.Max(-selectionStart.Item1 + columnIndex, -selectionBeingMoved.Left - selectionBeingMoved.Width), 0);

                            if (newSelectionMoveDelta != selectionMoveDelta)
                            {
                                selectionMoveDelta = newSelectionMoveDelta;
                                this.InvalidateVisual();
                            }
                        }
                    }
                }
            }

        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            // Release a pressed link: if the pointer is still over the link text, activate it.
            if (pressedLinkCell != null)
            {
                PointerPoint currentPoint = e.GetCurrentPoint(this);
                // The frozen strips are drawn with their own (unscrolled) origin, so a pointer
                // position over them has to be translated using that origin instead of the offset.
                double xPos = GetDrawnX(currentPoint.Position.X);
                double yPos = GetDrawnY(currentPoint.Position.Y);

                LinkCell releasedLink = pressedLinkCell;
                (int, int) releasedLinkKey = pressedLinkCellKey;
                pressedLinkCell = null;
                releasedLink.IsPressed = false;

                if (releasedLink.HitRect.Contains(new Point(xPos, yPos)))
                {
                    releasedLink.IsVisited = true;
                    this.Container.RaiseLinkClick(releasedLinkKey.Item1, releasedLinkKey.Item2, releasedLink.LinkPara);
                }

                this.InvalidateVisual();
            }

            // Release a pressed button cell: if the pointer is still over it, notify the host so
            // that it can open its own selection dialog.
            if (pressedButtonCell != null)
            {
                PointerPoint currentPoint = e.GetCurrentPoint(this);
                // The frozen strips are drawn with their own (unscrolled) origin, so a pointer
                // position over them has to be translated using that origin instead of the offset.
                double xPos = GetDrawnX(currentPoint.Position.X);
                double yPos = GetDrawnY(currentPoint.Position.Y);

                ButtonCell releasedButton = pressedButtonCell;
                (int, int) releasedButtonKey = pressedButtonCellKey;
                pressedButtonCell = null;
                releasedButton.IsPressed = false;

                if (releasedButton.HitRect.Contains(new Point(xPos, yPos)))
                {
                    this.Container?.RaiseCellTapped(releasedButtonKey.Item1, releasedButtonKey.Item2, releasedButton);
                }

                this.InvalidateVisual();
            }

            if (pointerPressedAction == 2 && (selectionMoveDelta.Item1 != 0 || selectionMoveDelta.Item2 != 0))
            {
                if (selectionBeingMoved.IsFinite(this))
                {
                    this.Data.Move(selectionBeingMoved, selectionMoveDelta.Item1, selectionMoveDelta.Item2, this.Container.UndoStack, e.KeyModifiers == Spreadsheet.ControlModifier);
                    this.CellForeground.Move(selectionBeingMoved, selectionMoveDelta.Item1, selectionMoveDelta.Item2, this.Container.UndoStackCellForeground, e.KeyModifiers == Spreadsheet.ControlModifier);
                    this.CellMargin.Move(selectionBeingMoved, selectionMoveDelta.Item1, selectionMoveDelta.Item2, this.Container.UndoStackCellMargin, e.KeyModifiers == Spreadsheet.ControlModifier);
                    this.CellTextAlignment.Move(selectionBeingMoved, selectionMoveDelta.Item1, selectionMoveDelta.Item2, this.Container.UndoStackCellHorizontalAlignment, e.KeyModifiers == Spreadsheet.ControlModifier);
                    this.CellTypefaces.Move(selectionBeingMoved, selectionMoveDelta.Item1, selectionMoveDelta.Item2, this.Container.UndoStackCellTypeface, e.KeyModifiers == Spreadsheet.ControlModifier);
                    this.CellVerticalAlignment.Move(selectionBeingMoved, selectionMoveDelta.Item1, selectionMoveDelta.Item2, this.Container.UndoStackCellVerticalAlignment, e.KeyModifiers == Spreadsheet.ControlModifier);

                    this.Container.UndoStackRowForeground.Push(null);
                    this.Container.UndoStackRowHeight.Push(null);
                    this.Container.UndoStackRowTypeface.Push(null);

                    this.Container.UndoStackColumnForeground.Push(null);
                    this.Container.UndoStackColumnWidth.Push(null);
                    this.Container.UndoStackColumnTypeface.Push(null);

                    this.Container.ClearRedoStack();

                    this.Container.Selection = ImmutableList.Create(new SelectionRange(selectionBeingMoved.Left + selectionMoveDelta.Item1, selectionBeingMoved.Top + selectionMoveDelta.Item2, selectionBeingMoved.Right + selectionMoveDelta.Item1, selectionBeingMoved.Bottom + selectionMoveDelta.Item2));
                }
                else if (selectionBeingMoved.IsRows(this) && !selectionBeingMoved.IsColumns(this))
                {
                    if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    {
                        this.Data.MoveRows(selectionBeingMoved.Top, selectionBeingMoved.Bottom, selectionMoveDelta.Item2, this.Container.UndoStack, e.KeyModifiers == Spreadsheet.ControlModifier);
                        this.CellForeground.MoveRows(selectionBeingMoved.Top, selectionBeingMoved.Bottom, selectionMoveDelta.Item2, this.Container.UndoStackCellForeground, e.KeyModifiers == Spreadsheet.ControlModifier);
                        this.CellMargin.MoveRows(selectionBeingMoved.Top, selectionBeingMoved.Bottom, selectionMoveDelta.Item2, this.Container.UndoStackCellMargin, e.KeyModifiers == Spreadsheet.ControlModifier);
                        this.CellTextAlignment.MoveRows(selectionBeingMoved.Top, selectionBeingMoved.Bottom, selectionMoveDelta.Item2, this.Container.UndoStackCellHorizontalAlignment, e.KeyModifiers == Spreadsheet.ControlModifier);
                        this.CellTypefaces.MoveRows(selectionBeingMoved.Top, selectionBeingMoved.Bottom, selectionMoveDelta.Item2, this.Container.UndoStackCellTypeface, e.KeyModifiers == Spreadsheet.ControlModifier);
                        this.CellVerticalAlignment.MoveRows(selectionBeingMoved.Top, selectionBeingMoved.Bottom, selectionMoveDelta.Item2, this.Container.UndoStackCellVerticalAlignment, e.KeyModifiers == Spreadsheet.ControlModifier);

                        this.RowForeground.Move(selectionBeingMoved.Top, selectionBeingMoved.Bottom, selectionMoveDelta.Item2, this.Container.UndoStackRowForeground, e.KeyModifiers == Spreadsheet.ControlModifier);
                        this.RowHeights.Move(selectionBeingMoved.Top, selectionBeingMoved.Bottom, selectionMoveDelta.Item2, this.Container.UndoStackRowHeight, e.KeyModifiers == Spreadsheet.ControlModifier);
                        this.RowTypefaces.Move(selectionBeingMoved.Top, selectionBeingMoved.Bottom, selectionMoveDelta.Item2, this.Container.UndoStackRowTypeface, e.KeyModifiers == Spreadsheet.ControlModifier);

                        this.Container.UndoStackColumnForeground.Push(null);
                        this.Container.UndoStackColumnWidth.Push(null);
                        this.Container.UndoStackColumnTypeface.Push(null);

                        this.Container.ClearRedoStack();

                        this.Container.Selection = ImmutableList.Create(new SelectionRange(selectionBeingMoved.Left + selectionMoveDelta.Item1, selectionBeingMoved.Top + selectionMoveDelta.Item2, selectionBeingMoved.Right + selectionMoveDelta.Item1, selectionBeingMoved.Bottom + selectionMoveDelta.Item2));
                    }
                    else if (selectionMoveDelta.Item2 < 0 || selectionMoveDelta.Item2 > selectionBeingMoved.Height)
                    {
                        this.Data = this.Data.MoveInsertDeleteRows(selectionBeingMoved.Top, selectionBeingMoved.Bottom, selectionMoveDelta.Item2, this.Container.UndoStack, e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier));
                        this.CellForeground = this.CellForeground.MoveInsertDeleteRows(selectionBeingMoved.Top, selectionBeingMoved.Bottom, selectionMoveDelta.Item2, this.Container.UndoStackCellForeground, e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier));
                        this.CellMargin = this.CellMargin.MoveInsertDeleteRows(selectionBeingMoved.Top, selectionBeingMoved.Bottom, selectionMoveDelta.Item2, this.Container.UndoStackCellMargin, e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier));
                        this.CellTextAlignment = this.CellTextAlignment.MoveInsertDeleteRows(selectionBeingMoved.Top, selectionBeingMoved.Bottom, selectionMoveDelta.Item2, this.Container.UndoStackCellHorizontalAlignment, e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier));
                        this.CellTypefaces = this.CellTypefaces.MoveInsertDeleteRows(selectionBeingMoved.Top, selectionBeingMoved.Bottom, selectionMoveDelta.Item2, this.Container.UndoStackCellTypeface, e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier));
                        this.CellVerticalAlignment = this.CellVerticalAlignment.MoveInsertDeleteRows(selectionBeingMoved.Top, selectionBeingMoved.Bottom, selectionMoveDelta.Item2, this.Container.UndoStackCellVerticalAlignment, e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier));

                        this.RowForeground = this.RowForeground.MoveInsertDelete(selectionBeingMoved.Top, selectionBeingMoved.Bottom, selectionMoveDelta.Item2, selectionBeingMoved, this.Container.UndoStackRowForeground, e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier));
                        this.RowHeights = this.RowHeights.MoveInsertDelete(selectionBeingMoved.Top, selectionBeingMoved.Bottom, selectionMoveDelta.Item2, selectionBeingMoved, this.Container.UndoStackRowHeight, e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier));
                        this.RowTypefaces = this.RowTypefaces.MoveInsertDelete(selectionBeingMoved.Top, selectionBeingMoved.Bottom, selectionMoveDelta.Item2, selectionBeingMoved, this.Container.UndoStackRowTypeface, e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier));

                        this.Container.UndoStackColumnForeground.Push(null);
                        this.Container.UndoStackColumnWidth.Push(null);
                        this.Container.UndoStackColumnTypeface.Push(null);

                        this.Container.ClearRedoStack();

                        if (e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier))
                        {
                            this.Container.Selection = ImmutableList.Create(new SelectionRange(selectionBeingMoved.Left + selectionMoveDelta.Item1, selectionBeingMoved.Top + selectionMoveDelta.Item2, selectionBeingMoved.Right + selectionMoveDelta.Item1, selectionBeingMoved.Bottom + selectionMoveDelta.Item2));
                        }
                        else if (selectionMoveDelta.Item2 > 0)
                        {
                            this.Container.Selection = ImmutableList.Create(new SelectionRange(selectionBeingMoved.Left + selectionMoveDelta.Item1, selectionBeingMoved.Top + selectionMoveDelta.Item2 - selectionBeingMoved.Height, selectionBeingMoved.Right + selectionMoveDelta.Item1, selectionBeingMoved.Bottom + selectionMoveDelta.Item2 - selectionBeingMoved.Height));
                        }
                        else
                        {
                            this.Container.Selection = ImmutableList.Create(new SelectionRange(selectionBeingMoved.Left + selectionMoveDelta.Item1, selectionBeingMoved.Top + selectionMoveDelta.Item2, selectionBeingMoved.Right + selectionMoveDelta.Item1, selectionBeingMoved.Bottom + selectionMoveDelta.Item2));
                        }
                    }
                }
                else if (selectionBeingMoved.IsColumns(this) && !selectionBeingMoved.IsRows(this))
                {
                    if (!e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                    {
                        this.Data.MoveColumns(selectionBeingMoved.Left, selectionBeingMoved.Right, selectionMoveDelta.Item1, this.Container.UndoStack, e.KeyModifiers == Spreadsheet.ControlModifier);
                        this.CellForeground.MoveColumns(selectionBeingMoved.Left, selectionBeingMoved.Right, selectionMoveDelta.Item1, this.Container.UndoStackCellForeground, e.KeyModifiers == Spreadsheet.ControlModifier);
                        this.CellMargin.MoveColumns(selectionBeingMoved.Left, selectionBeingMoved.Right, selectionMoveDelta.Item1, this.Container.UndoStackCellMargin, e.KeyModifiers == Spreadsheet.ControlModifier);
                        this.CellTextAlignment.MoveColumns(selectionBeingMoved.Left, selectionBeingMoved.Right, selectionMoveDelta.Item1, this.Container.UndoStackCellHorizontalAlignment, e.KeyModifiers == Spreadsheet.ControlModifier);
                        this.CellTypefaces.MoveColumns(selectionBeingMoved.Left, selectionBeingMoved.Right, selectionMoveDelta.Item1, this.Container.UndoStackCellTypeface, e.KeyModifiers == Spreadsheet.ControlModifier);
                        this.CellVerticalAlignment.MoveColumns(selectionBeingMoved.Left, selectionBeingMoved.Right, selectionMoveDelta.Item1, this.Container.UndoStackCellVerticalAlignment, e.KeyModifiers == Spreadsheet.ControlModifier);

                        this.ColumnForeground.Move(selectionBeingMoved.Left, selectionBeingMoved.Right, selectionMoveDelta.Item1, this.Container.UndoStackColumnForeground, e.KeyModifiers == Spreadsheet.ControlModifier);
                        this.ColumnWidths.Move(selectionBeingMoved.Left, selectionBeingMoved.Right, selectionMoveDelta.Item1, this.Container.UndoStackColumnWidth, e.KeyModifiers == Spreadsheet.ControlModifier);
                        this.ColumnTypefaces.Move(selectionBeingMoved.Left, selectionBeingMoved.Right, selectionMoveDelta.Item1, this.Container.UndoStackColumnTypeface, e.KeyModifiers == Spreadsheet.ControlModifier);

                        this.Container.UndoStackRowForeground.Push(null);
                        this.Container.UndoStackRowHeight.Push(null);
                        this.Container.UndoStackRowTypeface.Push(null);

                        this.Container.ClearRedoStack();

                        this.Container.Selection = ImmutableList.Create(new SelectionRange(selectionBeingMoved.Left + selectionMoveDelta.Item1, selectionBeingMoved.Top + selectionMoveDelta.Item2, selectionBeingMoved.Right + selectionMoveDelta.Item1, selectionBeingMoved.Bottom + selectionMoveDelta.Item2));
                    }
                    else if (selectionMoveDelta.Item1 < 0 || selectionMoveDelta.Item1 > selectionBeingMoved.Width)
                    {
                        this.Data = this.Data.MoveInsertDeleteColumns(selectionBeingMoved.Left, selectionBeingMoved.Right, selectionMoveDelta.Item1, this.Container.UndoStack, e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier));
                        this.CellForeground = this.CellForeground.MoveInsertDeleteColumns(selectionBeingMoved.Left, selectionBeingMoved.Right, selectionMoveDelta.Item1, this.Container.UndoStackCellForeground, e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier));
                        this.CellMargin = this.CellMargin.MoveInsertDeleteColumns(selectionBeingMoved.Left, selectionBeingMoved.Right, selectionMoveDelta.Item1, this.Container.UndoStackCellMargin, e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier));
                        this.CellTextAlignment = this.CellTextAlignment.MoveInsertDeleteColumns(selectionBeingMoved.Left, selectionBeingMoved.Right, selectionMoveDelta.Item1, this.Container.UndoStackCellHorizontalAlignment, e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier));
                        this.CellTypefaces = this.CellTypefaces.MoveInsertDeleteColumns(selectionBeingMoved.Left, selectionBeingMoved.Right, selectionMoveDelta.Item1, this.Container.UndoStackCellTypeface, e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier));
                        this.CellVerticalAlignment = this.CellVerticalAlignment.MoveInsertDeleteColumns(selectionBeingMoved.Left, selectionBeingMoved.Right, selectionMoveDelta.Item1, this.Container.UndoStackCellVerticalAlignment, e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier));

                        this.ColumnForeground = this.ColumnForeground.MoveInsertDelete(selectionBeingMoved.Left, selectionBeingMoved.Right, selectionMoveDelta.Item1, selectionBeingMoved, this.Container.UndoStackColumnForeground, e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier));
                        this.ColumnWidths = this.ColumnWidths.MoveInsertDelete(selectionBeingMoved.Left, selectionBeingMoved.Right, selectionMoveDelta.Item1, selectionBeingMoved, this.Container.UndoStackColumnWidth, e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier));
                        this.ColumnTypefaces = this.ColumnTypefaces.MoveInsertDelete(selectionBeingMoved.Left, selectionBeingMoved.Right, selectionMoveDelta.Item1, selectionBeingMoved, this.Container.UndoStackColumnTypeface, e.KeyModifiers.HasFlag(Spreadsheet.ControlModifier));

                        this.Container.UndoStackRowForeground.Push(null);
                        this.Container.UndoStackRowHeight.Push(null);
                        this.Container.UndoStackRowTypeface.Push(null);

                        this.Container.ClearRedoStack();
                    }
                }
            }
            else if (pointerPressedAction == 3 && (selectionMoveDelta.Item1 != 0 || selectionMoveDelta.Item2 != 0))
            {
                bool isRows = selectionBeingMoved.IsRows(this);
                bool isColumns = selectionBeingMoved.IsColumns(this);

                if (isRows || isColumns)
                {
                    selectionBeingMoved = selectionBeingMoved.Intersection(Data.SelectAll(this));
                }

                if ((Math.Abs(selectionMoveDelta.Item1) > Math.Abs(selectionMoveDelta.Item2) || isColumns && !isRows) && !(isRows && !isColumns))
                {
                    if (selectionMoveDelta.Item1 > 0)
                    {
                        this.Data.FillRight(selectionBeingMoved, selectionMoveDelta.Item1, this.Container.UndoStack);
                        this.Container.PushNonDataStackNull();
                        this.Container.ClearRedoStack();
                        this.Container.Selection = ImmutableList.Create(new SelectionRange(selectionBeingMoved.Left, selectionBeingMoved.Top, selectionBeingMoved.Right + selectionMoveDelta.Item1, selectionBeingMoved.Bottom));
                    }
                    else if (selectionMoveDelta.Item1 > -selectionBeingMoved.Width)
                    {
                        this.Data = this.Data.Remove(new SelectionRange(selectionBeingMoved.Right + selectionMoveDelta.Item1 + 1, selectionBeingMoved.Top, selectionBeingMoved.Right, selectionBeingMoved.Bottom), this.Container.UndoStack);
                        this.Container.UndoStack.Peek().Selection = ImmutableList.Create(selectionBeingMoved);

                        this.Container.PushNonDataStackNull();
                        this.Container.ClearRedoStack();

                        this.Container.Selection = ImmutableList.Create(new SelectionRange(selectionBeingMoved.Left, selectionBeingMoved.Top, selectionBeingMoved.Right + selectionMoveDelta.Item1, selectionBeingMoved.Bottom));
                    }
                    else
                    {
                        this.Data.FillLeft(selectionBeingMoved, -selectionBeingMoved.Width - selectionMoveDelta.Item1 + 1, this.Container.UndoStack);
                        this.Container.PushNonDataStackNull();
                        this.Container.ClearRedoStack();
                        this.Container.Selection = ImmutableList.Create(new SelectionRange(selectionBeingMoved.Left + selectionMoveDelta.Item1 + selectionBeingMoved.Width - 1, selectionBeingMoved.Top, selectionBeingMoved.Right, selectionBeingMoved.Bottom));
                    }
                }
                else
                {
                    if (selectionMoveDelta.Item2 > 0)
                    {
                        this.Data.FillBottom(selectionBeingMoved, selectionMoveDelta.Item2, this.Container.UndoStack, HiddenRows.Count > 0 ? HiddenRows : null);
                        this.Container.PushNonDataStackNull();
                        this.Container.ClearRedoStack();
                        this.Container.Selection = ImmutableList.Create(new SelectionRange(selectionBeingMoved.Left, selectionBeingMoved.Top, selectionBeingMoved.Right, selectionBeingMoved.Bottom + selectionMoveDelta.Item2));
                    }
                    else if (selectionMoveDelta.Item2 > -selectionBeingMoved.Height)
                    {
                        this.Data = this.Data.Remove(new SelectionRange(selectionBeingMoved.Left, selectionBeingMoved.Bottom + selectionMoveDelta.Item2 + 1, selectionBeingMoved.Right, selectionBeingMoved.Bottom), this.Container.UndoStack);
                        this.Container.UndoStack.Peek().Selection = ImmutableList.Create(selectionBeingMoved);

                        this.Container.PushNonDataStackNull();
                        this.Container.ClearRedoStack();
                        this.Container.Selection = ImmutableList.Create(new SelectionRange(selectionBeingMoved.Left, selectionBeingMoved.Top, selectionBeingMoved.Right, selectionBeingMoved.Bottom + selectionMoveDelta.Item2));
                    }
                    else
                    {
                        this.Data.FillTop(selectionBeingMoved, -selectionBeingMoved.Height - selectionMoveDelta.Item2 + 1, this.Container.UndoStack, HiddenRows.Count > 0 ? HiddenRows : null);
                        this.Container.PushNonDataStackNull();
                        this.Container.ClearRedoStack();
                        this.Container.Selection = ImmutableList.Create(new SelectionRange(selectionBeingMoved.Left, selectionBeingMoved.Top + selectionMoveDelta.Item2 + selectionBeingMoved.Height - 1, selectionBeingMoved.Right, selectionBeingMoved.Bottom));
                    }
                }
            }


            pointerPressedAction = 0;
            this.InvalidateVisual();
        }

        int pointerPressedAction = 0;
        (int, int) selectionStart = (-1, -1);
        int selectionMode = 0;
        (int, int) selectionMoveDelta = (-1, -1);
        ImmutableList<SelectionRange> previousSelection;
        SelectionRange selectionBeingMoved;
        bool mouseMoveShiftPressed = false;
        LinkCell pressedLinkCell = null;
        (int, int) pressedLinkCellKey = (-1, -1);
        ButtonCell pressedButtonCell = null;
        (int, int) pressedButtonCellKey = (-1, -1);
        ButtonCell hoveredButtonCell = null;
        private static readonly Cursor HandCursor = new Cursor(StandardCursorType.Hand);

        /// <summary>
        /// Returns the <see cref="LinkCell"/> whose hit rectangle contains the given pointer
        /// position. The hit rectangles are updated every time the table is drawn, so only visible
        /// link cells can be hit. Depending on <see cref="LinkCell.HitMode"/> the rectangle covers
        /// the rendered text only or the whole cell.
        /// </summary>
        private bool TryHitLinkCell(double xPos, double yPos, out LinkCell linkCell, out (int, int) key)
        {
            linkCell = null;
            key = (-1, -1);

            if (this.LinkCells == null || this.LinkCells.Count == 0)
            {
                return false;
            }

            Point p = new Point(xPos, yPos);

            foreach (KeyValuePair<(int, int), LinkCell> kvp in this.LinkCells)
            {
                if (kvp.Value != null && kvp.Value.HitRect.Contains(p))
                {
                    linkCell = kvp.Value;
                    key = kvp.Key;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Clears the hover state of a button cell when the pointer leaves the table.
        /// </summary>
        protected override void OnPointerExited(PointerEventArgs e)
        {
            if (hoveredButtonCell != null)
            {
                hoveredButtonCell.IsHovered = false;
                hoveredButtonCell = null;
                this.InvalidateVisual();
            }

            base.OnPointerExited(e);
        }

        /// <summary>
        /// Returns the <see cref="ButtonCell"/> that contains the given pointer position. The hit
        /// rectangles are updated every time the table is drawn, so only visible button cells can
        /// be hit.
        /// </summary>
        private bool TryHitButtonCell(double xPos, double yPos, out ButtonCell buttonCell, out (int, int) key)
        {
            return TryHitButtonCell(new Point(xPos, yPos), out buttonCell, out key);
        }

        /// <summary>
        /// Returns the <see cref="ButtonCell"/> that contains the given point.
        /// </summary>
        private bool TryHitButtonCell(Point p, out ButtonCell buttonCell, out (int, int) key)
        {
            buttonCell = null;
            key = (-1, -1);

            if (this.ButtonCells == null || this.ButtonCells.Count == 0)
            {
                return false;
            }

            foreach (KeyValuePair<(int, int), ButtonCell> kvp in this.ButtonCells)
            {
                if (kvp.Value != null && kvp.Value.HitRect.Contains(p))
                {
                    buttonCell = kvp.Value;
                    key = kvp.Key;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether the cell at the specified coordinates is a <see cref="ButtonCell"/> whose
        /// <see cref="ButtonCell.SuppressEdit"/> flag is set, i.e. a cell whose value is chosen
        /// from an external dialog rather than typed by the user.
        /// </summary>
        internal bool IsCellEditSuppressed(int column, int row)
        {
            return this.ButtonCells != null &&
                   this.ButtonCells.TryGetValue((column, row), out ButtonCell cell) &&
                   cell != null &&
                   cell.SuppressEdit;
        }

        /// <summary>
        /// Returns the <see cref="TreeGradeCell"/> whose +/- box contains the given pointer
        /// position. The hit rectangles are updated every time the table is drawn, so only visible
        /// tree cells can be hit.
        /// </summary>
        private bool TryHitTreeBox(double xPos, double yPos, out TreeGradeCell treeCell, out (int, int) key)
        {
            treeCell = null;
            key = (-1, -1);

            if (this.TreeCells == null || this.TreeCells.Count == 0)
            {
                return false;
            }

            Point p = new Point(xPos, yPos);

            foreach (KeyValuePair<(int, int), TreeGradeCell> kvp in this.TreeCells)
            {
                if (kvp.Value != null && kvp.Value.HasBox && kvp.Value.BoxHitRect.Contains(p))
                {
                    treeCell = kvp.Value;
                    key = kvp.Key;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Toggles a tree cell: flips its <see cref="TreeGradeCell.IsExpanded"/> flag, applies the
        /// collapse / expand rules when <see cref="AutoManageRows"/> is set, and raises the
        /// <see cref="Spreadsheet.TreeExpand"/> event.
        /// </summary>
        /// <param name="column">The column of the tree cell.</param>
        /// <param name="row">The row of the tree cell.</param>
        /// <param name="cell">The tree cell to toggle.</param>
        internal void ToggleTreeCell(int column, int row, TreeGradeCell cell)
        {
            if (cell == null)
            {
                return;
            }

            cell.IsExpanded = !cell.IsExpanded;

            if (AutoManageRows)
            {
                ApplyTreeExpansion(row, cell.IsExpanded);
            }

            this.Container?.RaiseTreeExpand(column, row, cell, cell.IsExpanded);
            this.InvalidateVisual();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            PointerPoint currentPoint = e.GetCurrentPoint(this);
            // The frozen strips are drawn with their own (unscrolled) origin, so a pointer position
            // over them has to be translated using that origin instead of the scroll offset.
            double xPos = GetDrawnX(currentPoint.Position.X);
            double yPos = GetDrawnY(currentPoint.Position.Y);

            bool moving = false;
            bool fill = false;

            if (Selection.Count == 1)
            {
                double selectionLeft = 0;
                double selectionTop = 0;
                double selectionRight = 0;
                double selectionBottom = 0;

                // The frozen strips are drawn with their own origin, so the edges of a frozen column
                // or row have to be resolved from there instead of the scrollable region.
                selectionLeft = GetSelectionColumnEdge(Selection[0].Left, true);
                selectionRight = GetSelectionColumnEdge(Selection[0].Right, false);
                selectionTop = GetSelectionRowEdge(Selection[0].Top, true);
                selectionBottom = GetSelectionRowEdge(Selection[0].Bottom, false);

                // A worksheet can forbid the user from moving the content of a selection by dragging
                // its border; the pointer then starts a new selection instead.
                bool canMove = this.Sheet == null || this.Sheet.AllowDragMoveCells;

                moving = canMove && ((Math.Abs(xPos - selectionLeft) <= 3 && yPos >= selectionTop && yPos <= selectionBottom) || (Math.Abs(xPos - selectionRight) <= 3 && yPos >= selectionTop && yPos <= selectionBottom) ||
                    (Math.Abs(yPos - selectionTop) <= 3 && xPos >= selectionLeft && xPos <= selectionRight) || (Math.Abs(yPos - selectionBottom) <= 3 && xPos >= selectionLeft && xPos <= selectionRight));

                bool isRows = Selection[0].IsRows(this);
                bool isColumns = Selection[0].IsColumns(this);

                if (!isRows && !isColumns)
                {
                    fill = Math.Abs(xPos - selectionRight) <= 3 && Math.Abs(yPos - selectionBottom) <= 3;
                }
                else if (isRows && !isColumns)
                {
                    fill = Math.Abs(xPos - selectionLeft) <= 3 && Math.Abs(yPos - selectionBottom) <= 3;
                }
                else if (!isRows && isColumns)
                {
                    fill = Math.Abs(xPos - selectionRight) <= 3 && Math.Abs(yPos - selectionTop) <= 3;
                }
            }

            int x = -1;
            int y = -1;

            for (int i = 0; i < lastDrawnXs.Length; i++)
            {
                if (lastDrawnXs[i] > xPos && xPos >= (i == 0 ? 0 : lastDrawnXs[i - 1]))
                {
                    x = i;
                    break;
                }
            }

            for (int i = 0; i < lastDrawnYs.Length; i++)
            {
                if (lastDrawnYs[i] > yPos && yPos >= (i == 0 ? 0 : lastDrawnYs[i - 1]))
                {
                    y = i;
                    break;
                }
            }

            // The frozen strips are drawn with their own coordinate origin, so the slot indices
            // computed above do not describe them: resolve the absolute cell indices instead.
            int columnIndex = x + lastDrawnLeft;
            int rowIndex = y + lastDrawnTop;

            if (IsOverFrozenColumns(currentPoint.Position.X))
            {
                columnIndex = GetFrozenColumnIndex(currentPoint.Position.X);
            }

            if (IsOverFrozenRows(currentPoint.Position.Y))
            {
                rowIndex = GetFrozenRowIndex(currentPoint.Position.Y);
            }

            if (fill)
            {
                selectionStart = (Math.Max(Selection[0].Left, Math.Min(Selection[0].Right, columnIndex)), Math.Max(Selection[0].Top, Math.Min(Selection[0].Bottom, rowIndex)));

                selectionMoveDelta = (0, 0);
                selectionBeingMoved = Selection[0];
                pointerPressedAction = 3;
                this.InvalidateVisual();
            }
            else if (moving)
            {
                selectionStart = (Math.Max(Selection[0].Left, Math.Min(Selection[0].Right, columnIndex)), Math.Max(Selection[0].Top, Math.Min(Selection[0].Bottom, rowIndex)));


                selectionMoveDelta = (0, 0);
                selectionBeingMoved = Selection[0];
                pointerPressedAction = 2;
                this.InvalidateVisual();
            }
            else
            {
                // If the pressed cell belongs to a merged range, redirect the interaction to the
                // top-left cell of the merge so that selection and editing act on the whole range.
                SelectionRange pressedSelection = new SelectionRange(columnIndex, rowIndex, columnIndex, rowIndex);

                if (x >= 0 && y >= 0 && MergedRanges != null && MergedRanges.Count > 0)
                {
                    int pressedColumn = columnIndex;
                    int pressedRow = rowIndex;

                    foreach (SelectionRange mergedRange in MergedRanges)
                    {
                        if (pressedColumn >= mergedRange.Left && pressedColumn <= mergedRange.Right &&
                            pressedRow >= mergedRange.Top && pressedRow <= mergedRange.Bottom)
                        {
                            x = mergedRange.Left - lastDrawnLeft;
                            y = mergedRange.Top - lastDrawnTop;
                            columnIndex = mergedRange.Left;
                            rowIndex = mergedRange.Top;
                            pressedSelection = mergedRange;
                            break;
                        }
                    }
                }

                if (currentPoint.Properties.IsLeftButtonPressed && e.ClickCount == 1)
                {
                    // Clicking the +/- box of a tree cell collapses / expands the node instead of
                    // starting a selection or an edit.
                    if (TryHitTreeBox(xPos, yPos, out TreeGradeCell pressedTree, out (int, int) pressedTreeKey))
                    {
                        ToggleTreeCell(pressedTreeKey.Item1, pressedTreeKey.Item2, pressedTree);
                        pointerPressedAction = 0;
                        return;
                    }

                    // Pressing inside a link text area starts a link click (activated on release).
                    if (TryHitLinkCell(xPos, yPos, out LinkCell pressedLink, out (int, int) pressedLinkKey))
                    {
                        pressedLinkCell = pressedLink;
                        pressedLinkCellKey = pressedLinkKey;
                        pressedLink.IsPressed = true;
                        this.InvalidateVisual();
                    }

                    // Pressing a button cell arms it; the CellTapped event is raised on release so
                    // that a click can still be cancelled by dragging away from the button.
                    if (TryHitButtonCell(xPos, yPos, out ButtonCell pressedButton, out (int, int) pressedButtonKey))
                    {
                        pressedButtonCell = pressedButton;
                        pressedButtonCellKey = pressedButtonKey;
                        pressedButton.IsPressed = true;
                        this.InvalidateVisual();
                    }

                    if (e.KeyModifiers == Spreadsheet.ControlModifier)
                    {
                        bool found = false;

                        for (int i = 0; i < this.Selection.Count; i++)
                        {
                            if (this.Selection[i].Contains(x, y))
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            this.Container.Selection = this.Container.Selection.Add(pressedSelection);
                            selectionStart = (columnIndex, rowIndex);
                            selectionMode = 0;
                        }
                        else
                        {
                            previousSelection = this.Container.Selection;
                            this.Container.Selection = this.Container.Selection.Difference(pressedSelection);
                            selectionStart = (columnIndex, rowIndex);
                            selectionMode = 1;
                        }
                    }
                    else if (e.KeyModifiers == KeyModifiers.Shift)
                    {
                        int prevStartColumn = this.Selection[this.Selection.Count - 1].Left;
                        int prevEndColumn = this.Selection[this.Selection.Count - 1].Right;

                        int prevStartRow = this.Selection[this.Selection.Count - 1].Top;
                        int prevEndRow = this.Selection[this.Selection.Count - 1].Bottom;

                        int selStartX;

                        if (columnIndex >= prevStartColumn)
                        {
                            selStartX = prevStartColumn;
                        }
                        else
                        {
                            selStartX = prevEndColumn;
                        }

                        int selStartY;

                        if (rowIndex >= prevStartRow)
                        {
                            selStartY = prevStartRow;
                        }
                        else
                        {
                            selStartY = prevEndRow;
                        }

                        selectionStart = (selStartX, selStartY);
                        this.Container.Selection = this.Selection.SetItem(this.Selection.Count - 1, new SelectionRange(Math.Min(selStartX, columnIndex), Math.Min(selStartY, rowIndex), Math.Max(selStartX, columnIndex), Math.Max(selStartY, rowIndex)));
                        selectionMode = 0;
                    }
                    else
                    {
                        this.Container.Selection = ImmutableList.Create(pressedSelection);
                        selectionStart = (columnIndex, rowIndex);
                        selectionMode = 0;
                    }

                    pointerPressedAction = 1;
                }
                else if (currentPoint.Properties.IsRightButtonPressed)
                {
                    bool found = false;

                    for (int i = 0; i < this.Selection.Count; i++)
                    {
                        if (this.Selection[i].Contains(x, y))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        this.Container.Selection = ImmutableList.Create(pressedSelection);
                    }

                    selectionStart = (columnIndex, rowIndex);
                    selectionMode = 0;
                    pointerPressedAction = 0;
                }
                else if (currentPoint.Properties.IsLeftButtonPressed && e.ClickCount == 2)
                {
                    (int, int) cell = (columnIndex, rowIndex);

                    // Double clicking the +/- box must not start an edit either.
                    if (TryHitTreeBox(xPos, yPos, out TreeGradeCell doubleClickedTree, out (int, int) doubleClickedTreeKey))
                    {
                        pointerPressedAction = 0;
                        return;
                    }

                    // Double clicking a button cell raises CellDoubleTapped instead of opening the
                    // editing box.
                    if (TryHitButtonCell(xPos, yPos, out ButtonCell doubleClickedButton, out (int, int) doubleClickedButtonKey))
                    {
                        pointerPressedAction = 0;
                        this.Container?.RaiseCellDoubleTapped(doubleClickedButtonKey.Item1, doubleClickedButtonKey.Item2, doubleClickedButton);
                        return;
                    }

                    // Double clicking a link cell must not open the editing box either: the cell is
                    // a navigation entry point, and the value can still be typed over (or edited
                    // with F2) when that is what the user wants. The click itself has already been
                    // raised by the first click of the double click.
                    if (TryHitLinkCell(xPos, yPos, out _, out _))
                    {
                        pointerPressedAction = 0;
                        return;
                    }

                    Color? clickedColor = null;

                    if (this.Container.ShowColorPreview && this.Data.TryGetValue(cell, out string txt) && txt.StartsWith("#") && (txt.Length == 7 || txt.Length == 9))
                    {
                        if (!CellMargin.TryGetValue(cell, out Thickness margin))
                        {
                            margin = this.DefaultMargin;
                        }

                        double realX = x == 0 ? 0 : lastDrawnXs[x - 1];

                        if (!CellTypefaces.TryGetValue(cell, out Typeface face) &&
                                            !RowTypefaces.TryGetValue(cell.Item2, out face) &&
                                            !ColumnTypefaces.TryGetValue(cell.Item1, out face))
                        {
                            face = new Typeface(this.FontFamily, this.FontStyle, this.FontWeight);
                        }

                        if (!CellTextAlignment.TryGetValue(cell, out TextAlignment hor))
                        {
                            hor = this.DefaultTextAlignment;
                        }

                        FormattedText fmtText = new FormattedText(txt, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, face, this.FontSize, Foreground);

                        double textWidth = fmtText.Width;

                        if (hor == TextAlignment.Left)
                        {
                            realX += margin.Left;
                        }
                        else if (hor == TextAlignment.Center)
                        {
                            realX = (realX + margin.Left + lastDrawnXs[x] - margin.Right) * 0.5 - textWidth * 0.5;
                        }
                        else if (hor == TextAlignment.Right)
                        {
                            realX = lastDrawnXs[x] - margin.Right - textWidth;
                        }

                        if (xPos >= realX && xPos <= realX + this.FontSize)
                        {
                            try
                            {
                                SolidColorBrush colourBrush = SolidColorBrush.Parse(txt.Length == 7 ? txt : ("#" + txt.Substring(7, 2) + txt.Substring(1, 6)));
                                clickedColor = colourBrush.Color;
                            }
                            catch { }
                        }
                    }

                    if (clickedColor != null)
                    {
                        if (!this.Container.RaiseColorDoubleTapped(cell, clickedColor.Value))
                        {
                            this.Container.EditingCell = cell;
                            this.Container.IsEditing = true;
                        }
                    }
                    else
                    {
                        this.Container.EditingCell = cell;
                        this.Container.IsEditing = true;
                    }
                }


            }
        }

        internal double[] lastDrawnXs = null;
        internal double[] lastDrawnYs = null;
        internal int lastDrawnLeft = -1;
        internal int lastDrawnTop = -1;
        internal int lastDrawnWidth = -1;
        internal int lastDrawnHeight = -1;
        internal double lastDrawnDeltaX = 0;
        internal double lastDrawnDeltaY = 0;

        protected override void OnLoaded(RoutedEventArgs e)
        {
            (int left, _, int top, _, int width, double actualWidth, double startWidth, int height, double actualHeight, double startHeight) = GetRange(Offset.X, this.Offset.Y, this.Bounds.Width, this.Bounds.Height);
            Container.SetScrollbarMaximum(actualWidth + startWidth - this.Bounds.Width + GetWidth(left + width), actualHeight + startHeight - this.Bounds.Height + GetHeight(top + height));

            base.OnLoaded(e);
        }

        public override void Render(DrawingContext context)
        {
            context.FillRectangle(this.Background, new Rect(0, 0, this.Bounds.Width, this.Bounds.Height));

            double viewWidth = this.Bounds.Width;
            double viewHeight = this.Bounds.Height;

            frozenStripWidth = GetClampedFrozenWidth(viewWidth);
            frozenStripHeight = GetClampedFrozenHeight(viewHeight);

            // Discard the hit rectangles of the button cells before anything is drawn: only the ones
            // that are actually drawn by one of the regions below get a new one, so that a button
            // that has been scrolled out of view can no longer be clicked.
            if (ButtonCells != null && ButtonCells.Count > 0)
            {
                foreach (KeyValuePair<(int, int), ButtonCell> kvp in ButtonCells)
                {
                    if (kvp.Value != null)
                    {
                        kvp.Value.HitRect = default;
                    }
                }
            }

            // The same for the link cells. This matters more than for buttons because the hit
            // rectangle of a link cell can now cover the whole cell (LinkCell.HitMode), so a stale
            // rectangle of a cell that has been scrolled out of view would swallow the clicks of
            // whatever is drawn at that place now.
            if (LinkCells != null && LinkCells.Count > 0)
            {
                foreach (KeyValuePair<(int, int), LinkCell> kvp in LinkCells)
                {
                    if (kvp.Value != null)
                    {
                        kvp.Value.HitRect = default;
                    }
                }
            }

            using (context.PushTransform(Matrix.CreateTranslation(-1, -1)))
            {
                // The scrollable part of the table starts after the frozen strip and covers the rest
                // of the viewport: the scroll offset therefore has to be measured from the end of the
                // frozen strip, and the frozen rows / columns have to be skipped.
                (int left, double leftDelta, int width, double actualWidth, double startWidth) = GetRangeX(this.Offset.X + frozenStripWidth, viewWidth - frozenStripWidth);
                (int top, double topDelta, int height, double actualHeight, double startHeight) = GetRangeY(this.Offset.Y + frozenStripHeight, viewHeight - frozenStripHeight);

                // The frozen strip is drawn without any scroll offset, hence the compensation.
                double offsetX = leftDelta + frozenStripWidth;
                double offsetY = topDelta + frozenStripHeight;

                int frozenLeft = 0;
                double frozenLeftDelta = 0;
                int frozenColumnSpan = Math.Max(0, this.Sheet.FrozenColumnCount - 1);

                if (frozenStripWidth > 0)
                {
                    (frozenLeft, frozenLeftDelta, frozenColumnSpan, _, _) = GetRangeX(0, frozenStripWidth);
                }

                int frozenTop = 0;
                double frozenTopDelta = 0;
                int frozenRowSpan = Math.Max(0, this.Sheet.FrozenRowCount - 1);

                if (frozenStripHeight > 0)
                {
                    (frozenTop, frozenTopDelta, frozenRowSpan, _, _) = GetRangeY(0, frozenStripHeight);
                }

                // 1. scrollable rows and columns. This region also owns the hit testing data used by
                // the pointer handlers; the frozen strips are resolved separately.
                RenderRegion(context, left, top, width, height, offsetX, offsetY, new Rect(frozenStripWidth, frozenStripHeight, viewWidth - frozenStripWidth, viewHeight - frozenStripHeight), true, false, false);

                // 2. frozen rows, scrollable columns.
                if (frozenStripHeight > 0)
                {
                    RenderRegion(context, left, frozenTop, width, frozenRowSpan, offsetX, frozenTopDelta, new Rect(frozenStripWidth, 0, viewWidth - frozenStripWidth, frozenStripHeight), false, false, true);
                }

                // 3. scrollable rows, frozen columns.
                if (frozenStripWidth > 0)
                {
                    RenderRegion(context, frozenLeft, top, frozenColumnSpan, height, frozenLeftDelta, offsetY, new Rect(0, frozenStripHeight, frozenStripWidth, viewHeight - frozenStripHeight), false, true, false);
                }

                // 4. frozen rows and frozen columns.
                if (frozenStripWidth > 0 && frozenStripHeight > 0)
                {
                    RenderRegion(context, frozenLeft, frozenTop, frozenColumnSpan, frozenRowSpan, frozenLeftDelta, frozenTopDelta, new Rect(0, 0, frozenStripWidth, frozenStripHeight), false, true, true);
                }

                _ = Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.Container.SetScrollbarMaximum(actualWidth + startWidth - viewWidth + GetWidth(left + width), actualHeight + startHeight - viewHeight + GetHeight(top + height));
                });
            }

            // Separator lines between the frozen strips and the scrollable area.
            if (frozenStripWidth > 0 || frozenStripHeight > 0)
            {
                Pen frozenSeparatorPen = new Pen(new SolidColorBrush(Color.FromRgb((byte)(this.GridColor.R * 0.6), (byte)(this.GridColor.G * 0.6), (byte)(this.GridColor.B * 0.6))), 1);

                if (frozenStripWidth > 0)
                {
                    context.DrawLine(frozenSeparatorPen, new Point(frozenStripWidth + 0.5, 0).SnapToDevicePixels(this), new Point(frozenStripWidth + 0.5, viewHeight).SnapToDevicePixels(this));
                }

                if (frozenStripHeight > 0)
                {
                    context.DrawLine(frozenSeparatorPen, new Point(0, frozenStripHeight + 0.5).SnapToDevicePixels(this), new Point(viewWidth, frozenStripHeight + 0.5).SnapToDevicePixels(this));
                }
            }
        }

        /// <summary>
        /// Draws one rectangular region of the table (either the scrollable area or one of the frozen
        /// strips). Every region uses its own coordinate origin: the frozen ones are not translated by
        /// the scroll offset, which is what keeps them in place while the rest of the table scrolls.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <param name="left">The index of the first drawn column.</param>
        /// <param name="top">The index of the first drawn row.</param>
        /// <param name="width">The number of drawn columns.</param>
        /// <param name="height">The number of drawn rows.</param>
        /// <param name="offsetX">The horizontal translation applied to the region.</param>
        /// <param name="offsetY">The vertical translation applied to the region.</param>
        /// <param name="region">The region of the viewport covered by this region, in screen coordinates.</param>
        /// <param name="isMainRegion"><see langword="true"/> for the scrollable region, which owns the hit testing data.</param>
        /// <param name="frozenColumns"><see langword="true"/> when the region draws the frozen column strip.</param>
        /// <param name="frozenRows"><see langword="true"/> when the region draws the frozen row strip.</param>
        private void RenderRegion(DrawingContext context, int left, int top, int width, int height, double offsetX, double offsetY, Rect region, bool isMainRegion, bool frozenColumns, bool frozenRows)
        {
            Pen gridPen = new Pen(new SolidColorBrush(GridColor));
                Pen blackPen = new Pen(Brushes.Black);

                // A worksheet can let a long text spill over the empty cells that follow it (the
                // default is to clip it to its own cell) and ask for a focus rectangle instead of a
                // filled selection.
                bool allowTextOverflow = this.Sheet != null && this.Sheet.AllowTextOverflow;
                bool fillSelection = this.Sheet == null || this.Sheet.SelectionStyle != SheetSelectionStyle.FocusRect;

                Pen selectionPen = new Pen(SelectionAccent, 2);
                Pen selectionMovePen = new Pen(SelectionAccent, 3);
                Pen selectionWhitePen = new Pen(this.Background, 1);
                Brush selectionHighlightBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
                Brush clearingBrush = new SolidColorBrush(this.Background.Color, 0.5);
                BoxShadows shadows = new BoxShadows(new BoxShadow() { Blur = 5, OffsetX = 1, OffsetY = 1, Color = Color.FromArgb(128, 0, 0, 0) });

                Typeface defaultTypeFace = new Typeface(this.FontFamily, this.FontStyle, this.FontWeight);

                // Everything drawn by this region (including the selection) is clipped to it, so that
                // the frozen strips never paint over the scrollable area and vice versa.
                using (context.PushTransform(Matrix.CreateTranslation(offsetX, offsetY)))
                using (context.PushClip(new Rect(region.X + 1 - offsetX, region.Y + 1 - offsetY, region.Width, region.Height)))
                {
                    double[] xs = new double[width + 1];
                    double[] ys = new double[height + 1];

                    using (context.PushClip(new Rect(-offsetX + 1, -offsetY + 1, this.Bounds.Width, this.Bounds.Height)))
                    {

                        double currX = 0;
                        for (int x = 0; x <= width; x++)
                        {
                            currX += GetWidth(left + x);
                            xs[x] = currX;
                        }

                        double currY = 0;
                        for (int y = 0; y <= height; y++)
                        {
                            currY += GetHeight(top + y);
                            ys[y] = currY;
                        }

                        // Only the scrollable region publishes its coordinates: the frozen strips are
                        // resolved by GetDrawnX / GetDrawnY, which know about the frozen origin.
                        if (isMainRegion)
                        {
                            lastDrawnXs = xs;
                            lastDrawnYs = ys;

                            lastDrawnDeltaX = offsetX;
                            lastDrawnDeltaY = offsetY;

                            lastDrawnTop = top;
                            lastDrawnLeft = left;
                            lastDrawnWidth = width;
                            lastDrawnHeight = height;
                        }


                        // Draw vertical grid lines, skipping the interior of merged ranges
                        for (int x = 0; x <= width; x++)
                        {
                            if (x + left > 0)
                            {
                                int lineIndex = left + x - 1;

                                foreach ((int segmentStart, int segmentEnd) in GetVisibleLineSegments(top, top + height, lineIndex, false))
                                {
                                    double lineY0 = segmentStart == top ? 0 : ys[segmentStart - top - 1];
                                    double lineY1 = segmentEnd == top + height ? ys[height] : ys[segmentEnd - top];

                                    context.DrawLine(gridPen, new Point(xs[x], lineY0).SnapToDevicePixels(this), new Point(xs[x], lineY1).SnapToDevicePixels(this));
                                }
                            }
                        }

                        // Draw horizontal grid lines, skipping the interior of merged ranges
                        for (int y = 0; y <= height; y++)
                        {
                            if (y + top > 0)
                            {
                                int lineIndex = top + y - 1;

                                foreach ((int segmentStart, int segmentEnd) in GetVisibleLineSegments(left, left + width, lineIndex, true))
                                {
                                    double lineX0 = segmentStart == left ? 0 : xs[segmentStart - left - 1];
                                    double lineX1 = segmentEnd == left + width ? xs[width] : xs[segmentEnd - left];

                                    context.DrawLine(gridPen, new Point(lineX0, ys[y]).SnapToDevicePixels(this), new Point(lineX1, ys[y]).SnapToDevicePixels(this));
                                }
                            }
                        }

                        // Draw cell backgrounds on top of the grid lines (they are painted over the
                        // default grid, while explicitly defined borders are drawn on top of them)
                        if (this.Sheet.CellBackgrounds.Count > 0)
                        {
                            foreach (CellBackground cellBackground in this.Sheet.CellBackgrounds)
                            {
                                if (cellBackground == null || cellBackground.Brush == null)
                                {
                                    continue;
                                }

                                SelectionRange visibleBackground = cellBackground.Range.Intersection(new SelectionRange(left, top, left + width, top + height));

                                if (visibleBackground.Width > 0 && visibleBackground.Height > 0)
                                {
                                    double backgroundX0 = visibleBackground.Left == left ? 0 : xs[visibleBackground.Left - left - 1];
                                    double backgroundY0 = visibleBackground.Top == top ? 0 : ys[visibleBackground.Top - top - 1];
                                    double backgroundX1 = visibleBackground.Right - left < width ? xs[visibleBackground.Right - left] : xs[width];
                                    double backgroundY1 = visibleBackground.Bottom - top < height ? ys[visibleBackground.Bottom - top] : ys[height];

                                    context.FillRectangle(cellBackground.Brush, new Rect(backgroundX0, backgroundY0, backgroundX1 - backgroundX0, backgroundY1 - backgroundY0));
                                }
                            }
                        }

                        // Cells covered by merged ranges (excluding their top-left cell) are drawn
                        // as part of the merged range, so they are skipped here.
                        HashSet<(int, int)> mergedCells = null;
                        if (MergedRanges.Count > 0)
                        {
                            mergedCells = new HashSet<(int, int)>();
                            foreach (SelectionRange mergedRange in MergedRanges)
                            {
                                for (int mergedRow = mergedRange.Top; mergedRow <= mergedRange.Bottom; mergedRow++)
                                {
                                    for (int mergedColumn = mergedRange.Left; mergedColumn <= mergedRange.Right; mergedColumn++)
                                    {
                                        // The whole merged range is skipped here: the merged-range text
                                        // drawing code below draws the top-left cell content centred
                                        // over the entire range. Including the top-left cell in this
                                        // list avoids drawing the same text twice.
                                        mergedCells.Add((mergedColumn, mergedRow));
                                    }
                                }
                            }
                        }

                        for (int y = 0; y <= height; y++)
                        {
                            // Hidden (collapsed) rows have a height of zero; nothing of theirs is drawn.
                            if (IsRowHidden(top + y))
                            {
                                continue;
                            }

                            // Rows at the very beginning / end of a tree expand / collapse animation
                            // have (almost) no height: skip their content entirely.
                            if (animatedRowOpacity.Count > 0 && animatedRowOpacity.TryGetValue(top + y, out double rowOpacity) && rowOpacity <= 0.02)
                            {
                                continue;
                            }

                            // Rows that are fading in / out together with the tree expand / collapse
                            // animation are drawn with the interpolated opacity (1 when the row is
                            // not animating). Everything drawn for the row (text, tree guide lines,
                            // numeric formatting) is affected.
                            double rowFade = animatedRowOpacity.Count > 0 && animatedRowOpacity.TryGetValue(top + y, out double rowOpacityValue) ? rowOpacityValue : 1;

                            using (context.PushOpacity(rowFade))
                            {
                            for (int x = 0; x <= width; x++)
                            {
                                // Hidden columns have a width of zero; nothing of theirs is drawn,
                                // otherwise their text would spill over the neighbouring column.
                                if (IsColumnHidden(left + x))
                                {
                                    continue;
                                }

                                bool hasData = Data.TryGetValue((left + x, top + y), out string txt);
                                bool hasTreeCell = TreeCells.TryGetValue((left + x, top + y), out TreeGradeCell treeCell) && treeCell != null;
                                bool hasNumericCell = NumericCells.TryGetValue((left + x, top + y), out NumericCell numericCellPlaceholder) && numericCellPlaceholder != null;
                                bool hasButtonCell = ButtonCells.TryGetValue((left + x, top + y), out ButtonCell buttonCell) && buttonCell != null;
                                bool hasLinkCell = LinkCells.TryGetValue((left + x, top + y), out LinkCell linkCell) && linkCell != null;

                                // A link cell carries its own content (and its own empty-value
                                // placeholder), so it is drawn even when the sheet has no data for
                                // it: an empty amount still has to be clickable.
                                if (!hasData && !hasTreeCell && !hasNumericCell && !hasButtonCell && !hasLinkCell)
                                {
                                    continue;
                                }

                                // Tree cells draw their own content (guide lines, +/- box and caption).
                                if (hasTreeCell && (!Container.IsEditing || left + x != Container.EditingCell.Item1 || top + y != Container.EditingCell.Item2))
                                {
                                    double treeX = x == 0 ? 0 : xs[x - 1];
                                    double treeY = y == 0 ? 0 : ys[y - 1];

                                    if (!CellTypefaces.TryGetValue((left + x, top + y), out Typeface treeFace) &&
                                        !RowTypefaces.TryGetValue(top + y, out treeFace) &&
                                        !ColumnTypefaces.TryGetValue(left + x, out treeFace))
                                    {
                                        treeFace = defaultTypeFace;
                                    }

                                    double treeFontSize = this.FontSize;
                                    if (CellFontSize.TryGetValue((left + x, top + y), out double customTreeFontSize))
                                    {
                                        treeFontSize = customTreeFontSize;
                                    }

                                    int treeRow = top + y;
                                    treeCell.Render(context, new Rect(treeX, treeY, xs[x] - treeX, ys[y] - treeY), xs[width], this, treeFace, treeFontSize, this.Foreground, lvl => HasNextSiblingAtLevel(treeRow, lvl));
                                }

                                // Button cells render a clickable button instead of the default
                                // text; they carry their own content, so they do not need any data.
                                if (hasButtonCell && (!Container.IsEditing || left + x != Container.EditingCell.Item1 || top + y != Container.EditingCell.Item2))
                                {
                                    double buttonX = x == 0 ? 0 : xs[x - 1];
                                    double buttonY = y == 0 ? 0 : ys[y - 1];

                                    if (!CellTypefaces.TryGetValue((left + x, top + y), out Typeface buttonFace) &&
                                        !RowTypefaces.TryGetValue(top + y, out buttonFace) &&
                                        !ColumnTypefaces.TryGetValue(left + x, out buttonFace))
                                    {
                                        buttonFace = defaultTypeFace;
                                    }

                                    double buttonFontSize = this.FontSize;
                                    if (CellFontSize.TryGetValue((left + x, top + y), out double customButtonFontSize))
                                    {
                                        buttonFontSize = customButtonFontSize;
                                    }

                                    if (!CellForeground.TryGetValue((left + x, top + y), out IBrush buttonForeground) &&
                                        !RowForeground.TryGetValue(top + y, out buttonForeground) &&
                                        !ColumnForeground.TryGetValue(left + x, out buttonForeground))
                                    {
                                        buttonForeground = this.Foreground;
                                    }

                                    TextAlignment buttonHor = this.DefaultTextAlignment;
                                    if (CellTextAlignment.TryGetValue((left + x, top + y), out TextAlignment customButtonHor))
                                    {
                                        buttonHor = customButtonHor;
                                    }

                                    VerticalAlignment buttonVer = this.DefaultVerticalAlignment;
                                    if (CellVerticalAlignment.TryGetValue((left + x, top + y), out VerticalAlignment customButtonVer))
                                    {
                                        buttonVer = customButtonVer;
                                    }

                                    buttonCell.Render(context, new Rect(buttonX, buttonY, xs[x] - buttonX, ys[y] - buttonY), buttonForeground, buttonFace, buttonFontSize, buttonHor, buttonVer, txt);
                                }

                                // Link cells render a clickable hyperlink instead of the default
                                // text. They bring their own content (values of zero are drawn as
                                // the "…" placeholder), so they do not need any data of their own.
                                if (hasLinkCell && (mergedCells == null || !mergedCells.Contains((left + x, top + y))) &&
                                    (!Container.IsEditing || left + x != Container.EditingCell.Item1 || top + y != Container.EditingCell.Item2))
                                {
                                    double linkX = x == 0 ? 0 : xs[x - 1];
                                    double linkY = y == 0 ? 0 : ys[y - 1];

                                    if (!CellTypefaces.TryGetValue((left + x, top + y), out Typeface linkFace) &&
                                        !RowTypefaces.TryGetValue(top + y, out linkFace) &&
                                        !ColumnTypefaces.TryGetValue(left + x, out linkFace))
                                    {
                                        linkFace = defaultTypeFace;
                                    }

                                    double linkFontSize = this.FontSize;
                                    if (CellFontSize.TryGetValue((left + x, top + y), out double customLinkFontSize))
                                    {
                                        linkFontSize = customLinkFontSize;
                                    }

                                    TextAlignment linkHor = this.DefaultTextAlignment;
                                    if (CellTextAlignment.TryGetValue((left + x, top + y), out TextAlignment customLinkHor))
                                    {
                                        linkHor = customLinkHor;
                                    }

                                    VerticalAlignment linkVer = this.DefaultVerticalAlignment;
                                    if (CellVerticalAlignment.TryGetValue((left + x, top + y), out VerticalAlignment customLinkVer))
                                    {
                                        linkVer = customLinkVer;
                                    }

                                    Thickness linkMargin = this.DefaultMargin;
                                    if (CellMargin.TryGetValue((left + x, top + y), out Thickness customLinkMargin))
                                    {
                                        linkMargin = customLinkMargin;
                                    }

                                    // Lets the cell resolve table level defaults (EmptyNumericLinkText).
                                    linkCell.OwnerTable = this;

                                    linkCell.Render(context, new Rect(linkX, linkY, xs[x] - linkX, ys[y] - linkY), null, linkMargin, txt, linkFace, linkFontSize, linkHor, linkVer);
                                    continue;
                                }

                                if (hasData)
                                {
                                    if (mergedCells != null && mergedCells.Contains((left + x, top + y)))
                                    {
                                        continue;
                                    }


                                    if (!Container.IsEditing || left + x != Container.EditingCell.Item1 || top + y != Container.EditingCell.Item2)
                                    {
                                        // A tree cell draws its own caption; the raw text must not be
                                        // drawn on top of it.
                                        if (hasTreeCell)
                                        {
                                            continue;
                                        }

                                        // A button cell has just drawn its own content.
                                        if (hasButtonCell)
                                        {
                                            continue;
                                        }

                                        // Title cells render their own content and override the default text drawing.
                                        if (TitleCells.TryGetValue((left + x, top + y), out TitleCell titleCell))
                                        {
                                            double titleX = x == 0 ? 0 : xs[x - 1];
                                            double titleY = y == 0 ? 0 : ys[y - 1];
                                            titleCell.Render(context, new Rect(titleX, titleY, xs[x] - titleX, ys[y] - titleY), null, null);
                                            continue;
                                        }

                                        // Determine display text: numeric cells are formatted, then
                                        // formula results, then the raw text.
                                        string displayText = txt;
                                        NumericCell activeNumericCell = null;

                                        if (NumericCells.TryGetValue((left + x, top + y), out NumericCell numericCell) && numericCell != null)
                                        {
                                            activeNumericCell = numericCell;
                                            displayText = numericCell.GetDisplayText(txt);
                                        }
                                        else if (FormulaCells != null && FormulaCells.TryGetValue((left + x, top + y), out CellData formulaCell))
                                        {
                                            displayText = formulaCell.DisplayText;
                                        }


                                        if (!CellTypefaces.TryGetValue((left + x, top + y), out Typeface face) &&
                                            !RowTypefaces.TryGetValue(top + y, out face) &&
                                            !ColumnTypefaces.TryGetValue(left + x, out face))
                                        {
                                            face = defaultTypeFace;
                                        }

                                        if (!CellForeground.TryGetValue((left + x, top + y), out IBrush brs) &&
                                            !RowForeground.TryGetValue(top + y, out brs) &&
                                            !ColumnForeground.TryGetValue(left + x, out brs))
                                        {
                                            brs = this.Foreground;
                                        }

                                        if (!CellTextAlignment.TryGetValue((left + x, top + y), out TextAlignment hor))
                                        {
                                            hor = this.DefaultTextAlignment;
                                        }

                                        if (activeNumericCell != null)
                                        {
                                            // Numeric values are always right aligned, like the WPF reference.
                                            hor = TextAlignment.Right;

                                            if (activeNumericCell.NegativeRed && activeNumericCell.IsNegative(txt))
                                            {
                                                brs = activeNumericCell.NegativeBrush ?? new SolidColorBrush(Colors.Red);
                                            }
                                        }

                                        if (!CellVerticalAlignment.TryGetValue((left + x, top + y), out VerticalAlignment ver))
                                        {
                                            ver = this.DefaultVerticalAlignment;
                                        }

                                        if (!CellMargin.TryGetValue((left + x, top + y), out Thickness margin))
                                        {
                                            margin = this.DefaultMargin;
                                        }

                                        double realX = x == 0 ? 0 : xs[x - 1];
                                        double realY = y == 0 ? 0 : ys[y - 1];

                                        double cellFontSize = this.FontSize;
                                        if (CellFontSize.TryGetValue((left + x, top + y), out double customFontSize))
                                        {
                                            cellFontSize = customFontSize;
                                        }

                                        FormattedText fmtText = new FormattedText(displayText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, face, cellFontSize, brs);

                                        double textWidth = fmtText.Width;

                                        IBrush colourBrush = null;

                                        if (this.Container.ShowColorPreview && displayText.StartsWith("#") && (displayText.Length == 7 || displayText.Length == 9))
                                        {
                                            try
                                            {
                                                colourBrush = Brush.Parse(displayText.Length == 7 ? displayText : ("#" + displayText.Substring(7, 2) + displayText.Substring(1, 6)));
                                                textWidth += cellFontSize + 3;
                                            }
                                            catch
                                            {
                                                colourBrush = null;
                                            }
                                        }

                                        // When a formatted numeric value does not fit into the column, show a
                                        // trailing ellipsis ("1,120,7…") instead of clipping it mid-glyph, like
                                        // the WPF reference (SSFinance) does.
                                        if (activeNumericCell != null && colourBrush == null)
                                        {
                                            double availableWidth = xs[x] - realX - margin.Left - margin.Right;

                                            if (textWidth > availableWidth)
                                            {
                                                string truncated = TextTruncation.TruncateWithEllipsis(displayText, face, cellFontSize, brs, availableWidth);

                                                if (truncated != displayText)
                                                {
                                                    displayText = truncated;
                                                    fmtText = new FormattedText(displayText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, face, cellFontSize, brs);
                                                    textWidth = fmtText.Width;
                                                }
                                            }
                                        }

                                        double clipRight = allowTextOverflow && hor == TextAlignment.Left && activeNumericCell == null
                                            ? GetOverflowRightEdge(left, top + y, x, width, xs)
                                            : xs[x];

                                        using (context.PushClip(new Rect(realX + margin.Left, realY + margin.Top, clipRight - realX - margin.Left - margin.Right, ys[y] - realY - margin.Top - margin.Bottom)))
                                        {
                                            if (ver == VerticalAlignment.Top)
                                            {
                                                realY += margin.Top;
                                            }
                                            else if (ver == VerticalAlignment.Bottom)
                                            {
                                                realY = ys[y] - margin.Bottom - fmtText.Height;
                                            }
                                            else if (ver == VerticalAlignment.Center || ver == VerticalAlignment.Stretch)
                                            {
                                                realY = (realY + margin.Top + ys[y] - margin.Bottom) * 0.5 - fmtText.Height * 0.5;
                                            }

                                            if (hor == TextAlignment.Left)
                                            {
                                                realX += margin.Left;
                                            }
                                            else if (hor == TextAlignment.Center)
                                            {
                                                realX = (realX + margin.Left + xs[x] - margin.Right) * 0.5 - textWidth * 0.5;
                                            }
                                            else if (hor == TextAlignment.Right)
                                            {
                                                realX = xs[x] - margin.Right - textWidth;
                                            }


                                            if (activeNumericCell != null && activeNumericCell.Strikethrough && textWidth > 0)
                                            {
                                                double strikeY = realY + fmtText.Height * 0.5;
                                                context.DrawLine(new Pen(activeNumericCell.StrikethroughBrush ?? new SolidColorBrush(Colors.Red), 2), new Point(realX, strikeY), new Point(realX + textWidth, strikeY));
                                            }

                                            if (colourBrush == null)
                                            {
                                                context.DrawText(fmtText, new Point(realX, realY));
                                            }
                                            else
                                            {
                                                double rectY = realY + fmtText.Height * 0.5 - cellFontSize * 0.5;

                                                if (txt.Length == 9)
                                                {
                                                    context.DrawRectangle(gridPen.Brush, null, new Rect(new Point(realX, rectY).SnapToDevicePixels(this), new Point(realX + cellFontSize * 0.5, rectY + cellFontSize * 0.5).SnapToDevicePixels(this)), 0, 0);
                                                    context.DrawRectangle(gridPen.Brush, null, new Rect(new Point(realX + cellFontSize * 0.5, rectY + cellFontSize * 0.5).SnapToDevicePixels(this), new Point(realX + cellFontSize, rectY + cellFontSize).SnapToDevicePixels(this)), 0, 0);
                                                }

                                                context.DrawRectangle(colourBrush, blackPen, new Rect(new Point(realX, rectY).SnapToDevicePixels(this), new Size(cellFontSize, cellFontSize)), 0, 0);
                                                context.DrawText(fmtText, new Point(realX + cellFontSize + 3, realY));
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        // Draw the content of merged ranges centred over the whole range
                        if (MergedRanges.Count > 0)
                        {
                            foreach (SelectionRange mergedRange in MergedRanges)
                            {
                                // Only skip merges that are entirely outside the visible range
                                if (mergedRange.Right < left || mergedRange.Left > left + width ||
                                    mergedRange.Bottom < top || mergedRange.Top > top + height)
                                {
                                    continue;
                                }

                                // The merged text is centred over the *whole* merged range, even
                                // when part of the range falls outside this region (e.g. a merged
                                // title that crosses the frozen column boundary): each region draws
                                // its own portion of the same centred text, clipped to its area, so
                                // the text stays continuous across the frozen separator instead of
                                // being duplicated or re-centred on each side.
                                double mergeX0;
                                double mergeX1;

                                if (mergedRange.Left < left)
                                {
                                    double w = 0;
                                    for (int c = mergedRange.Left; c < left; c++) { w += GetWidth(c); }
                                    mergeX0 = -w;
                                }
                                else
                                {
                                    mergeX0 = mergedRange.Left == left ? 0 : xs[mergedRange.Left - left - 1];
                                }

                                if (mergedRange.Right >= left + width)
                                {
                                    double w = 0;
                                    // xs[width] already includes column left+width, so the extra
                                    // columns start at left+width+1.
                                    for (int c = left + width + 1; c <= mergedRange.Right; c++) { w += GetWidth(c); }
                                    mergeX1 = xs[width] + w;
                                }
                                else
                                {
                                    mergeX1 = xs[mergedRange.Right - left];
                                }

                                double mergeY0;
                                double mergeY1;

                                if (mergedRange.Top < top)
                                {
                                    double h = 0;
                                    for (int r = mergedRange.Top; r < top; r++) { h += GetHeight(r); }
                                    mergeY0 = -h;
                                }
                                else
                                {
                                    mergeY0 = mergedRange.Top == top ? 0 : ys[mergedRange.Top - top - 1];
                                }

                                if (mergedRange.Bottom >= top + height)
                                {
                                    double h = 0;
                                    // ys[height] already includes row top+height, so the extra
                                    // rows start at top+height+1.
                                    for (int r = top + height + 1; r <= mergedRange.Bottom; r++) { h += GetHeight(r); }
                                    mergeY1 = ys[height] + h;
                                }
                                else
                                {
                                    mergeY1 = ys[mergedRange.Bottom - top];
                                }

                                bool hasMergedData = Data.TryGetValue((mergedRange.Left, mergedRange.Top), out string mergedText);
                                bool hasMergedTitleCell = TitleCells.TryGetValue((mergedRange.Left, mergedRange.Top), out TitleCell mergedTitleCell);
                                bool hasMergedLinkCell = LinkCells.TryGetValue((mergedRange.Left, mergedRange.Top), out LinkCell mergedLinkCell);
                                if (!hasMergedData && !hasMergedTitleCell && !hasMergedLinkCell)
                                {
                                    continue;
                                }

                                if (Container.IsEditing && Container.EditingCell.Item1 == mergedRange.Left && Container.EditingCell.Item2 == mergedRange.Top)
                                {
                                    continue;
                                }

                                // Title cells render their own content over the whole merged range.
                                if (hasMergedTitleCell)
                                {
                                    mergedTitleCell.Render(context, new Rect(mergeX0, mergeY0, mergeX1 - mergeX0, mergeY1 - mergeY0), null, null);
                                    continue;
                                }

                                // Link cells render a clickable hyperlink over the whole merged range.
                                if (hasMergedLinkCell)
                                {


                                    if (!CellTypefaces.TryGetValue((mergedRange.Left, mergedRange.Top), out Typeface mergedLinkFace) &&
                                        !RowTypefaces.TryGetValue(mergedRange.Top, out mergedLinkFace) &&
                                        !ColumnTypefaces.TryGetValue(mergedRange.Left, out mergedLinkFace))
                                    {
                                        mergedLinkFace = defaultTypeFace;
                                    }

                                    double mergedLinkFontSize = this.FontSize;
                                    if (CellFontSize.TryGetValue((mergedRange.Left, mergedRange.Top), out double mergedCustomLinkFontSize))
                                    {
                                        mergedLinkFontSize = mergedCustomLinkFontSize;
                                    }

                                    TextAlignment mergedLinkHor = TextAlignment.Center;
                                    if (CellTextAlignment.TryGetValue((mergedRange.Left, mergedRange.Top), out TextAlignment mergedCustomLinkHor))
                                    {
                                        mergedLinkHor = mergedCustomLinkHor;
                                    }

                                    VerticalAlignment mergedLinkVer = VerticalAlignment.Center;
                                    if (CellVerticalAlignment.TryGetValue((mergedRange.Left, mergedRange.Top), out VerticalAlignment mergedCustomLinkVer))
                                    {
                                        mergedLinkVer = mergedCustomLinkVer;
                                    }

                                    Thickness mergedLinkMargin = new Thickness(3);
                                    if (CellMargin.TryGetValue((mergedRange.Left, mergedRange.Top), out Thickness mergedCustomLinkMargin))
                                    {
                                        mergedLinkMargin = mergedCustomLinkMargin;
                                    }

                                    // Lets the cell resolve table level defaults (EmptyNumericLinkText).
                                    mergedLinkCell.OwnerTable = this;

                                    mergedLinkCell.Render(context, new Rect(mergeX0, mergeY0, mergeX1 - mergeX0, mergeY1 - mergeY0), null, mergedLinkMargin, mergedText, mergedLinkFace, mergedLinkFontSize, mergedLinkHor, mergedLinkVer);
                                    continue;
                                }

                                string mergedDisplayText = mergedText;
                                if (FormulaCells != null && FormulaCells.TryGetValue((mergedRange.Left, mergedRange.Top), out CellData mergedFormulaCell))
                                {
                                    mergedDisplayText = mergedFormulaCell.DisplayText;
                                }

                                if (!CellTypefaces.TryGetValue((mergedRange.Left, mergedRange.Top), out Typeface mergedFace) &&
                                    !RowTypefaces.TryGetValue(mergedRange.Top, out mergedFace) &&
                                    !ColumnTypefaces.TryGetValue(mergedRange.Left, out mergedFace))
                                {
                                    mergedFace = defaultTypeFace;
                                }

                                if (!CellForeground.TryGetValue((mergedRange.Left, mergedRange.Top), out IBrush mergedBrush) &&
                                    !RowForeground.TryGetValue(mergedRange.Top, out mergedBrush) &&
                                    !ColumnForeground.TryGetValue(mergedRange.Left, out mergedBrush))
                                {
                                    mergedBrush = this.Foreground;
                                }

                                double mergedFontSize = this.FontSize;
                                if (CellFontSize.TryGetValue((mergedRange.Left, mergedRange.Top), out double mergedCustomFontSize))
                                {
                                    mergedFontSize = mergedCustomFontSize;
                                }

                                TextAlignment mergedHor = TextAlignment.Center;
                                if (CellTextAlignment.TryGetValue((mergedRange.Left, mergedRange.Top), out TextAlignment mergedCustomHor))
                                {
                                    mergedHor = mergedCustomHor;
                                }

                                VerticalAlignment mergedVer = VerticalAlignment.Center;
                                if (CellVerticalAlignment.TryGetValue((mergedRange.Left, mergedRange.Top), out VerticalAlignment mergedCustomVer))
                                {
                                    mergedVer = mergedCustomVer;
                                }

                                Thickness mergedMargin = new Thickness(3);
                                if (CellMargin.TryGetValue((mergedRange.Left, mergedRange.Top), out Thickness mergedCustomMargin))
                                {
                                    mergedMargin = mergedCustomMargin;
                                }

                                // Clip the merged range so the text never spills over neighbouring
                                // cells; the clip covers the whole merged range, while the region clip
                                // already limits the drawing to the visible part of this region.
                                double mergedX0 = mergeX0;
                                double mergedY0 = mergeY0;
                                double mergedX1 = mergeX1;
                                double mergedY1 = mergeY1;

                                FormattedText mergedFormattedText = new FormattedText(mergedDisplayText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, mergedFace, mergedFontSize, mergedBrush);

                                using (context.PushClip(new Rect(mergedX0 + 1, mergedY0 + 1, Math.Max(0, mergedX1 - mergedX0 - 2), Math.Max(0, mergedY1 - mergedY0 - 2))))
                                {
                                    double mergedRealX = mergedX0;
                                    double mergedRealY = mergedY0;

                                    if (mergedVer == VerticalAlignment.Top)
                                    {
                                        mergedRealY += mergedMargin.Top;
                                    }
                                    else if (mergedVer == VerticalAlignment.Bottom)
                                    {
                                        mergedRealY = mergedY1 - mergedMargin.Bottom - mergedFormattedText.Height;
                                    }
                                    else
                                    {
                                        mergedRealY = (mergedY0 + mergedMargin.Top + mergedY1 - mergedMargin.Bottom) * 0.5 - mergedFormattedText.Height * 0.5;
                                    }

                                    if (mergedHor == TextAlignment.Left)
                                    {
                                        mergedRealX += mergedMargin.Left;
                                    }
                                    else if (mergedHor == TextAlignment.Center)
                                    {
                                        mergedRealX = (mergedX0 + mergedMargin.Left + mergedX1 - mergedMargin.Right) * 0.5 - mergedFormattedText.Width * 0.5;
                                    }
                                    else if (mergedHor == TextAlignment.Right)
                                    {
                                        mergedRealX = mergedX1 - mergedMargin.Right - mergedFormattedText.Width;
                                    }

                                    context.DrawText(mergedFormattedText, new Point(mergedRealX, mergedRealY));
                                }
                            }
                            }
                        }

                        // Draw borders on top of the grid lines
                        if (this.Sheet.CellBorders.Count > 0)
                        {
                            foreach (CellBorder border in this.Sheet.CellBorders)
                            {
                                if (border == null || border.Brush == null)
                                {
                                    continue;
                                }

                                Pen borderPen = new Pen(border.Brush, border.Thickness > 0 ? border.Thickness : 1)
                                {
                                    DashStyle = border.DashStyle
                                };

                                if (border.IsHorizontal)
                                {
                                    if (border.LineIndex < top || border.LineIndex > top + height + 1)
                                    {
                                        continue;
                                    }

                                    double borderY;
                                    if (border.LineIndex == top)
                                    {
                                        borderY = 0;
                                    }
                                    else if (border.LineIndex >= top + height + 1)
                                    {
                                        borderY = ys[height];
                                    }
                                    else
                                    {
                                        borderY = ys[border.LineIndex - top - 1];
                                    }

                                    int borderColumnStart = Math.Max(border.From, left);
                                    int borderColumnEnd = Math.Min(border.To, left + width);

                                    if (borderColumnStart > borderColumnEnd)
                                    {
                                        continue;
                                    }

                                    // Skip the parts of the border that lie inside merged ranges
                                    foreach ((int segmentStart, int segmentEnd) in GetVisibleLineSegments(borderColumnStart, borderColumnEnd, border.LineIndex, true))
                                    {
                                        double borderX0 = segmentStart == left ? 0 : xs[segmentStart - left - 1];
                                        double borderX1 = segmentEnd == left + width ? xs[width] : xs[segmentEnd - left];

                                        context.DrawLine(borderPen, new Point(borderX0, borderY).SnapToDevicePixels(this), new Point(borderX1, borderY).SnapToDevicePixels(this));
                                    }
                                }
                                else
                                {
                                    if (border.LineIndex < left || border.LineIndex > left + width + 1)
                                    {
                                        continue;
                                    }

                                    double borderX;
                                    if (border.LineIndex == left)
                                    {
                                        borderX = 0;
                                    }
                                    else if (border.LineIndex >= left + width + 1)
                                    {
                                        borderX = xs[width];
                                    }
                                    else
                                    {
                                        borderX = xs[border.LineIndex - left - 1];
                                    }

                                    int borderRowStart = Math.Max(border.From, top);
                                    int borderRowEnd = Math.Min(border.To, top + height);

                                    if (borderRowStart > borderRowEnd)
                                    {
                                        continue;
                                    }

                                    // Skip the parts of the border that lie inside merged ranges
                                    foreach ((int segmentStart, int segmentEnd) in GetVisibleLineSegments(borderRowStart, borderRowEnd, border.LineIndex, false))
                                    {
                                        double borderY0 = segmentStart == top ? 0 : ys[segmentStart - top - 1];
                                        double borderY1 = segmentEnd == top + height ? ys[height] : ys[segmentEnd - top];

                                        context.DrawLine(borderPen, new Point(borderX, borderY0).SnapToDevicePixels(this), new Point(borderX, borderY1).SnapToDevicePixels(this));
                                    }
                                }
                            }
                        }
                    }


                    if (Selection.Count == 1)
                    {
                        if (Selection[0].Left <= left + width && Selection[0].Right >= left && Selection[0].Top <= top + height && Selection[0].Bottom >= top)
                        {
                            double x0 = Selection[0].Left - left == 0 ? 0 : Selection[0].Left - left < 0 ? -5 : xs[Selection[0].Left - left - 1];
                            double y0 = Selection[0].Top - top == 0 ? 0 : Selection[0].Top - top < 0 ? -5 : ys[Selection[0].Top - top - 1];

                            double x1 = Selection[0].Right - left < 0 ? 0 : Selection[0].Right > left + width ? xs[xs.Length - 1] + 5 : xs[Selection[0].Right - left] + 1;
                            double y1 = Selection[0].Bottom - top < 0 ? 0 : Selection[0].Bottom > top + height ? ys[ys.Length - 1] + 5 : ys[Selection[0].Bottom - top] + 1;

                            int leftMargin = 0;
                            int topMargin = 0;

                            if (Selection[0].Left == left)
                            {
                                leftMargin = 2;
                            }

                            if (Selection[0].Top == top)
                            {
                                topMargin = 2;
                            }

                            if (!Container.IsEditing && fillSelection)
                            {
                                using (context.PushClip(new Rect(-offsetX + 1, -offsetY + 1, this.Bounds.Width, this.Bounds.Height)))
                                {
                                    context.DrawRectangle(selectionWhitePen, new Rect(new Point(x0 + 1, y0 + 1).SnapToDevicePixels(this), new Point(x1 - 2, y1 - 2).SnapToDevicePixels(this)));
                                    context.FillRectangle(selectionHighlightBrush, new Rect(new Point(x0 + 2, y0 + 2).SnapToDevicePixels(this, true, true), new Point(x1 - 2, y1 - 2).SnapToDevicePixels(this, true, true)));
                                }
                            }

                            using (context.PushClip(new Rect(-offsetX - leftMargin + 1, -offsetY - topMargin + 1, this.Bounds.Width + leftMargin, this.Bounds.Height + topMargin)))
                            {
                                context.DrawRectangle(selectionPen, new Rect(new Point(x0, y0).SnapToDevicePixels(this, true, true), new Point(x1, y1).SnapToDevicePixels(this, true, true)));
                            }

                            bool isRows = this.Selection[0].IsRows(this);
                            bool isColumns = this.Selection[0].IsColumns(this);

                            if (!isRows && !isColumns)
                            {
                                context.FillRectangle(this.Background, new Rect(new Point(x1 - 4, y1 - 4).SnapToDevicePixels(this, true, true), new Point(x1 + 3, y1 + 3).SnapToDevicePixels(this, true, true)));
                                context.FillRectangle(SelectionAccent, new Rect(new Point(x1 - 3, y1 - 3).SnapToDevicePixels(this, true, true), new Point(x1 + 2, y1 + 2).SnapToDevicePixels(this, true, true)));
                            }
                            else if (isRows && !isColumns)
                            {
                                context.FillRectangle(this.Background, new Rect(new Point(x0, y1 - 4).SnapToDevicePixels(this, true, true), new Point(x0 + 6, y1 + 3).SnapToDevicePixels(this, true, true)));
                                context.FillRectangle(SelectionAccent, new Rect(new Point(x0, y1 - 3).SnapToDevicePixels(this, true, true), new Point(x0 + 5, y1 + 2).SnapToDevicePixels(this, true, true)));
                            }
                            else if (isColumns && !isRows)
                            {
                                context.FillRectangle(this.Background, new Rect(new Point(x1 - 4, y0).SnapToDevicePixels(this, true, true), new Point(x1 + 3, y0 + 6).SnapToDevicePixels(this, true, true)));
                                context.FillRectangle(SelectionAccent, new Rect(new Point(x1 - 3, y0).SnapToDevicePixels(this, true, true), new Point(x1 + 2, y0 + 5).SnapToDevicePixels(this, true, true)));
                            }
                        }

                        if (pointerPressedAction == 2)
                        {
                            SelectionRange shiftedSelection = new SelectionRange(selectionBeingMoved.Left + selectionMoveDelta.Item1, selectionBeingMoved.Top + selectionMoveDelta.Item2, selectionBeingMoved.Right + selectionMoveDelta.Item1, selectionBeingMoved.Bottom + selectionMoveDelta.Item2);

                            if (shiftedSelection.Top <= top + height && shiftedSelection.Bottom >= top && shiftedSelection.Left <= left + width && shiftedSelection.Right >= left)
                            {
                                double x0 = shiftedSelection.Left - left == 0 ? 0 : shiftedSelection.Left - left < 0 ? -5 : xs[shiftedSelection.Left - left - 1];
                                double y0 = shiftedSelection.Top - top == 0 ? 0 : shiftedSelection.Top - top < 0 ? -5 : ys[shiftedSelection.Top - top - 1];

                                double x1 = shiftedSelection.Right - left < 0 ? 0 : shiftedSelection.Right > left + width ? xs[xs.Length - 1] + 5 : xs[shiftedSelection.Right - left];
                                double y1 = shiftedSelection.Bottom - top < 0 ? 0 : shiftedSelection.Bottom > top + height ? ys[ys.Length - 1] + 5 : ys[shiftedSelection.Bottom - top];

                                int leftMargin = 0;
                                int topMargin = 0;

                                if (shiftedSelection.Left == left)
                                {
                                    leftMargin = 2;
                                }

                                if (shiftedSelection.Top == top)
                                {
                                    topMargin = 2;
                                }

                                bool isRows = this.Selection[0].IsRows(this);
                                bool isColumns = this.Selection[0].IsColumns(this);

                                using (context.PushClip(new Rect(-offsetX - leftMargin + 1, -offsetY - topMargin + 1, this.Bounds.Width + leftMargin, this.Bounds.Height + topMargin)))
                                {
                                    if (isRows && !isColumns && mouseMoveShiftPressed)
                                    {
                                        if (selectionMoveDelta.Item2 < 0 || selectionMoveDelta.Item2 > Selection[0].Height)
                                        {
                                            context.DrawLine(selectionMovePen, new Point(x0, y0).SnapToDevicePixels(this, false, false), new Point(x1, y0).SnapToDevicePixels(this, false, false));
                                            context.DrawLine(selectionMovePen, new Point(x0 + 1, y0 - 10).SnapToDevicePixels(this, false, false), new Point(x0 + 1, y0 + 10).SnapToDevicePixels(this, false, false));
                                            context.DrawLine(selectionMovePen, new Point(x1 - 1, y0 - 10).SnapToDevicePixels(this, false, false), new Point(x1 - 1, y0 + 10).SnapToDevicePixels(this, false, false));
                                        }
                                    }
                                    else if (!isRows && isColumns && mouseMoveShiftPressed)
                                    {
                                        if (selectionMoveDelta.Item1 < 0 || selectionMoveDelta.Item1 > Selection[0].Width)
                                        {
                                            context.DrawLine(selectionMovePen, new Point(x0, y0).SnapToDevicePixels(this, false, false), new Point(x0, y1).SnapToDevicePixels(this, false, false));
                                            context.DrawLine(selectionMovePen, new Point(x0 - 10, y0 + 1).SnapToDevicePixels(this, false, false), new Point(x0 + 10, y0 + 1).SnapToDevicePixels(this, false, false));
                                            context.DrawLine(selectionMovePen, new Point(x0 - 10, y1 - 1).SnapToDevicePixels(this, false, false), new Point(x0 + 10, y1 - 1).SnapToDevicePixels(this, false, false));
                                        }
                                    }
                                    else
                                    {
                                        context.DrawRectangle(selectionMovePen, new Rect(new Point(x0, y0).SnapToDevicePixels(this, false, false), new Point(x1, y1).SnapToDevicePixels(this, false, false)));
                                    }
                                }
                            }
                        }
                        else if (pointerPressedAction == 3)
                        {
                            bool isRows = selectionBeingMoved.IsRows(this);
                            bool isColumns = selectionBeingMoved.IsColumns(this);

                            SelectionRange shiftedSelection;

                            SelectionRange clearingSelection = new SelectionRange();
                            bool clearing = false;

                            int direction = 0;

                            if (selectionMoveDelta.Item1 == 0 && selectionMoveDelta.Item2 == 0)
                            {
                                shiftedSelection = selectionBeingMoved;
                            }
                            else
                            {
                                if ((Math.Abs(selectionMoveDelta.Item1) > Math.Abs(selectionMoveDelta.Item2) || isColumns && !isRows) && !(isRows && !isColumns))
                                {
                                    if (selectionMoveDelta.Item1 > 0)
                                    {
                                        direction = 1;
                                        shiftedSelection = new SelectionRange(selectionBeingMoved.Left, selectionBeingMoved.Top, selectionBeingMoved.Right + selectionMoveDelta.Item1, selectionBeingMoved.Bottom);
                                    }
                                    else if (selectionMoveDelta.Item1 > -selectionBeingMoved.Width)
                                    {
                                        shiftedSelection = new SelectionRange(selectionBeingMoved.Left, selectionBeingMoved.Top, selectionBeingMoved.Right + selectionMoveDelta.Item1, selectionBeingMoved.Bottom);
                                        clearingSelection = new SelectionRange(selectionBeingMoved.Right + selectionMoveDelta.Item1 + 1, selectionBeingMoved.Top, selectionBeingMoved.Right, selectionBeingMoved.Bottom);
                                        clearing = true;
                                    }
                                    else
                                    {
                                        direction = -1;
                                        shiftedSelection = new SelectionRange(selectionBeingMoved.Left + selectionMoveDelta.Item1 + selectionBeingMoved.Width - 1, selectionBeingMoved.Top, selectionBeingMoved.Right, selectionBeingMoved.Bottom);
                                    }
                                }
                                else
                                {
                                    if (selectionMoveDelta.Item2 > 0)
                                    {
                                        direction = 2;
                                        shiftedSelection = new SelectionRange(selectionBeingMoved.Left, selectionBeingMoved.Top, selectionBeingMoved.Right, selectionBeingMoved.Bottom + selectionMoveDelta.Item2);
                                    }
                                    else if (selectionMoveDelta.Item2 > -selectionBeingMoved.Height)
                                    {
                                        shiftedSelection = new SelectionRange(selectionBeingMoved.Left, selectionBeingMoved.Top, selectionBeingMoved.Right, selectionBeingMoved.Bottom + selectionMoveDelta.Item2);
                                        clearingSelection = new SelectionRange(selectionBeingMoved.Left, selectionBeingMoved.Bottom + selectionMoveDelta.Item2 + 1, selectionBeingMoved.Right, selectionBeingMoved.Bottom);
                                        clearing = true;
                                    }
                                    else
                                    {
                                        direction = -2;
                                        shiftedSelection = new SelectionRange(selectionBeingMoved.Left, selectionBeingMoved.Top + selectionMoveDelta.Item2 + selectionBeingMoved.Height - 1, selectionBeingMoved.Right, selectionBeingMoved.Bottom);
                                    }
                                }
                            }

                            if (clearing && clearingSelection.Top <= top + height && clearingSelection.Bottom >= top && clearingSelection.Left <= left + width && clearingSelection.Right >= left)
                            {
                                double x0 = clearingSelection.Left - left == 0 ? 0 : clearingSelection.Left - left < 0 ? -5 : xs[clearingSelection.Left - left - 1];
                                double y0 = clearingSelection.Top - top == 0 ? 0 : clearingSelection.Top - top < 0 ? -5 : ys[clearingSelection.Top - top - 1];

                                double x1 = clearingSelection.Right - left < 0 ? 0 : clearingSelection.Right > left + width ? xs[xs.Length - 1] + 5 : xs[clearingSelection.Right - left];
                                double y1 = clearingSelection.Bottom - top < 0 ? 0 : clearingSelection.Bottom > top + height ? ys[ys.Length - 1] + 5 : ys[clearingSelection.Bottom - top];

                                int leftMargin = 0;
                                int topMargin = 0;

                                if (clearingSelection.Left == left)
                                {
                                    leftMargin = 2;
                                }

                                if (clearingSelection.Top == top)
                                {
                                    topMargin = 2;
                                }

                                using (context.PushClip(new Rect(-offsetX - leftMargin + 1, -offsetY - topMargin + 1, this.Bounds.Width + leftMargin, this.Bounds.Height + topMargin)))
                                {
                                    context.FillRectangle(clearingBrush, new Rect(new Point(x0, y0).SnapToDevicePixels(this, false, false), new Point(x1, y1).SnapToDevicePixels(this, false, false)));
                                }
                            }

                            if (shiftedSelection.Top <= top + height && shiftedSelection.Bottom >= top && shiftedSelection.Left <= left + width && shiftedSelection.Right >= left)
                            {
                                double x0 = shiftedSelection.Left - left == 0 ? 0 : shiftedSelection.Left - left < 0 ? -5 : xs[shiftedSelection.Left - left - 1];
                                double y0 = shiftedSelection.Top - top == 0 ? 0 : shiftedSelection.Top - top < 0 ? -5 : ys[shiftedSelection.Top - top - 1];

                                double x1 = shiftedSelection.Right - left < 0 ? 0 : shiftedSelection.Right > left + width ? xs[xs.Length - 1] + 5 : xs[shiftedSelection.Right - left];
                                double y1 = shiftedSelection.Bottom - top < 0 ? 0 : shiftedSelection.Bottom > top + height ? ys[ys.Length - 1] + 5 : ys[shiftedSelection.Bottom - top];

                                int leftMargin = 0;
                                int topMargin = 0;

                                if (shiftedSelection.Left == left)
                                {
                                    leftMargin = 2;
                                }

                                if (shiftedSelection.Top == top)
                                {
                                    topMargin = 2;
                                }

                                using (context.PushClip(new Rect(-offsetX - leftMargin + 1, -offsetY - topMargin + 1, this.Bounds.Width + leftMargin, this.Bounds.Height + topMargin)))
                                {
                                    context.DrawRectangle(selectionMovePen, new Rect(new Point(x0, y0).SnapToDevicePixels(this, false, false), new Point(x1, y1).SnapToDevicePixels(this, false, false)));
                                }

                                if (!clearing && direction != 0)
                                {
                                    string text = "";

                                    if (direction == 1)
                                    {
                                        text = this.Data.GetLastFillRight(selectionBeingMoved.Left, selectionBeingMoved.Right, isColumns ? 0 : selectionBeingMoved.Bottom, selectionMoveDelta.Item1);
                                    }
                                    else if (direction == -1)
                                    {
                                        text = this.Data.GetLastFillLeft(selectionBeingMoved.Left, selectionBeingMoved.Right, isColumns ? 0 : selectionBeingMoved.Bottom, -selectionBeingMoved.Width - selectionMoveDelta.Item1 + 1);
                                    }
                                    else if (direction == 2)
                                    {
                                        text = this.Data.GetLastFillBottom(selectionBeingMoved.Top, selectionBeingMoved.Bottom, isRows ? 0 : selectionBeingMoved.Right, selectionMoveDelta.Item2);
                                    }
                                    else if (direction == -2)
                                    {
                                        text = this.Data.GetLastFillTop(selectionBeingMoved.Top, selectionBeingMoved.Bottom, isRows ? 0 : selectionBeingMoved.Right, -selectionBeingMoved.Height - selectionMoveDelta.Item2 + 1);
                                    }

                                    FormattedText txt = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, defaultTypeFace, this.FontSize, this.Foreground);

                                    Point topLeft = new Point(x1 + 3, y1 + 3);

                                    if ((direction == 1 && isColumns) || (direction == -2 && !isColumns && !isRows))
                                    {
                                        topLeft = new Point(x1 + 3, y0 + 3);
                                    }
                                    else if ((direction == 2 && isRows) || (direction == -1 && !isColumns && !isRows))
                                    {
                                        topLeft = new Point(x0 + 3, y1 + 3);
                                    }
                                    else if ((direction == -1 && isColumns) || (direction == -2 && isRows))
                                    {
                                        topLeft = new Point(Math.Max(3, x0 - 9 - txt.Width), y0 + 3);
                                    }

                                    Point bottomRight = new Point(topLeft.X + 6 + txt.Width, topLeft.Y + 6 + txt.Height);

                                    topLeft = topLeft.SnapToDevicePixels(this);
                                    bottomRight = bottomRight.SnapToDevicePixels(this);

                                    context.DrawRectangle(this.Background, gridPen, new Rect(topLeft, bottomRight), 0, 0, new BoxShadows(new BoxShadow() { OffsetX = 3, OffsetY = 3, Blur = 5, Color = Color.FromArgb((byte)(this.GridColor.A * 0.5), this.GridColor.R, this.GridColor.G, this.GridColor.B), Spread = 0 }));
                                    context.DrawText(txt, new Point(topLeft.X + 3, topLeft.Y + 3));
                                }
                            }
                        }
                    }
                    else if (Selection.Count > 1)
                    {
                        using (context.PushClip(new Rect(-offsetX + 1, -offsetY + 1, this.Bounds.Width, this.Bounds.Height)))
                        {
                            for (int i = 0; i < Selection.Count; i++)
                            {
                                if (Selection[i].Left <= left + width && Selection[i].Right >= left && Selection[i].Top <= top + height && Selection[i].Bottom >= top)
                                {
                                    double x0 = Selection[i].Left - left <= 0 ? -5 : xs[Selection[i].Left - left - 1];
                                    double y0 = Selection[i].Top - top <= 0 ? -5 : ys[Selection[i].Top - top - 1];

                                    double x1 = Selection[i].Right - left < 0 ? -5 : Selection[i].Right > left + width ? xs[xs.Length - 1] + 5 : xs[Selection[i].Right - left] + 1;
                                    double y1 = Selection[i].Bottom - top < 0 ? -5 : Selection[i].Bottom > top + height ? ys[ys.Length - 1] + 5 : ys[Selection[i].Bottom - top] + 1;

                                    context.DrawRectangle(selectionWhitePen, new Rect(new Point(x0 + 1, y0 + 1).SnapToDevicePixels(this), new Point(x1 - 2, y1 - 2).SnapToDevicePixels(this)));
                                    context.FillRectangle(selectionHighlightBrush, new Rect(new Point(x0 + 2, y0 + 2).SnapToDevicePixels(this, true, true), new Point(x1 - 2, y1 - 2).SnapToDevicePixels(this, true, true)));
                                }
                            }
                        }
                    }
                }
        }


        /// <summary>
        /// Splits the line segment <c>[start, end]</c> (inclusive cell indices along the orthogonal
        /// direction) into the sub-segments that are not covered by merged ranges crossing the
        /// separator identified by <paramref name="lineIndex"/>.
        /// </summary>
        /// <param name="start">The first cell index of the segment.</param>
        /// <param name="end">The last cell index of the segment (inclusive).</param>
        /// <param name="lineIndex">The separator index. For vertical borders this is a column separator, for horizontal borders a row separator.</param>
        /// <param name="isHorizontal"><see langword="true"/> if the segment is a horizontal (row separator) line.</param>
        /// <returns>The visible sub-segments (inclusive pairs), skipping the parts covered by merged ranges.</returns>
        private IEnumerable<(int, int)> GetVisibleLineSegments(int start, int end, int lineIndex, bool isHorizontal)
        {
            if (MergedRanges == null || MergedRanges.Count == 0)
            {
                yield return (start, end);
                yield break;
            }

            List<(int, int)> blocked = null;

            foreach (SelectionRange range in MergedRanges)
            {
                bool covered;

                if (isHorizontal)
                {
                    // lineIndex is the absolute row-separator index (between row lineIndex-1 and lineIndex).
                    // The separator lies inside the merged range iff lineIndex-1 is one of the merged rows,
                    // i.e. range.Top < lineIndex && lineIndex <= range.Bottom.
                    covered = range.Top < lineIndex && range.Bottom >= lineIndex;
                }
                else
                {
                    // Same idea for column separators: inside the range iff range.Left < lineIndex <= range.Right.
                    covered = range.Left < lineIndex && range.Right >= lineIndex;
                }

                if (covered)
                {
                    int blockedFrom = isHorizontal ? range.Left : range.Top;
                    int blockedTo = isHorizontal ? range.Right : range.Bottom;

                    int from = Math.Max(start, blockedFrom);
                    int to = Math.Min(end, blockedTo);

                    if (from <= to)
                    {
                        if (blocked == null)
                        {
                            blocked = new List<(int, int)>();
                        }

                        blocked.Add((from, to));
                    }
                }
            }

            if (blocked == null)
            {
                yield return (start, end);
                yield break;
            }

            blocked.Sort();

            int current = start;

            foreach ((int from, int to) in blocked)
            {
                if (from > current)
                {
                    yield return (current, from - 1);
                }

                current = Math.Max(current, to + 1);

                if (current > end)
                {
                    yield break;
                }
            }

            if (current <= end)
            {
                yield return (current, end);
            }
        }
    }
}
