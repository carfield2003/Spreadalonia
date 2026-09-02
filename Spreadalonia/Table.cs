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


        public int MaxTableHeight { get; set; } = int.MaxValue - 2;
        public int MaxTableWidth { get; set; } = int.MaxValue - 2;


        public Dictionary<(int, int), string> Data { get; internal set; }

        /// <summary>
        /// Stores the parsed and evaluated formula data for cells that contain formulas.
        /// Key: (col, row), Value: CellData with formula info and cached value.
        /// </summary>
        public Dictionary<(int, int), CellData> FormulaCells { get; internal set; }

        /// <summary>
        /// Reference to the formula engine for evaluation.
        /// </summary>
        public FormulaEngine FormulaEngine { get; set; }

        public Dictionary<int, double> RowHeights { get; internal set; }
        public Dictionary<int, double> ColumnWidths { get; internal set; }

        public Dictionary<int, Typeface> RowTypefaces { get; internal set; }
        public Dictionary<int, Typeface> ColumnTypefaces { get; internal set; }
        public Dictionary<(int, int), Typeface> CellTypefaces { get; internal set; }

        public Dictionary<int, IBrush> RowForeground { get; internal set; }
        public Dictionary<int, IBrush> ColumnForeground { get; internal set; }
        public Dictionary<(int, int), IBrush> CellForeground { get; internal set; }

        public Dictionary<(int, int), TextAlignment> CellTextAlignment { get; internal set; }
        public Dictionary<(int, int), VerticalAlignment> CellVerticalAlignment { get; internal set; }
        public Dictionary<(int, int), Thickness> CellMargin { get; internal set; }

        /// <summary>
        /// The font size of each cell, indexed by (column, row). If a cell is not present in this
        /// dictionary, the <see cref="Table.FontSize"/> value is used.
        /// </summary>
        public Dictionary<(int, int), double> CellFontSize { get; internal set; } = new Dictionary<(int, int), double>();

        /// <summary>
        /// The custom title cells, indexed by (column, row). When a cell has a title cell, the
        /// title cell is rendered instead of the regular text content. This is used for report
        /// captions that contain multiple formatted text chunks (e.g. a title and subtitle).
        /// </summary>
        public Dictionary<(int, int), TitleCell> TitleCells { get; internal set; } = new Dictionary<(int, int), TitleCell>();

        private List<CellBackground> _cellBackgrounds = new List<CellBackground>();

        /// <summary>
        /// The backgrounds to be applied to ranges of cells. Backgrounds are drawn below the grid lines.
        /// </summary>
        public List<CellBackground> CellBackgrounds
        {
            get { return _cellBackgrounds; }
            set { _cellBackgrounds = value ?? new List<CellBackground>(); InvalidateVisual(); }
        }

        private List<CellBorder> _cellBorders = new List<CellBorder>();

        /// <summary>
        /// The border line segments to be drawn on top of the grid lines.
        /// </summary>
        public List<CellBorder> CellBorders
        {
            get { return _cellBorders; }
            set { _cellBorders = value ?? new List<CellBorder>(); InvalidateVisual(); }
        }

        /// <summary>
        /// Adds a background to a range of cells and schedules a redraw.
        /// </summary>
        /// <param name="range">The range of cells the background is applied to.</param>
        /// <param name="brush">The brush used to fill the background.</param>
        public void AddCellBackground(SelectionRange range, IBrush brush)
        {
            _cellBackgrounds.Add(new CellBackground(range, brush));
            InvalidateVisual();
        }

        /// <summary>
        /// Removes all cell backgrounds and schedules a redraw.
        /// </summary>
        public void ClearCellBackgrounds()
        {
            _cellBackgrounds.Clear();
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
                _cellBorders.Add(border);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Removes all border line segments and schedules a redraw.
        /// </summary>
        public void ClearCellBorders()
        {
            _cellBorders.Clear();
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

        public static readonly StyledProperty<ImmutableList<SelectionRange>> MergedRangesProperty = AvaloniaProperty.Register<Table, ImmutableList<SelectionRange>>(nameof(MergedRanges), ImmutableList.Create<SelectionRange>());

        /// <summary>
        /// The ranges of cells that are merged together. The content of the top-left cell of each
        /// range is drawn centred over the whole merged range, and grid lines inside the merged
        /// range are suppressed.
        /// </summary>
        public ImmutableList<SelectionRange> MergedRanges
        {
            get { return GetValue(MergedRangesProperty); }
            set { SetValue(MergedRangesProperty, value ?? ImmutableList.Create<SelectionRange>()); }
        }

        static Table()
        {
            AffectsRender<Table>(OffsetProperty, DefaultColumnWidthProperty, DefaultRowHeightProperty, GridColorProperty, SelectionProperty, ForegroundProperty, BackgroundProperty, FontFamilyProperty, FontSizeProperty, FontStyleProperty, FontWeightProperty, SelectionAccentProperty, IsFocusedProperty, DefaultMarginProperty, MergedRangesProperty);
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

            while (currWidth < x)
            {
                currWidth += GetWidth(leftIndex);
                leftIndex++;
            }

            int topIndex = 0;
            double currHeight = 0;

            while (currHeight < y)
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
            while (currWidth + nextWidth < x)
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
            while (currHeight + nextHeight < y)
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
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            PointerPoint currentPoint = e.GetCurrentPoint(this);
            double xPos = currentPoint.Position.X - lastDrawnDeltaX;
            double yPos = currentPoint.Position.Y - lastDrawnDeltaY;


            if (!currentPoint.Properties.IsLeftButtonPressed && !currentPoint.Properties.IsRightButtonPressed)
            {
                if (Selection.Count == 1)
                {
                    double selectionLeft = 0;
                    double selectionTop = 0;
                    double selectionRight = 0;
                    double selectionBottom = 0;

                    if (Selection[0].Left >= lastDrawnLeft && Selection[0].Left <= lastDrawnLeft + lastDrawnWidth)
                    {
                        selectionLeft = Selection[0].Left == lastDrawnLeft ? 0 : lastDrawnXs[Selection[0].Left - lastDrawnLeft - 1];
                    }
                    else if (Selection[0].Left < lastDrawnLeft)
                    {
                        selectionLeft = double.NegativeInfinity;
                    }
                    else if (Selection[0].Left > lastDrawnLeft + lastDrawnWidth)
                    {
                        selectionLeft = double.PositiveInfinity;
                    }

                    if (Selection[0].Right >= lastDrawnLeft && Selection[0].Right <= lastDrawnLeft + lastDrawnWidth)
                    {
                        selectionRight = lastDrawnXs[Selection[0].Right - lastDrawnLeft];
                    }
                    else if (Selection[0].Right < lastDrawnLeft)
                    {
                        selectionRight = double.NegativeInfinity;
                    }
                    else if (Selection[0].Right > lastDrawnLeft + lastDrawnWidth)
                    {
                        selectionRight = double.PositiveInfinity;
                    }

                    if (Selection[0].Top >= lastDrawnTop && Selection[0].Top <= lastDrawnTop + lastDrawnHeight)
                    {
                        selectionTop = Selection[0].Top == lastDrawnTop ? 0 : lastDrawnYs[Selection[0].Top - lastDrawnTop - 1];
                    }
                    else if (Selection[0].Top < lastDrawnTop)
                    {
                        selectionTop = double.NegativeInfinity;
                    }
                    else if (Selection[0].Top > lastDrawnTop + lastDrawnHeight)
                    {
                        selectionTop = double.PositiveInfinity;
                    }

                    if (Selection[0].Bottom >= lastDrawnTop && Selection[0].Bottom <= lastDrawnTop + lastDrawnHeight)
                    {
                        selectionBottom = lastDrawnYs[Selection[0].Bottom - lastDrawnTop];
                    }
                    else if (Selection[0].Bottom < lastDrawnTop)
                    {
                        selectionBottom = double.NegativeInfinity;
                    }
                    else if (Selection[0].Bottom > lastDrawnTop + lastDrawnHeight)
                    {
                        selectionBottom = double.PositiveInfinity;
                    }

                    bool found = (Math.Abs(xPos - selectionLeft) <= 3 && yPos >= selectionTop && yPos <= selectionBottom) || (Math.Abs(xPos - selectionRight) <= 3 && yPos >= selectionTop && yPos <= selectionBottom) ||
                        (Math.Abs(yPos - selectionTop) <= 3 && xPos >= selectionLeft && xPos <= selectionRight) || (Math.Abs(yPos - selectionBottom) <= 3 && xPos >= selectionLeft && xPos <= selectionRight);

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
                            this.Container.Selection = this.Container.Selection.SetItem(this.Container.Selection.Count - 1, new SelectionRange(Math.Min(x + lastDrawnLeft, selectionStart.Item1), Math.Min(y + lastDrawnTop, selectionStart.Item2), Math.Max(x + lastDrawnLeft, selectionStart.Item1), Math.Max(y + lastDrawnTop, selectionStart.Item2)));
                        }
                        else if (selectionMode == 1)
                        {
                            this.Container.Selection = previousSelection.Difference(new SelectionRange(Math.Min(x + lastDrawnLeft, selectionStart.Item1), Math.Min(y + lastDrawnTop, selectionStart.Item2), Math.Max(x + lastDrawnLeft, selectionStart.Item1), Math.Max(y + lastDrawnTop, selectionStart.Item2)));
                        }
                    }
                    else if (pointerPressedAction == 2)
                    {
                        if (selectionBeingMoved.IsFinite(this))
                        {
                            (int, int) newSelectionMoveDelta = (Math.Max(-selectionStart.Item1 + x + lastDrawnLeft, -selectionBeingMoved.Left), Math.Max(-selectionStart.Item2 + y + lastDrawnTop, -selectionBeingMoved.Top));

                            if (newSelectionMoveDelta != selectionMoveDelta)
                            {
                                selectionMoveDelta = newSelectionMoveDelta;
                                this.InvalidateVisual();
                            }
                        }
                        else if (selectionBeingMoved.IsRows(this) && !selectionBeingMoved.IsColumns(this))
                        {
                            (int, int) newSelectionMoveDelta = (0, Math.Max(-selectionStart.Item2 + y + lastDrawnTop, -selectionBeingMoved.Top));

                            if (newSelectionMoveDelta != selectionMoveDelta)
                            {
                                selectionMoveDelta = newSelectionMoveDelta;
                                this.InvalidateVisual();
                            }
                        }
                        else if (selectionBeingMoved.IsColumns(this) && !selectionBeingMoved.IsRows(this))
                        {
                            (int, int) newSelectionMoveDelta = (Math.Max(-selectionStart.Item1 + x + lastDrawnLeft, -selectionBeingMoved.Left), 0);

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
                            (int, int) newSelectionMoveDelta = (Math.Max(-selectionStart.Item1 + x + lastDrawnLeft, -selectionBeingMoved.Left - selectionBeingMoved.Width), Math.Max(-selectionStart.Item2 + y + lastDrawnTop, -selectionBeingMoved.Top - selectionBeingMoved.Height));

                            if (newSelectionMoveDelta != selectionMoveDelta)
                            {
                                selectionMoveDelta = newSelectionMoveDelta;
                                this.InvalidateVisual();
                            }
                        }
                        else if (selectionBeingMoved.IsRows(this) && !selectionBeingMoved.IsColumns(this))
                        {
                            (int, int) newSelectionMoveDelta = (0, Math.Max(-selectionStart.Item2 + y + lastDrawnTop, -selectionBeingMoved.Top - selectionBeingMoved.Height));

                            if (newSelectionMoveDelta != selectionMoveDelta)
                            {
                                selectionMoveDelta = newSelectionMoveDelta;
                                this.InvalidateVisual();
                            }
                        }
                        else if (selectionBeingMoved.IsColumns(this) && !selectionBeingMoved.IsRows(this))
                        {
                            (int, int) newSelectionMoveDelta = (Math.Max(-selectionStart.Item1 + x + lastDrawnLeft, -selectionBeingMoved.Left - selectionBeingMoved.Width), 0);

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
                        this.Data.FillBottom(selectionBeingMoved, selectionMoveDelta.Item2, this.Container.UndoStack);
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
                        this.Data.FillTop(selectionBeingMoved, -selectionBeingMoved.Height - selectionMoveDelta.Item2 + 1, this.Container.UndoStack);
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

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            PointerPoint currentPoint = e.GetCurrentPoint(this);
            double xPos = currentPoint.Position.X - lastDrawnDeltaX;
            double yPos = currentPoint.Position.Y - lastDrawnDeltaY;

            bool moving = false;
            bool fill = false;

            if (Selection.Count == 1)
            {
                double selectionLeft = 0;
                double selectionTop = 0;
                double selectionRight = 0;
                double selectionBottom = 0;

                if (Selection[0].Left >= lastDrawnLeft && Selection[0].Left <= lastDrawnLeft + lastDrawnWidth)
                {
                    selectionLeft = Selection[0].Left == lastDrawnLeft ? 0 : lastDrawnXs[Selection[0].Left - lastDrawnLeft - 1];
                }
                else if (Selection[0].Left < lastDrawnLeft)
                {
                    selectionLeft = double.NegativeInfinity;
                }
                else if (Selection[0].Left > lastDrawnLeft + lastDrawnWidth)
                {
                    selectionLeft = double.PositiveInfinity;
                }

                if (Selection[0].Right >= lastDrawnLeft && Selection[0].Right <= lastDrawnLeft + lastDrawnWidth)
                {
                    selectionRight = lastDrawnXs[Selection[0].Right - lastDrawnLeft];
                }
                else if (Selection[0].Right < lastDrawnLeft)
                {
                    selectionRight = double.NegativeInfinity;
                }
                else if (Selection[0].Right > lastDrawnLeft + lastDrawnWidth)
                {
                    selectionRight = double.PositiveInfinity;
                }

                if (Selection[0].Top >= lastDrawnTop && Selection[0].Top <= lastDrawnTop + lastDrawnHeight)
                {
                    selectionTop = Selection[0].Top == lastDrawnTop ? 0 : lastDrawnYs[Selection[0].Top - lastDrawnTop - 1];
                }
                else if (Selection[0].Top < lastDrawnTop)
                {
                    selectionTop = double.NegativeInfinity;
                }
                else if (Selection[0].Top > lastDrawnTop + lastDrawnHeight)
                {
                    selectionTop = double.PositiveInfinity;
                }

                if (Selection[0].Bottom >= lastDrawnTop && Selection[0].Bottom <= lastDrawnTop + lastDrawnHeight)
                {
                    selectionBottom = lastDrawnYs[Selection[0].Bottom - lastDrawnTop];
                }
                else if (Selection[0].Bottom < lastDrawnTop)
                {
                    selectionBottom = double.NegativeInfinity;
                }
                else if (Selection[0].Bottom > lastDrawnTop + lastDrawnHeight)
                {
                    selectionBottom = double.PositiveInfinity;
                }

                moving = (Math.Abs(xPos - selectionLeft) <= 3 && yPos >= selectionTop && yPos <= selectionBottom) || (Math.Abs(xPos - selectionRight) <= 3 && yPos >= selectionTop && yPos <= selectionBottom) ||
                    (Math.Abs(yPos - selectionTop) <= 3 && xPos >= selectionLeft && xPos <= selectionRight) || (Math.Abs(yPos - selectionBottom) <= 3 && xPos >= selectionLeft && xPos <= selectionRight);

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

            if (fill)
            {
                selectionStart = (Math.Max(Selection[0].Left, Math.Min(Selection[0].Right, x + lastDrawnLeft)), Math.Max(Selection[0].Top, Math.Min(Selection[0].Bottom, y + lastDrawnTop)));

                selectionMoveDelta = (0, 0);
                selectionBeingMoved = Selection[0];
                pointerPressedAction = 3;
                this.InvalidateVisual();
            }
            else if (moving)
            {
                selectionStart = (Math.Max(Selection[0].Left, Math.Min(Selection[0].Right, x + lastDrawnLeft)), Math.Max(Selection[0].Top, Math.Min(Selection[0].Bottom, y + lastDrawnTop)));


                selectionMoveDelta = (0, 0);
                selectionBeingMoved = Selection[0];
                pointerPressedAction = 2;
                this.InvalidateVisual();
            }
            else
            {
                // If the pressed cell belongs to a merged range, redirect the interaction to the
                // top-left cell of the merge so that selection and editing act on the whole range.
                SelectionRange pressedSelection = new SelectionRange(x + lastDrawnLeft, y + lastDrawnTop, x + lastDrawnLeft, y + lastDrawnTop);

                if (x >= 0 && y >= 0 && MergedRanges != null && MergedRanges.Count > 0)
                {
                    int pressedColumn = x + lastDrawnLeft;
                    int pressedRow = y + lastDrawnTop;

                    foreach (SelectionRange mergedRange in MergedRanges)
                    {
                        if (pressedColumn >= mergedRange.Left && pressedColumn <= mergedRange.Right &&
                            pressedRow >= mergedRange.Top && pressedRow <= mergedRange.Bottom)
                        {
                            x = mergedRange.Left - lastDrawnLeft;
                            y = mergedRange.Top - lastDrawnTop;
                            pressedSelection = mergedRange;
                            break;
                        }
                    }
                }

                if (currentPoint.Properties.IsLeftButtonPressed && e.ClickCount == 1)
                {
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
                            selectionStart = (x + lastDrawnLeft, y + lastDrawnTop);
                            selectionMode = 0;
                        }
                        else
                        {
                            previousSelection = this.Container.Selection;
                            this.Container.Selection = this.Container.Selection.Difference(pressedSelection);
                            selectionStart = (x + lastDrawnLeft, y + lastDrawnTop);
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

                        if (x + lastDrawnLeft >= prevStartColumn)
                        {
                            selStartX = prevStartColumn;
                        }
                        else
                        {
                            selStartX = prevEndColumn;
                        }

                        int selStartY;

                        if (y + lastDrawnTop >= prevStartRow)
                        {
                            selStartY = prevStartRow;
                        }
                        else
                        {
                            selStartY = prevEndRow;
                        }

                        selectionStart = (selStartX, selStartY);
                        this.Container.Selection = this.Selection.SetItem(this.Selection.Count - 1, new SelectionRange(Math.Min(selStartX, x + lastDrawnLeft), Math.Min(selStartY, y + lastDrawnTop), Math.Max(selStartX, x + lastDrawnLeft), Math.Max(selStartY, y + lastDrawnTop)));
                        selectionMode = 0;
                    }
                    else
                    {
                        this.Container.Selection = ImmutableList.Create(pressedSelection);
                        selectionStart = (x + lastDrawnLeft, y + lastDrawnTop);
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

                    selectionStart = (x + lastDrawnLeft, y + lastDrawnTop);
                    selectionMode = 0;
                    pointerPressedAction = 0;
                }
                else if (currentPoint.Properties.IsLeftButtonPressed && e.ClickCount == 2)
                {
                    (int, int) cell = (x + lastDrawnLeft, y + lastDrawnTop);

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

            using (context.PushTransform(Matrix.CreateTranslation(-1, -1)))
            {
                (int left, double offsetX, int top, double offsetY, int width, double actualWidth, double startWidth, int height, double actualHeight, double startHeight) = GetRange(this.Offset.X, this.Offset.Y, this.Bounds.Width, this.Bounds.Height);

                lastDrawnDeltaX = offsetX;
                lastDrawnDeltaY = offsetY;

                lastDrawnTop = top;
                lastDrawnLeft = left;
                lastDrawnWidth = width;
                lastDrawnHeight = height;

                Pen gridPen = new Pen(new SolidColorBrush(GridColor));
                Pen blackPen = new Pen(Brushes.Black);
                Pen selectionPen = new Pen(SelectionAccent, 2);
                Pen selectionMovePen = new Pen(SelectionAccent, 3);
                Pen selectionWhitePen = new Pen(this.Background, 1);
                Brush selectionHighlightBrush = new SolidColorBrush(Color.FromArgb(40, 0, 0, 0));
                Brush clearingBrush = new SolidColorBrush(this.Background.Color, 0.5);
                BoxShadows shadows = new BoxShadows(new BoxShadow() { Blur = 5, OffsetX = 1, OffsetY = 1, Color = Color.FromArgb(128, 0, 0, 0) });

                Typeface defaultTypeFace = new Typeface(this.FontFamily, this.FontStyle, this.FontWeight);

                using (context.PushTransform(Matrix.CreateTranslation(offsetX, offsetY)))
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

                        lastDrawnXs = xs;
                        lastDrawnYs = ys;

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
                        if (_cellBackgrounds.Count > 0)
                        {
                            foreach (CellBackground cellBackground in _cellBackgrounds)
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
                            for (int x = 0; x <= width; x++)
                            {
                                if (Data.TryGetValue((left + x, top + y), out string txt))
                                {
                                    if (mergedCells != null && mergedCells.Contains((left + x, top + y)))
                                    {
                                        continue;
                                    }

                                    if (!Container.IsEditing || left + x != Container.EditingCell.Item1 || top + y != Container.EditingCell.Item2)
                                    {
                                        // Title cells render their own content and override the default text drawing.
                                        if (TitleCells.TryGetValue((left + x, top + y), out TitleCell titleCell))
                                        {
                                            double titleX = x == 0 ? 0 : xs[x - 1];
                                            double titleY = y == 0 ? 0 : ys[y - 1];
                                            titleCell.Render(context, new Rect(titleX, titleY, xs[x] - titleX, ys[y] - titleY), null, null);
                                            continue;
                                        }

                                        // Determine display text: use formula result if available, otherwise raw text
                                        string displayText = txt;
                                        if (FormulaCells != null && FormulaCells.TryGetValue((left + x, top + y), out CellData formulaCell))
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

                                        using (context.PushClip(new Rect(realX + margin.Left, realY + margin.Top, xs[x] - realX - margin.Left - margin.Right, ys[y] - realY - margin.Top - margin.Bottom)))
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

                                bool hasMergedData = Data.TryGetValue((mergedRange.Left, mergedRange.Top), out string mergedText);
                                bool hasMergedTitleCell = TitleCells.TryGetValue((mergedRange.Left, mergedRange.Top), out TitleCell mergedTitleCell);
                                if (!hasMergedData && !hasMergedTitleCell)
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
                                    SelectionRange titleVisibleMerge = mergedRange.Intersection(new SelectionRange(left, top, left + width, top + height));

                                    double titleMergeX0 = titleVisibleMerge.Left == left ? 0 : xs[titleVisibleMerge.Left - left - 1];
                                    double titleMergeY0 = titleVisibleMerge.Top == top ? 0 : ys[titleVisibleMerge.Top - top - 1];
                                    double titleMergeX1 = titleVisibleMerge.Right - left < width ? xs[titleVisibleMerge.Right - left] : xs[width];
                                    double titleMergeY1 = titleVisibleMerge.Bottom - top < height ? ys[titleVisibleMerge.Bottom - top] : ys[height];

                                    mergedTitleCell.Render(context, new Rect(titleMergeX0, titleMergeY0, titleMergeX1 - titleMergeX0, titleMergeY1 - titleMergeY0), null, null);
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

                                // Clip the merged range to the visible area so partial merges still render
                                SelectionRange visibleMerge = mergedRange.Intersection(new SelectionRange(left, top, left + width, top + height));

                                double mergedX0 = visibleMerge.Left == left ? 0 : xs[visibleMerge.Left - left - 1];
                                double mergedY0 = visibleMerge.Top == top ? 0 : ys[visibleMerge.Top - top - 1];
                                double mergedX1 = visibleMerge.Right - left < width ? xs[visibleMerge.Right - left] : xs[width];
                                double mergedY1 = visibleMerge.Bottom - top < height ? ys[visibleMerge.Bottom - top] : ys[height];

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

                        // Draw borders on top of the grid lines
                        if (_cellBorders.Count > 0)
                        {
                            foreach (CellBorder border in _cellBorders)
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

                            if (!Container.IsEditing)
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

                _ = Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    this.Container.SetScrollbarMaximum(actualWidth + startWidth - this.Bounds.Width + GetWidth(left + width), actualHeight + startHeight - this.Bounds.Height + GetHeight(top + height));
                });
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
