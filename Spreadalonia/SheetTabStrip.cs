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
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;

namespace Spreadalonia
{
    /// <summary>
    /// The strip of worksheet tabs that is drawn at the bottom of a <see cref="Spreadsheet"/>, below
    /// the horizontal scroll bar. Clicking a tab switches the displayed worksheet, the wheel (or the
    /// two arrow buttons) scrolls the tabs horizontally and a tab can be renamed, deleted or moved
    /// from its context menu when the corresponding <see cref="Spreadsheet"/> option is enabled.
    /// </summary>
    /// <remarks>
    /// The strip is built out of plain Avalonia controls (a <see cref="ScrollViewer"/> holding a
    /// horizontal <see cref="StackPanel"/> of <see cref="Button"/>s), so it follows the theme and the
    /// dark / light mode of the host application.
    /// </remarks>
    public class SheetTabStrip : UserControl
    {
        /// <summary>
        /// Defines the <see cref="Container"/> property.
        /// </summary>
        public static readonly StyledProperty<Spreadsheet> ContainerProperty = AvaloniaProperty.Register<SheetTabStrip, Spreadsheet>(nameof(Container));

        /// <summary>
        /// The <see cref="Spreadsheet"/> whose worksheets are shown by this strip.
        /// </summary>
        public Spreadsheet Container
        {
            get { return GetValue(ContainerProperty); }
            set { SetValue(ContainerProperty, value); }
        }

        /// <summary>
        /// The tabs that are currently shown, in the same order as the worksheets they stand for.
        /// </summary>
        internal IReadOnlyList<Button> TabButtons => this.tabButtons;

        /// <summary>
        /// The worksheets behind <see cref="TabButtons"/>.
        /// </summary>
        internal IReadOnlyList<SpreadsheetSheet> TabSheets => this.tabSheets;

        private readonly Border outerBorder = new Border();
        private readonly Grid layout = new Grid();
        private readonly StackPanel tabsPanel = new StackPanel() { Orientation = Orientation.Horizontal };
        private readonly ScrollViewer scroller;
        private readonly Button scrollLeftButton;
        private readonly Button scrollRightButton;
        private readonly Button newSheetButton;

        private readonly List<Button> tabButtons = new List<Button>();
        private readonly List<SpreadsheetSheet> tabSheets = new List<SpreadsheetSheet>();

        private TextBox renameBox;
        private SpreadsheetSheet renameSheet;

        /// <summary>
        /// Create a new <see cref="SheetTabStrip"/>.
        /// </summary>
        public SheetTabStrip()
        {
            this.scroller = new ScrollViewer()
            {
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch,
                Content = this.tabsPanel
            };

            this.scroller.PointerWheelChanged += this.Scroller_PointerWheelChanged;

            this.scrollLeftButton = CreateToolButton("<");
            this.scrollRightButton = CreateToolButton(">");
            this.newSheetButton = CreateToolButton("+");

            this.scrollLeftButton.Click += (s, e) => this.ScrollBy(-80);
            this.scrollRightButton.Click += (s, e) => this.ScrollBy(80);
            this.newSheetButton.Click += (s, e) => this.Container?.AddSheet(null);

            this.layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            this.layout.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
            this.layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            this.layout.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            this.layout.Children.Add(this.scrollLeftButton);
            this.layout.Children.Add(this.scroller);
            this.layout.Children.Add(this.scrollRightButton);
            this.layout.Children.Add(this.newSheetButton);

            Grid.SetColumn(this.scroller, 1);
            Grid.SetColumn(this.scrollRightButton, 2);
            Grid.SetColumn(this.newSheetButton, 3);

            this.outerBorder.BorderThickness = new Thickness(0, 1, 0, 0);
            this.outerBorder.Child = this.layout;

            this.Content = this.outerBorder;

            this.IsVisible = false;
            this.Height = 0;
            this.MinHeight = 0;
            this.Focusable = false;
        }

        /// <summary>
        /// Creates one of the small buttons at the two ends of the strip (the scroll arrows and the
        /// "new worksheet" button).
        /// </summary>
        /// <param name="text">The glyph shown by the button.</param>
        private static Button CreateToolButton(string text)
        {
            Button button = new Button()
            {
                Content = text,
                Width = 24,
                MinWidth = 0,
                MinHeight = 0,
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                BorderThickness = new Thickness(0),
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch,
                VerticalContentAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
                HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
                Focusable = false,
                IsTabStop = false
            };

            return button;
        }

        /// <summary>
        /// Rebuilds the tabs from the worksheets of <see cref="Container"/>. Call this whenever a
        /// worksheet is added, removed, renamed or moved, or when one of the options of the strip
        /// changes.
        /// </summary>
        internal void Refresh()
        {
            Spreadsheet container = this.Container;

            if (container == null)
            {
                this.tabButtons.Clear();
                this.tabSheets.Clear();
                this.tabsPanel.Children.Clear();
                this.IsVisible = false;
                this.Height = 0;
                return;
            }

            this.tabSheets.Clear();

            foreach (SpreadsheetSheet sheet in container.Sheets)
            {
                // A hidden worksheet has no tab of its own, unless the host asked for the hidden
                // ones to be shown as well; the worksheet that is being displayed always has one,
                // otherwise there would be no way back from it.
                if (!sheet.IsHidden || container.SheetTabHiddenSheetsVisible || sheet == container.CurrentSheet)
                {
                    this.tabSheets.Add(sheet);
                }
            }

            bool visible = container.SheetTabVisible && (this.tabSheets.Count > 1 || container.SheetTabVisibleWhenSingle);

            this.IsVisible = visible;
            this.Height = visible ? container.SheetTabHeight : 0;

            if (!visible)
            {
                this.tabButtons.Clear();
                this.tabsPanel.Children.Clear();
                return;
            }

            // Keep the strip close to the classic Excel tab bar: a neutral grey rail with
            // individually separated tabs. Hosts can still override all of these brushes through
            // the public SheetTab* properties.
            this.outerBorder.Background = container.SheetTabBackground ?? new SolidColorBrush(Color.FromRgb(242, 242, 242));
            this.outerBorder.BorderBrush = container.SheetTabBorderBrush ?? new SolidColorBrush(container.GridColor);

            this.newSheetButton.IsVisible = container.SheetTabNewButtonVisible && container.SheetTabAllowInsert;

            this.tabButtons.Clear();
            this.tabsPanel.Children.Clear();

            foreach (SpreadsheetSheet sheet in this.tabSheets)
            {
                Button button = CreateTabButton(sheet);
                this.tabButtons.Add(button);
                this.tabsPanel.Children.Add(button);
            }

            this.UpdateSelection();
        }

        /// <summary>
        /// Marks the tab of the worksheet that is currently displayed and makes sure that it is
        /// visible in the strip.
        /// </summary>
        internal void UpdateSelection()
        {
            Spreadsheet container = this.Container;

            if (container == null)
            {
                return;
            }

            IBrush selectedBackground = container.SheetTabSelectedBackground ?? new SolidColorBrush(Colors.White);

            for (int i = 0; i < this.tabButtons.Count; i++)
            {
                Button button = this.tabButtons[i];
                SpreadsheetSheet sheet = this.tabSheets[i];

                bool selected = sheet == container.CurrentSheet;

                if (selected)
                {
                    button.Background = selectedBackground ?? new SolidColorBrush(Colors.White);
                    button.BorderBrush = new SolidColorBrush(Color.FromRgb(33, 115, 70));
                    button.BorderThickness = new Thickness(1, 1, 1, 2);
                    button.FontWeight = FontWeight.Bold;
                }
                else
                {
                    // The background is cleared instead of being set to transparent, so that the
                    // theme keeps painting the hover state of the button.
                    button.ClearValue(Button.BackgroundProperty);
                    button.ClearValue(Button.FontWeightProperty);
                    button.BorderBrush = container.SheetTabBorderBrush ?? new SolidColorBrush(container.GridColor);
                    button.BorderThickness = new Thickness(0, 1, 1, 0);
                }

                // A hidden worksheet that is shown anyway (because it is the current one, or because
                // the host wants to see it) is dimmed.
                button.Opacity = sheet.IsHidden ? 0.6 : 1;
            }

            Dispatcher.UIThread.Post(() =>
            {
                this.ScrollCurrentIntoView();
                this.UpdateScrollButtons();
            });
        }

        /// <summary>
        /// Creates the tab of a worksheet.
        /// </summary>
        /// <param name="sheet">The worksheet the tab stands for.</param>
        private Button CreateTabButton(SpreadsheetSheet sheet)
        {
            Spreadsheet container = this.Container;

            TextBlock text = new TextBlock()
            {
                Text = sheet.Name,
                MaxWidth = 160,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.NoWrap,
                Margin = new Thickness(8, 0),
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center
            };

            if (container.SheetTabForeground != null)
            {
                text.Foreground = container.SheetTabForeground;
            }

            Button button = new Button()
            {
                Content = text,
                MinWidth = 0,
                MinHeight = 0,
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                BorderThickness = new Thickness(0, 1, 1, 0),
                BorderBrush = container.SheetTabBorderBrush ?? new SolidColorBrush(container.GridColor),
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Stretch,
                HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch,
                VerticalContentAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
                Focusable = false,
                IsTabStop = false,
                Tag = sheet
            };

            ToolTip.SetTip(button, sheet.Name);

            button.Click += (s, e) =>
            {
                if (this.Container != null)
                {
                    this.Container.CurrentSheetIndex = sheet.Index;
                }
            };

            button.DoubleTapped += (s, e) =>
            {
                this.BeginRename(sheet);
                e.Handled = true;
            };

            button.ContextMenu = this.CreateTabContextMenu(sheet);

            return button;
        }

        /// <summary>
        /// Creates the context menu of a tab: rename, delete and move, each of them enabled only
        /// when the corresponding option of the <see cref="Spreadsheet"/> allows it.
        /// </summary>
        /// <param name="sheet">The worksheet the tab stands for.</param>
        private ContextMenu CreateTabContextMenu(SpreadsheetSheet sheet)
        {
            Spreadsheet container = this.Container;

            ContextMenu menu = new ContextMenu();

            MenuItem renameItem = new MenuItem() { Header = "重命名", IsEnabled = container.SheetTabAllowRename };
            renameItem.Click += (s, e) => this.BeginRename(sheet);
            menu.Items.Add(renameItem);

            MenuItem deleteItem = new MenuItem() { Header = "删除", IsEnabled = container.SheetTabAllowDelete && container.Sheets.Count > 1 };
            deleteItem.Click += (s, e) => container.RemoveSheet(sheet);
            menu.Items.Add(deleteItem);

            menu.Items.Add(new Separator());

            MenuItem moveLeftItem = new MenuItem() { Header = "向左移动", IsEnabled = container.SheetTabAllowReorder && sheet.Index > 0 };
            moveLeftItem.Click += (s, e) => container.MoveSheet(sheet, sheet.Index - 1);
            menu.Items.Add(moveLeftItem);

            MenuItem moveRightItem = new MenuItem() { Header = "向右移动", IsEnabled = container.SheetTabAllowReorder && sheet.Index < container.Sheets.Count - 1 };
            moveRightItem.Click += (s, e) => container.MoveSheet(sheet, sheet.Index + 1);
            menu.Items.Add(moveRightItem);

            return menu;
        }

        /// <summary>
        /// Replaces the tab of a worksheet with a text box, so that the user can type a new name
        /// for it in place.
        /// </summary>
        /// <param name="sheet">The worksheet to rename.</param>
        private void BeginRename(SpreadsheetSheet sheet)
        {
            if (this.Container == null || !this.Container.SheetTabAllowRename || this.renameBox != null)
            {
                return;
            }

            int index = this.tabSheets.IndexOf(sheet);

            if (index < 0 || index >= this.tabButtons.Count)
            {
                return;
            }

            this.renameSheet = sheet;

            TextBox box = new TextBox()
            {
                Text = sheet.Name,
                Width = Math.Max(60, this.tabButtons[index].Bounds.Width),
                MinWidth = 0,
                MinHeight = 0,
                Padding = new Thickness(4, 0),
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center,
                CornerRadius = new CornerRadius(0)
            };

            this.renameBox = box;
            this.tabsPanel.Children[index] = box;

            box.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    this.EndRename(true);
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    this.EndRename(false);
                    e.Handled = true;
                }
            };

            box.LostFocus += (s, e) => this.EndRename(true);

            box.Focus();
            box.SelectAll();
        }

        /// <summary>
        /// Closes the in-place rename text box and, when the rename is confirmed, gives the new name
        /// to the worksheet.
        /// </summary>
        /// <param name="commit">Whether the typed name should be applied.</param>
        private void EndRename(bool commit)
        {
            if (this.renameBox == null)
            {
                return;
            }

            SpreadsheetSheet sheet = this.renameSheet;
            string newName = this.renameBox.Text;

            this.renameBox = null;
            this.renameSheet = null;

            if (commit && this.Container != null && sheet != null && !string.IsNullOrWhiteSpace(newName) && newName != sheet.Name)
            {
                this.Container.RenameSheet(sheet, newName.Trim());
            }
            else
            {
                this.Refresh();
            }
        }

        /// <summary>
        /// Scrolls the tabs horizontally by the specified amount, in device independent pixels.
        /// </summary>
        /// <param name="delta">The amount to scroll by; positive values move to the right.</param>
        private void ScrollBy(double delta)
        {
            double extent = this.scroller.Extent.Width;
            double viewport = this.scroller.Viewport.Width;
            double maxOffset = Math.Max(0, extent - viewport);
            double offset = Math.Max(0, Math.Min(maxOffset, this.scroller.Offset.X + delta));

            this.scroller.Offset = new Avalonia.Vector(offset, 0);

            this.UpdateScrollButtons();
        }

        /// <summary>
        /// Scrolls the tab of the worksheet that is currently displayed into view.
        /// </summary>
        private void ScrollCurrentIntoView()
        {
            Spreadsheet container = this.Container;

            if (container == null)
            {
                return;
            }

            int index = this.tabSheets.IndexOf(container.CurrentSheet);

            if (index < 0 || index >= this.tabButtons.Count)
            {
                return;
            }

            Rect bounds = this.tabButtons[index].Bounds;

            if (bounds.Width <= 0)
            {
                // The strip has not been laid out yet.
                return;
            }

            double viewport = this.scroller.Viewport.Width > 0 ? this.scroller.Viewport.Width : this.scroller.Bounds.Width;
            double left = this.scroller.Offset.X;
            double right = left + viewport;

            if (bounds.Left < left)
            {
                this.ScrollBy(bounds.Left - left);
            }
            else if (bounds.Right > right)
            {
                this.ScrollBy(bounds.Right - right);
            }
        }

        /// <summary>
        /// Enables the scroll arrows only while there is something to scroll.
        /// </summary>
        private void UpdateScrollButtons()
        {
            double maxOffset = Math.Max(0, this.scroller.Extent.Width - this.scroller.Viewport.Width);

            this.scrollLeftButton.IsEnabled = this.scroller.Offset.X > 0;
            this.scrollRightButton.IsEnabled = this.scroller.Offset.X < maxOffset - 0.5;
        }

        /// <summary>
        /// Scrolls the tabs with the mouse wheel: a vertical wheel movement scrolls the strip
        /// horizontally, like every other spreadsheet application does.
        /// </summary>
        private void Scroller_PointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            double delta = e.Delta.X != 0 ? e.Delta.X : e.Delta.Y;

            if (delta == 0)
            {
                return;
            }

            this.ScrollBy(-delta * 50);
            e.Handled = true;
        }
    }
}
