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
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Spreadalonia
{
    internal class HorizontalHeader : Control
    {
        public static readonly StyledProperty<Spreadsheet> ContainerProperty = AvaloniaProperty.Register<HorizontalHeader, Spreadsheet>(nameof(Container));
        public Spreadsheet Container
        {
            get { return GetValue(ContainerProperty); }
            set { SetValue(ContainerProperty, value); }
        }

        public static readonly StyledProperty<Table> OwnerProperty = AvaloniaProperty.Register<HorizontalHeader, Table>(nameof(Owner));
        public Table Owner
        {
            get { return GetValue(OwnerProperty); }
            set { SetValue(OwnerProperty, value); }
        }

        public static readonly StyledProperty<Vector> OffsetProperty = AvaloniaProperty.Register<HorizontalHeader, Vector>(nameof(Offset), new Vector(0, 0));
        public Vector Offset
        {
            get { return GetValue(OffsetProperty); }
            set { SetValue(OffsetProperty, value); }
        }

        public static readonly StyledProperty<Color> GridColorProperty = AvaloniaProperty.Register<HorizontalHeader, Color>(nameof(GridColor), Color.FromRgb(220, 220, 220));
        public Color GridColor
        {
            get { return GetValue(GridColorProperty); }
            set { SetValue(GridColorProperty, value); }
        }

        public static readonly StyledProperty<Color> HeaderBackgroundProperty = AvaloniaProperty.Register<HorizontalHeader, Color>(nameof(HeaderBackground), Color.FromRgb(240, 240, 240));
        public Color HeaderBackground
        {
            get { return GetValue(HeaderBackgroundProperty); }
            set { SetValue(HeaderBackgroundProperty, value); }
        }

        public static readonly StyledProperty<Pen> HeaderSeparatorProperty = AvaloniaProperty.Register<HorizontalHeader, Pen>(nameof(HeaderSeparator), new Pen(new SolidColorBrush(Color.FromRgb(170, 170, 170))));
        public Pen HeaderSeparator
        {
            get { return GetValue(HeaderSeparatorProperty); }
            set { SetValue(HeaderSeparatorProperty, value); }
        }

        public static readonly StyledProperty<ImmutableList<SelectionRange>> SelectionProperty = AvaloniaProperty.Register<HorizontalHeader, ImmutableList<SelectionRange>>(nameof(Selection), ImmutableList.Create<SelectionRange>());
        public ImmutableList<SelectionRange> Selection
        {
            get { return GetValue(SelectionProperty); }
            set { SetValue(SelectionProperty, value); }
        }

        public static readonly StyledProperty<SolidColorBrush> SelectionAccentProperty = AvaloniaProperty.Register<HorizontalHeader, SolidColorBrush>(nameof(SelectionAccent), new SolidColorBrush(Color.FromRgb(0, 114, 176)));
        public SolidColorBrush SelectionAccent
        {
            get { return GetValue(SelectionAccentProperty); }
            set { SetValue(SelectionAccentProperty, value); }
        }

        public static readonly StyledProperty<FontFamily> FontFamilyProperty = UserControl.FontFamilyProperty.AddOwner<HorizontalHeader>();
        public FontFamily FontFamily
        {
            get { return GetValue(FontFamilyProperty); }
            set { SetValue(FontFamilyProperty, value); }
        }

        public static readonly StyledProperty<double> FontSizeProperty = UserControl.FontSizeProperty.AddOwner<HorizontalHeader>();
        public double FontSize
        {
            get { return GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public static readonly StyledProperty<FontStyle> FontStyleProperty = UserControl.FontStyleProperty.AddOwner<HorizontalHeader>();
        public FontStyle FontStyle
        {
            get { return GetValue(FontStyleProperty); }
            set { SetValue(FontStyleProperty, value); }
        }

        public static readonly StyledProperty<FontWeight> FontWeightProperty = UserControl.FontWeightProperty.AddOwner<HorizontalHeader>();
        public FontWeight FontWeight
        {
            get { return GetValue(FontWeightProperty); }
            set { SetValue(FontWeightProperty, value); }
        }

        public static readonly StyledProperty<IBrush> ForegroundProperty = UserControl.ForegroundProperty.AddOwner<HorizontalHeader>();
        public IBrush Foreground
        {
            get { return GetValue(ForegroundProperty); }
            set { SetValue(ForegroundProperty, value); }
        }


        static HorizontalHeader()
        {
            AffectsRender<HorizontalHeader>(OffsetProperty, GridColorProperty, HeaderBackgroundProperty, SelectionProperty, SelectionAccentProperty);
        }

        public HorizontalHeader()
        {
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

        protected override Size MeasureOverride(Size availableSize)
        {
            FormattedText fmt = new FormattedText("ABCDEFGHIJKLMNOPQRSTUVWXYZ", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(this.FontFamily), this.FontSize, Foreground);

            if (this.FontFamily != null)
            {
                return new Size(availableSize.Width, fmt.Height + 7);
            }
            else
            {
                return new Size(availableSize.Width, this.FontSize + 7);
            }
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);

            if (hoverColumn >= 0)
            {
                hoverColumn = -1;
                this.InvalidateVisual();
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            PointerPoint currentPoint = e.GetCurrentPoint(this);

            double xPos = currentPoint.Position.X - lastDrawnDelta;

            if (pointerPressedAction == 2)
            {
                double newWidth = Math.Max(10, originalColumnWidth + currentPoint.Position.X - dragStartX);

                if (newWidth != originalColumnWidth)
                {
                    this.Container.UndoStackColumnWidth.Push(new ValueStackFrame<int, double>(this.Container.Selection, new Dictionary<int, Reference<double>>() { { selectionStartColumn, wasOriginalColumnWidthDefault ? null : new Reference<double>(originalColumnWidth) } }, new Dictionary<int, Reference<double>>() { { selectionStartColumn, newWidth } }));
                    this.Container.UndoStack.Push(null);

                    this.Container.UndoStackRowForeground.Push(null);
                    this.Container.UndoStackRowHeight.Push(null);
                    this.Container.UndoStackRowTypeface.Push(null);

                    this.Container.UndoStackColumnForeground.Push(null);
                    this.Container.UndoStackColumnTypeface.Push(null);

                    this.Container.UndoStackCellForeground.Push(null);
                    this.Container.UndoStackCellMargin.Push(null);
                    this.Container.UndoStackCellHorizontalAlignment.Push(null);
                    this.Container.UndoStackCellTypeface.Push(null);
                    this.Container.UndoStackCellVerticalAlignment.Push(null);

                    this.Container.ClearRedoStack();

                    if (this.Container.Selection.Count > 0 && this.Container.Selection[0].Left == selectionStartColumn)
                    {
                        this.Container.RaiseCellSizeChanged();
                    }
                }
            }

            // The action ends with the drag: the pointer handlers must not resize on hover.
            pointerPressedAction = 0;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            PointerPoint currentPoint = e.GetCurrentPoint(this);

            (int clickedColumn, double closest) = ResolveColumnHit(currentPoint.Position.X);

            // A running resize drag must not depend on the hit testing: past the last drawn edge of a
            // fixed-size table there is no column to hit, but the drag has to keep working. The
            // button state still decides whether this is a drag at all: pointerPressedAction stays
            // set after the button has been released, so hovering must not keep resizing.
            if (pointerPressedAction == 2 && currentPoint.Properties.IsLeftButtonPressed)
            {
                double newWidth = Math.Max(10, originalColumnWidth + currentPoint.Position.X - dragStartX);

                Owner.ColumnWidths[selectionStartColumn] = newWidth;
                Owner.InvalidateVisual();
                this.InvalidateVisual();
            }
            else if (clickedColumn >= 0)
            {
                if (!currentPoint.Properties.IsLeftButtonPressed)
                {
                    if (closest <= 5)
                    {
                        this.Cursor = Cursors.ResizeColumnCursor((this.VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1);
                    }
                    else
                    {
                        this.Cursor = Cursors.SelectColumnCursor((this.VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1);
                    }

                    if (hoverColumn != clickedColumn)
                    {
                        hoverColumn = clickedColumn;
                        this.InvalidateVisual();
                    }

                }
                else
                {
                    if (pointerPressedAction == 1)
                    {
                        if (selectionMode == 0)
                        {
                            this.Container.Selection = this.Selection.SetItem(this.Selection.Count - 1, new SelectionRange(Math.Min(selectionStartColumn, clickedColumn), 0, Math.Max(selectionStartColumn, clickedColumn), Owner.MaxTableHeight));
                        }
                        else if (selectionMode == 1)
                        {
                            this.Container.Selection = previousSelection.Difference(new SelectionRange(Math.Min(selectionStartColumn, clickedColumn), 0, Math.Max(selectionStartColumn, clickedColumn), Owner.MaxTableHeight));
                        }
                    }
                }
            }
        }

        int pointerPressedAction = 0;
        int selectionStartColumn = -1;
        int selectionMode = 0;
        double dragStartX = 0;
        double originalColumnWidth = 0;
        bool wasOriginalColumnWidthDefault = false;
        ImmutableList<SelectionRange> previousSelection;

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            PointerPoint currentPoint = e.GetCurrentPoint(this);

            // Same hit test as OnPointerMoved; a press within 5px of a drawn column edge starts a
            // resize instead of selecting the whole column.
            (int clickedColumn, bool isResize) = ResolveColumnPress(currentPoint.Position.X);

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                if (!isResize)
                {
                    if (e.KeyModifiers == Spreadsheet.ControlModifier)
                    {
                        bool found = false;

                        for (int i = 0; i < this.Selection.Count; i++)
                        {
                            if (this.Selection[i].Top == 0 && this.Selection[i].Bottom == Owner.MaxTableHeight && this.Selection[i].Left <= clickedColumn && this.Selection[i].Right >= clickedColumn)
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                        {
                            this.Container.Selection = this.Container.Selection.Add(new SelectionRange(clickedColumn, 0, clickedColumn, Owner.MaxTableHeight));
                            selectionStartColumn = clickedColumn;
                            selectionMode = 0;
                        }
                        else
                        {
                            previousSelection = this.Container.Selection;
                            this.Container.Selection = this.Container.Selection.Difference(new SelectionRange(clickedColumn, 0, clickedColumn, Owner.MaxTableHeight));
                            selectionStartColumn = clickedColumn;
                            selectionMode = 1;
                        }
                    }
                    else if (e.KeyModifiers == KeyModifiers.Shift && this.Selection.Count > 0)
                    {
                        int prevStartColumn = this.Selection[this.Selection.Count - 1].Left;
                        int prevEndColumn = this.Selection[this.Selection.Count - 1].Right;

                        if (clickedColumn >= prevStartColumn)
                        {
                            selectionStartColumn = prevStartColumn;
                        }
                        else
                        {
                            selectionStartColumn = prevEndColumn;
                        }

                        this.Container.Selection = this.Selection.SetItem(this.Selection.Count - 1, new SelectionRange(Math.Min(selectionStartColumn, clickedColumn), 0, Math.Max(selectionStartColumn, clickedColumn), Owner.MaxTableHeight));
                        selectionMode = 0;
                    }
                    else
                    {
                        this.Container.Selection = ImmutableList.Create(new SelectionRange(clickedColumn, 0, clickedColumn, Owner.MaxTableHeight));
                        selectionStartColumn = clickedColumn;
                        selectionMode = 0;
                    }

                    this.Cursor = Cursors.CrossCursor((this.VisualRoot as ILayoutRoot)?.LayoutScaling ?? 1);
                    pointerPressedAction = 1;
                }
                else
                {
                    if (e.ClickCount == 1)
                    {
                        pointerPressedAction = 2;

                        selectionStartColumn = clickedColumn;

                        originalColumnWidth = Owner.GetWidth(clickedColumn, out wasOriginalColumnWidthDefault);

                        // The drag distance is measured in screen coordinates, so that it stays
                        // correct even when the pointer leaves the frozen column strip.
                        dragStartX = currentPoint.Position.X;
                    }
                    else if (e.ClickCount == 2)
                    {
                        AutoFitWidth(clickedColumn);
                        if (this.Container.Selection.Count > 0 && this.Container.Selection[0].Left == clickedColumn)
                        {
                            this.Container.RaiseCellSizeChanged();
                        }
                    }
                }
            }
            else if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                bool found = false;

                for (int i = 0; i < this.Selection.Count; i++)
                {
                    if (this.Selection[i].Top == 0 && this.Selection[i].Bottom == Owner.MaxTableHeight && this.Selection[i].Left <= clickedColumn && this.Selection[i].Right >= clickedColumn)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    this.Container.Selection = ImmutableList.Create(new SelectionRange(clickedColumn, 0, clickedColumn, Owner.MaxTableHeight));
                }
            }
        }

        private void AutoFitWidth(int column)
        {
            bool found = false;
            double maxWidth = 10;

            Typeface defaultTypeFace = new Typeface(Owner.FontFamily, Owner.FontStyle, Owner.FontWeight);

            foreach (KeyValuePair<(int, int), string> kvp in Owner.Data)
            {
                if (kvp.Key.Item1 == column)
                {
                    if (!Owner.CellTypefaces.TryGetValue(kvp.Key, out Typeface face) &&
                            !Owner.RowTypefaces.TryGetValue(kvp.Key.Item2, out face) &&
                            !Owner.ColumnTypefaces.TryGetValue(kvp.Key.Item1, out face))
                    {
                        face = defaultTypeFace;
                    }

                    if (!Owner.CellMargin.TryGetValue(kvp.Key, out Thickness margin))
                    {
                        margin = Owner.DefaultMargin;
                    }

                    FormattedText fmtText = new FormattedText(kvp.Value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, face, this.Container.FontSize, Foreground);

                    double textWidth = fmtText.Width;

                    if (this.Container.ShowColorPreview && kvp.Value.StartsWith("#") && (kvp.Value.Length == 7 || kvp.Value.Length == 9))
                    {
                        try
                        {
                            Brush.Parse(kvp.Value.Length == 7 ? kvp.Value : ("#" + kvp.Value.Substring(7, 2) + kvp.Value.Substring(1, 6)));
                            textWidth += this.Owner.FontSize + 3;
                        }
                        catch { }
                    }

                    found = true;
                    maxWidth = Math.Max(maxWidth, textWidth + margin.Left + margin.Right);
                }
            }

            if (found)
            {
                double originalWidth = this.Owner.GetWidth(column, out bool wasDefault);

                if (originalWidth != maxWidth)
                {
                    Owner.ColumnWidths[column] = maxWidth;
                    Owner.InvalidateVisual();
                    this.InvalidateVisual();

                    this.Container.UndoStackColumnWidth.Push(new ValueStackFrame<int, double>(this.Container.Selection, new Dictionary<int, Reference<double>>() { { column, wasDefault ? null : new Reference<double>(originalWidth) } }, new Dictionary<int, Reference<double>>() { { column, maxWidth } }));
                    this.Container.UndoStack.Push(null);

                    this.Container.UndoStackRowForeground.Push(null);
                    this.Container.UndoStackRowHeight.Push(null);
                    this.Container.UndoStackRowTypeface.Push(null);

                    this.Container.UndoStackColumnForeground.Push(null);
                    this.Container.UndoStackColumnTypeface.Push(null);

                    this.Container.UndoStackCellForeground.Push(null);
                    this.Container.UndoStackCellMargin.Push(null);
                    this.Container.UndoStackCellHorizontalAlignment.Push(null);
                    this.Container.UndoStackCellTypeface.Push(null);
                    this.Container.UndoStackCellVerticalAlignment.Push(null);

                    this.Container.ClearRedoStack();
                }
            }
            else
            {
                double originalWidth = this.Owner.GetWidth(column, out bool wasDefault);

                if (!wasDefault)
                {
                    Owner.ColumnWidths.Remove(column);
                    Owner.InvalidateVisual();
                    this.InvalidateVisual();

                    this.Container.UndoStackColumnWidth.Push(new ValueStackFrame<int, double>(this.Container.Selection, new Dictionary<int, Reference<double>>() { { column, originalWidth } }, new Dictionary<int, Reference<double>>() { { column, null } }));
                    this.Container.UndoStack.Push(null);

                    this.Container.UndoStackRowForeground.Push(null);
                    this.Container.UndoStackRowHeight.Push(null);
                    this.Container.UndoStackRowTypeface.Push(null);

                    this.Container.UndoStackColumnForeground.Push(null);
                    this.Container.UndoStackColumnTypeface.Push(null);

                    this.Container.UndoStackCellForeground.Push(null);
                    this.Container.UndoStackCellMargin.Push(null);
                    this.Container.UndoStackCellHorizontalAlignment.Push(null);
                    this.Container.UndoStackCellTypeface.Push(null);
                    this.Container.UndoStackCellVerticalAlignment.Push(null);

                    this.Container.ClearRedoStack();
                }
            }
        }

        internal Reference<double> AutoFitWidthWithoutStack(int column)
        {
            bool found = false;
            double maxWidth = 10;

            Typeface defaultTypeFace = new Typeface(Owner.FontFamily, Owner.FontStyle, Owner.FontWeight);

            foreach (KeyValuePair<(int, int), string> kvp in Owner.Data)
            {
                if (kvp.Key.Item1 == column)
                {
                    if (!Owner.CellTypefaces.TryGetValue(kvp.Key, out Typeface face) &&
                            !Owner.RowTypefaces.TryGetValue(kvp.Key.Item2, out face) &&
                            !Owner.ColumnTypefaces.TryGetValue(kvp.Key.Item1, out face))
                    {
                        face = defaultTypeFace;
                    }

                    if (!Owner.CellMargin.TryGetValue(kvp.Key, out Thickness margin))
                    {
                        margin = Owner.DefaultMargin;
                    }

                    FormattedText fmtText = new FormattedText(kvp.Value, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, face, this.Container.FontSize, Foreground);

                    double textWidth = fmtText.Width;

                    if (this.Container.ShowColorPreview && kvp.Value.StartsWith("#") && (kvp.Value.Length == 7 || kvp.Value.Length == 9))
                    {
                        try
                        {
                            Brush.Parse(kvp.Value.Length == 7 ? kvp.Value : ("#" + kvp.Value.Substring(7, 2) + kvp.Value.Substring(1, 6)));
                            textWidth += this.Owner.FontSize + 3;
                        }
                        catch { }
                    }

                    found = true;
                    maxWidth = Math.Max(maxWidth, textWidth + margin.Left + margin.Right);
                }
            }

            if (found)
            {
                double originalWidth = this.Owner.GetWidth(column, out bool wasDefault);

                if (originalWidth != maxWidth)
                {
                    Owner.ColumnWidths[column] = maxWidth;
                }

                return maxWidth;
            }
            else
            {
                this.Owner.GetWidth(column, out bool wasDefault);

                if (!wasDefault)
                {
                    Owner.ColumnWidths.Remove(column);
                }

                return null;
            }
        }

        internal static string GetLetter(int x)
        {
            if (x < 26)
            {
                return ((char)(x + 65)).ToString();
            }
            else
            {
                StringBuilder tbr = new StringBuilder();

                while (x >= 26)
                {
                    tbr.Insert(0, (char)((x % 26) + 65));
                    x = x / 26;
                }

                tbr.Insert(0, (char)(x + 64));

                return tbr.ToString();
            }
        }

        internal static int FromLetter(string letter)
        {
            int tbr = 0;

            for (int i = 0; i < letter.Length; i++)
            {
                int v = ((int)letter[letter.Length - i - 1] - 65) + 1;

                for (int j = 0; j < i; j++)
                {
                    v *= 26;
                }

                tbr += v;
            }

            return tbr;
        }

        /// <summary>
        /// Resolves which column of the header is under the specified viewport x coordinate,
        /// together with the distance to the closest drawn column edge. Over the frozen strip the
        /// drawn coordinates are the table coordinates themselves (the strip is drawn from its own
        /// unscrolled origin starting at column 0); elsewhere they are the coordinates of the
        /// scrollable slice, translated by its drawn offset.
        /// </summary>
        internal (int column, double closest) ResolveColumnHit(double screenX)
        {
            double closest = double.MaxValue;
            int clickedColumn = -1;

            bool overFrozen = Owner != null && Owner.IsOverFrozenColumns(screenX);

            double xPos = overFrozen ? screenX : screenX - lastDrawnDelta;

            if (lastDrawnXs != null && !overFrozen)
            {
                for (int i = 0; i < lastDrawnXs.Length; i++)
                {
                    if (lastDrawnXs[i] > xPos && xPos >= (i == 0 ? 0 : lastDrawnXs[i - 1]))
                    {
                        clickedColumn = i + lastDrawnLeft;
                    }

                    closest = Math.Min(closest, Math.Abs(lastDrawnXs[i] - xPos));

                    if (closest <= 5 && clickedColumn >= 0)
                    {
                        break;
                    }
                }
            }

            // The frozen columns are drawn with their own (unscrolled) origin, so the hit test above
            // does not describe them: resolve the column index from the frozen strip directly.
            if (overFrozen)
            {
                clickedColumn = Owner.GetFrozenColumnIndex(screenX);

                for (int i = 0; i <= Owner.FrozenColumnCount; i++)
                {
                    closest = Math.Min(closest, Math.Abs(Owner.GetColumnStart(i) - screenX));
                }
            }

            // Positions past the last drawn edge (a fixed MaxTableWidth means the table ends before
            // the viewport does) still belong to the last drawn column: the edge is its right
            // border, and pressing it has to resize the column (and clicking further has to select
            // it) rather than resolving to the non-existent column -1.
            if (clickedColumn < 0 && !overFrozen && lastDrawnXs != null && lastDrawnXs.Length > 0)
            {
                clickedColumn = lastDrawnLeft + lastDrawnXs.Length - 1;
            }

            return (clickedColumn, closest);
        }

        /// <summary>
        /// Resolves what a left-button press at the specified viewport x coordinate does in the
        /// column header: the column it acts on and whether it grabbed a column edge (i.e. starts a
        /// column resize) rather than selecting the whole column.
        /// </summary>
        internal (int column, bool isResize) ResolveColumnPress(double screenX)
        {
            (int clickedColumn, double closest) = ResolveColumnHit(screenX);

            if (closest > 5)
            {
                return (clickedColumn, false);
            }

            bool overFrozen = Owner != null && Owner.IsOverFrozenColumns(screenX);

            double xPos = overFrozen ? screenX : screenX - lastDrawnDelta;

            // In the frozen strip the left edge of the column is its table coordinate: the drawn
            // coordinates of the scrollable slice do not apply there. Inside the scrollable slice
            // the drawn coordinates are relative to the first drawn column, which is not column 0
            // when columns are frozen.
            double columnLeftEdge = overFrozen
                ? Owner.GetColumnStart(clickedColumn)
                : (lastDrawnXs != null && clickedColumn > lastDrawnLeft ? lastDrawnXs[clickedColumn - lastDrawnLeft - 1] : 0);

            if (xPos - columnLeftEdge <= 5)
            {
                // Dragging the left border of a column resizes the previous *visible* column: a
                // hidden one has no border of its own and must not be resized. The search is not
                // bounded by the columns that carry data, so that the border between the last
                // columns of the sheet still resizes the column on its left.
                clickedColumn = Owner.PreviousVisibleColumn(clickedColumn);
            }

            return (clickedColumn, true);
        }

        private double[] lastDrawnXs = null;
        private int lastDrawnLeft = -1;
        private int hoverColumn = -1;
        double lastDrawnDelta = 0;

        /// <summary>The index of the first column of the scrollable slice that was drawn last.</summary>
        internal int LastDrawnLeft => lastDrawnLeft;

        /// <summary>The number of columns of the scrollable slice that was drawn last.</summary>
        internal int LastDrawnWidth => lastDrawnXs == null ? -1 : lastDrawnXs.Length - 1;

        /// <summary>The horizontal translation applied to the scrollable slice that was drawn last.</summary>
        internal double LastDrawnDelta => lastDrawnDelta;

        /// <summary>The right edges of the columns of the scrollable slice that was drawn last, in drawing coordinates.</summary>
        internal double[] LastDrawnXs => lastDrawnXs;

        /// <summary>The width of the frozen column strip, clamped to the current size of the header.</summary>
        internal double ClampedFrozenWidth => Owner == null ? 0 : Owner.GetClampedFrozenWidth(this.Bounds.Width);

        public override void Render(DrawingContext context)
        {
            if (this.FontFamily == null)
            {
                return;
            }

            context.FillRectangle(new SolidColorBrush(this.HeaderBackground), new Rect(0, 0, this.Bounds.Width, this.Bounds.Height));

            double frozenWidth = Owner == null ? 0 : Owner.GetClampedFrozenWidth(this.Bounds.Width);

            (int top, double offsetY, int height, double actualHeight, double startHeight) = Owner.GetRangeY(Owner.Offset.Y, this.Bounds.Height);

            if (frozenWidth <= 0)
            {
                (int left, double offsetX, int width, double actualWidth, double startWidth) = Owner.GetRangeX(Owner.Offset.X, this.Bounds.Width);
                RenderColumns(context, left, width, offsetX, top, height, 0, this.Bounds.Width, true);
            }
            else
            {
                // Scrollable columns: they start after the frozen strip, exactly like the content area.
                (int left, double leftDelta, int width, double actualWidth, double startWidth) = Owner.GetRangeX(Owner.Offset.X + frozenWidth, this.Bounds.Width - frozenWidth);
                RenderColumns(context, left, width, leftDelta + frozenWidth, top, height, frozenWidth, this.Bounds.Width - frozenWidth, true);

                // Frozen columns: drawn without any scroll offset, so that they stay in place.
                (int frozenLeft, double frozenLeftDelta, int frozenColumnSpan, double frozenActualWidth, double frozenStartWidth) = Owner.GetRangeX(0, frozenWidth);
                RenderColumns(context, frozenLeft, frozenColumnSpan, frozenLeftDelta, top, height, 0, frozenWidth, false);

                context.DrawLine(this.HeaderSeparator, new Point(frozenWidth + 0.5, 0).SnapToDevicePixels(this), new Point(frozenWidth + 0.5, this.Bounds.Height).SnapToDevicePixels(this));
            }
        }

        /// <summary>
        /// Draws one horizontal slice of the header: either the scrollable columns or the frozen
        /// column strip, which is drawn without the scroll offset so that it stays in place.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <param name="left">The index of the first drawn column.</param>
        /// <param name="width">The number of drawn columns.</param>
        /// <param name="offsetX">The horizontal translation applied to the slice.</param>
        /// <param name="top">The index of the first visible row (used to decide the selection style).</param>
        /// <param name="height">The number of visible rows.</param>
        /// <param name="regionX">The left edge of the slice, in screen coordinates.</param>
        /// <param name="regionWidth">The width of the slice.</param>
        /// <param name="isMainRegion"><see langword="true"/> for the scrollable slice, which owns the hit testing data.</param>
        private void RenderColumns(DrawingContext context, int left, int width, double offsetX, int top, int height, double regionX, double regionWidth, bool isMainRegion)
        {
            Pen gridPen = new Pen(new LinearGradientBrush() { StartPoint = new RelativePoint(0, 0, RelativeUnit.Absolute), EndPoint = new RelativePoint(0, this.Bounds.Height, RelativeUnit.Absolute), GradientStops = new GradientStops() { new GradientStop(this.HeaderBackground, 0), new GradientStop(this.GridColor, 1) } });
            IBrush brs = this.Foreground;
            Brush selectionHighlightBrush = new SolidColorBrush(Color.FromArgb(16, 0, 0, 0));
            Brush fullSelectionHighlightBrush = new SolidColorBrush(SelectionAccent.Color, 0.2);
            Brush hoverHighlightBrush = new SolidColorBrush(SelectionAccent.Color, 0.35);
            Pen selectionPen = new Pen(SelectionAccent, 2);

            // A worksheet can ask for a focus rectangle instead of a filled selection.
            bool fillSelection = this.Owner == null || this.Owner.Sheet == null || this.Owner.Sheet.SelectionStyle != SheetSelectionStyle.FocusRect;

            Typeface typeFace = new Typeface(this.FontFamily, this.FontStyle, this.FontWeight);

            using (context.PushTransform(Matrix.CreateTranslation(offsetX, 0)))
            using (context.PushClip(new Rect(regionX - offsetX, 0, regionWidth, this.Bounds.Height)))
            {
                double[] xs = new double[width + 1];
                int[] selected = new int[width + 1];
                bool?[] shouldBeAccented = new bool?[width + 1];

                for (int i = 0; i < Selection.Count; i++)
                {
                    for (int j = Math.Max(Selection[i].Left, left); j <= Math.Min(Selection[i].Right, left + width); j++)
                    {
                        if (Selection[i].Top == 0 && Selection[i].Bottom == Owner.MaxTableHeight)
                        {
                            selected[j - left] = Math.Max(selected[j - left], 2);
                        }
                        else
                        {
                            selected[j - left] = Math.Max(selected[j - left], 1);
                        }


                        if (Selection.Count == 1 && Selection[i].Top < top && Selection[i].Bottom >= top)
                        {
                            shouldBeAccented[j - left] = false;
                        }
                        else if (shouldBeAccented[j - left] == null)
                        {
                            shouldBeAccented[j - left] = true;
                        }
                    }
                }

                double currX = 0;
                for (int x = 0; x <= width; x++)
                {
                    // A hidden column has a width of zero, so its left edge coincides with the
                    // left edge of the following column: the separator is drawn by that column
                    // instead, avoiding a duplicated (double-drawn) line.
                    if (!Owner.IsColumnHidden(left + x))
                    {
                        context.DrawLine(gridPen, new Point(currX, 0).SnapToDevicePixels(this), new Point(currX, this.Bounds.Height).SnapToDevicePixels(this));
                    }

                    currX += Owner.GetWidth(left + x);
                    xs[x] = currX;
                }

                // Only the scrollable slice publishes its coordinates: the frozen strip is resolved
                // by the pointer handlers, which know about the frozen origin.
                if (isMainRegion)
                {
                    lastDrawnXs = xs;
                    lastDrawnLeft = left;
                    lastDrawnDelta = offsetX;
                }

                for (int x = 0; x <= width; x++)
                {
                    // Hidden columns occupy no horizontal space; nothing is highlighted for them.
                    if (Owner.IsColumnHidden(left + x))
                    {
                        continue;
                    }

                    if (hoverColumn == x + left)
                    {
                        context.FillRectangle(hoverHighlightBrush, new Rect(new Point(x == 0 ? 0 : xs[x - 1], 0).SnapToDevicePixels(this, true, true), new Point(xs[x], this.Bounds.Height).SnapToDevicePixels(this, true, true)));
                    }
                    else
                    {
                        if (selected[x] == 1)
                        {
                            context.FillRectangle(selectionHighlightBrush, new Rect(new Point(x == 0 ? 0 : xs[x - 1], 0).SnapToDevicePixels(this, true, true), new Point(xs[x], this.Bounds.Height).SnapToDevicePixels(this, true, true)));
                        }
                        else if (selected[x] == 2 && fillSelection)
                        {
                            // A worksheet that asks for a focus rectangle does not paint the whole
                            // header of a selected column either.
                            context.FillRectangle(fullSelectionHighlightBrush, new Rect(new Point(x == 0 ? 0 : xs[x - 1], 0).SnapToDevicePixels(this, true, true), new Point(xs[x], this.Bounds.Height).SnapToDevicePixels(this, true, true)));
                        }
                    }
                }


                for (int x = 0; x <= width; x++)
                {
                    // Hidden columns do not get a header letter, so the letters of the remaining
                    // columns stay contiguous (A, B, D … rather than A, B, "", D).
                    if (Owner.IsColumnHidden(left + x))
                    {
                        continue;
                    }

                    string txt = GetLetter(x + left);

                    double realX = x == 0 ? 0 : xs[x - 1];

                    

                    using (context.PushClip(new Rect(realX + 3, 0, xs[x] - realX - 6, this.Bounds.Height)))
                    { 
                        FormattedText fmtText = new FormattedText(txt, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeFace, this.FontSize, selected[x] == 0 ? brs : SelectionAccent);
                        
                        double realY = this.Bounds.Height - 4 - fmtText.Height;

                        context.DrawText(fmtText, new Point(realX + (xs[x] - realX) * 0.5 - fmtText.Width * 0.5, realY));
                    }
                }

                context.DrawLine(this.HeaderSeparator, new Point(-offsetX, this.Bounds.Height - 1).SnapToDevicePixels(this), new Point(this.Bounds.Width - offsetX, this.Bounds.Height - 1).SnapToDevicePixels(this));

                for (int x = 0; x <= width; x++)
                {
                    if (Owner.IsColumnHidden(left + x))
                    {
                        continue;
                    }

                    if (selected[x] > 0 && shouldBeAccented[x] == true)
                    {
                        context.DrawLine(selectionPen, new Point((x == 0 ? 0 : xs[x - 1]), this.Bounds.Height - 1).SnapToDevicePixels(this, true, true), new Point(xs[x] + 1, this.Bounds.Height - 1).SnapToDevicePixels(this, true, true));
                    }
                }
            }


        }
    }
}
