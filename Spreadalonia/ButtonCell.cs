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
using System.Globalization;

namespace Spreadalonia
{
    /// <summary>
    /// A custom cell body that renders a clickable button (or link-styled button). It is meant for
    /// cells whose value is not typed by the user but chosen from an external dialog, e.g. the
    /// auxiliary-accounting items (department, employee, customer, supplier, project) of a
    /// bookkeeping opening-balance grid.
    /// </summary>
    /// <remarks>
    /// Clicking (or double clicking) a button cell raises <see cref="Spreadsheet.CellTapped"/>
    /// / <see cref="Spreadsheet.CellDoubleTapped"/> instead of starting the normal text editing,
    /// provided that <see cref="SuppressEdit"/> is <see langword="true"/> (the default). The host
    /// application opens its own selection dialog and writes the chosen value back through
    /// <c>Data</c>, which repaints the cell.
    /// </remarks>
    public class ButtonCell
    {
        /// <summary>
        /// The text displayed on the button. When it is empty the raw cell content is displayed.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// An arbitrary object attached to the cell by the host application. It is carried verbatim
        /// by the <see cref="Spreadsheet.CellTapped"/> event and can be used to identify which
        /// auxiliary item (department, employee, customer …) the cell refers to.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// A host-defined string identifying the kind of cell. It is carried verbatim by the
        /// <see cref="Spreadsheet.CellTapped"/> event so that a single handler can serve several
        /// kinds of picker.
        /// </summary>
        public string CellType { get; set; }

        /// <summary>
        /// The brush used to fill the button. When <see langword="null"/> the cell is left
        /// transparent and only the text (and optionally the border) is drawn.
        /// </summary>
        public IBrush Background { get; set; }

        /// <summary>
        /// The brush used to fill the button while it is pressed.
        /// </summary>
        public IBrush PressedBackground { get; set; }

        /// <summary>
        /// The brush used to draw the button text. When <see langword="null"/> the default
        /// foreground of the table is used.
        /// </summary>
        public IBrush Foreground { get; set; }

        /// <summary>
        /// The brush used to draw the button border. When <see langword="null"/> a grey border is
        /// used.
        /// </summary>
        public IBrush BorderBrush { get; set; }

        /// <summary>
        /// Whether a border is drawn around the button.
        /// </summary>
        public bool ShowBorder { get; set; } = true;

        /// <summary>
        /// Whether the text is underlined, which makes the cell look like a hyperlink rather than
        /// like a button.
        /// </summary>
        public bool Underline { get; set; }

        /// <summary>
        /// Whether the button occupies the whole cell. When <see langword="false"/> (the default)
        /// a small margin is left around it so that it looks like a button rather than like a
        /// filled cell.
        /// </summary>
        public bool StretchToCell { get; set; }

        /// <summary>
        /// Whether the default text editing is suppressed for this cell. When <see langword="true"/>
        /// (the default) double clicking, typing or pressing F2 raises the cell events instead of
        /// opening the editing box.
        /// </summary>
        public bool SuppressEdit { get; set; } = true;

        internal bool IsPressed { get; set; }

        internal Rect HitRect { get; set; }

        /// <summary>
        /// Creates a new empty <see cref="ButtonCell"/>.
        /// </summary>
        public ButtonCell()
        {
        }

        /// <summary>
        /// Creates a new <see cref="ButtonCell"/> displaying the specified text.
        /// </summary>
        /// <param name="text">The text displayed on the button.</param>
        public ButtonCell(string text)
        {
            Text = text;
        }

        /// <summary>
        /// Creates a new <see cref="ButtonCell"/> with the specified text, cell type and tag.
        /// </summary>
        /// <param name="text">The text displayed on the button.</param>
        /// <param name="cellType">A host-defined string identifying the kind of cell.</param>
        /// <param name="tag">An arbitrary object carried by the click event.</param>
        public ButtonCell(string text, string cellType, object tag)
        {
            Text = text;
            CellType = cellType;
            Tag = tag;
        }

        /// <summary>
        /// Computes the text to draw. The base implementation returns <see cref="Text"/>, falling
        /// back to the raw cell content when no text has been set.
        /// </summary>
        /// <param name="rawData">The raw string stored in the cell, or <see langword="null"/>.</param>
        internal virtual string GetDisplayText(string rawData)
        {
            return Text ?? rawData ?? "";
        }

        /// <summary>
        /// Renders the button into the specified drawing context and updates the internal hit-test
        /// rectangle used to detect hover and clicks.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <param name="bounds">The bounds of the cell (or of the merged range it belongs to).</param>
        /// <param name="defaultForeground">The foreground brush used when <see cref="Foreground"/> is <see langword="null"/>.</param>
        /// <param name="typeface">The typeface used to draw the text.</param>
        /// <param name="fontSize">The font size used to draw the text.</param>
        /// <param name="horAlign">The horizontal alignment of the text within the cell.</param>
        /// <param name="verAlign">The vertical alignment of the text within the cell.</param>
        /// <param name="rawData">The raw string stored in the cell (used when <see cref="Text"/> is not set).</param>
        public void Render(DrawingContext context, Rect bounds, IBrush defaultForeground, Typeface typeface, double fontSize, TextAlignment horAlign, VerticalAlignment verAlign, string rawData)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                HitRect = default;
                return;
            }

            Rect buttonBounds = bounds;

            if (!StretchToCell)
            {
                buttonBounds = bounds.Deflate(new Thickness(2, 1));
            }

            if (buttonBounds.Width <= 0 || buttonBounds.Height <= 0)
            {
                HitRect = default;
                return;
            }

            IBrush background = IsPressed ? (PressedBackground ?? Background) : Background;

            if (background != null)
            {
                context.FillRectangle(background, buttonBounds);
            }

            if (ShowBorder)
            {
                IBrush border = BorderBrush ?? new SolidColorBrush(Color.Parse("#FF767676"));
                context.DrawRectangle(new Pen(border, 1), buttonBounds);
            }

            string text = GetDisplayText(rawData);

            if (string.IsNullOrEmpty(text))
            {
                // Still clickable: the hit area is the whole (padded) button.
                HitRect = buttonBounds;
                return;
            }

            IBrush brush = Foreground ?? defaultForeground ?? Brushes.Black;

            FormattedText fmtText = null;
            double textWidth = 0;
            double textHeight = 0;

            try
            {
                Typeface face = typeface;

                if (face.FontFamily == null)
                {
                    face = new Typeface(FontFamily.Default, face.Style, face.Weight);
                }

                fmtText = new FormattedText(
                    text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    face,
                    fontSize,
                    brush);

                textWidth = fmtText.Width;
                textHeight = fmtText.Height;
            }
            catch
            {
                HitRect = buttonBounds;
                return;
            }

            double textX = buttonBounds.X + 3;
            double textY;

            if (verAlign == VerticalAlignment.Top)
            {
                textY = buttonBounds.Y + 1;
            }
            else if (verAlign == VerticalAlignment.Bottom)
            {
                textY = buttonBounds.Y + buttonBounds.Height - 1 - textHeight;
            }
            else
            {
                textY = buttonBounds.Y + buttonBounds.Height * 0.5 - textHeight * 0.5;
            }

            if (horAlign == TextAlignment.Center)
            {
                textX = buttonBounds.X + buttonBounds.Width * 0.5 - textWidth * 0.5;
            }
            else if (horAlign == TextAlignment.Right)
            {
                textX = buttonBounds.X + buttonBounds.Width - 3 - textWidth;
            }

            // Make sure the text never escapes the button rectangle.
            double left = buttonBounds.X + 3;
            double right = buttonBounds.X + buttonBounds.Width - 3;
            textX = Math.Max(left, Math.Min(textX, right - textWidth));

            using (context.PushClip(buttonBounds))
            {
                context.DrawText(fmtText, new Point(textX, textY));

                if (Underline)
                {
                    double strikeY = textY + textHeight + 0.5;
                    context.DrawLine(new Pen(brush, 1), new Point(textX, strikeY), new Point(textX + textWidth, strikeY));
                }
            }

            HitRect = buttonBounds;
        }
    }
}
