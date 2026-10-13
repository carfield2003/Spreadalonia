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
    /// The rendering mode of a <see cref="ButtonCell"/>.
    /// </summary>
    public enum ButtonCellMode
    {
        /// <summary>
        /// The whole cell is rendered as a clickable button.
        /// </summary>
        Button,

        /// <summary>
        /// The cell keeps its normal text and a small icon is drawn at the right edge of the
        /// cell. Clicking the icon raises the cell events, while the rest of the cell behaves
        /// like a normal cell. This is the reference-picker ("RefCell") style used by
        /// SSFinance: a magnifier icon invites the user to look up a value (customer,
        /// department, employee …) from an external dialog.
        /// </summary>
        Icon
    }

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
        /// The rendering mode of the cell: a full-cell <see cref="ButtonCellMode.Button"/> or a
        /// small icon at the right edge of an otherwise normal cell
        /// (<see cref="ButtonCellMode.Icon"/>).
        /// </summary>
        public ButtonCellMode Mode { get; set; } = ButtonCellMode.Button;

        /// <summary>
        /// The size of the icon in icon mode, in device independent pixels. The default of 20
        /// matches the button size of the SSFinance RefCell.
        /// </summary>
        public double IconSize { get; set; } = 20;

        /// <summary>
        /// The SVG path data of the icon drawn in icon mode. When it is empty a magnifier glyph
        /// (the one used by the SSFinance RefCell) is drawn.
        /// </summary>
        public string IconPath { get; set; }

        /// <summary>
        /// The brush used to draw the icon glyph. The default is a light grey; hovering the icon
        /// switches to <see cref="IconHoverFill"/>.
        /// </summary>
        public IBrush IconFill { get; set; }

        /// <summary>
        /// The brush used to draw the icon glyph while the pointer is over the icon.
        /// </summary>
        public IBrush IconHoverFill { get; set; }

        /// <summary>
        /// The brush used to fill the icon rectangle while the pointer is over the icon. The
        /// default is a light grey, matching the SSFinance RefCell.
        /// </summary>
        public IBrush IconHoverBackground { get; set; }

        /// <summary>
        /// Whether the default text editing is suppressed for this cell. When <see langword="true"/>
        /// (the default) double clicking, typing or pressing F2 raises the cell events instead of
        /// opening the editing box.
        /// </summary>
        public bool SuppressEdit { get; set; } = true;

        internal bool IsPressed { get; set; }

        /// <summary>
        /// Whether the pointer currently hovers over the hit-test rectangle. In icon mode this
        /// highlights the icon.
        /// </summary>
        internal bool IsHovered { get; set; }

        internal Rect HitRect { get; set; }

        /// <summary>
        /// The magnifier glyph used by the SSFinance RefCell, reused as the default icon.
        /// </summary>
        private static readonly StreamGeometry MagnifierGeometry = StreamGeometry.Parse("M42.5,22A12.5,12.5 0 0,1 55,34.5A12.5,12.5 0 0,1 42.5,47C40.14,47 37.92,46.34 36,45.24L26.97,54.27C25.8,55.44 23.9,55.44 22.73,54.27C21.56,53.1 21.56,51.2 22.73,50.03L31.8,40.96C30.66,39.08 30,36.86 30,34.5A12.5,12.5 0 0,1 42.5,22M42.5,26A8.5,8.5 0 0,0 34,34.5A8.5,8.5 0 0,0 42.5,43A8.5,8.5 0 0,0 51,34.5A8.5,8.5 0 0,0 42.5,26Z");

        /// <summary>
        /// Cache of the custom icon geometry, so that the path is only parsed once.
        /// </summary>
        private string cachedIconPath;

        private StreamGeometry cachedIconGeometry;

        /// <summary>
        /// Returns the parsed icon geometry: <see cref="IconPath"/> when set, the default
        /// magnifier otherwise.
        /// </summary>
        private StreamGeometry GetIconGeometry()
        {
            if (string.IsNullOrEmpty(IconPath))
            {
                return MagnifierGeometry;
            }

            if (!string.Equals(cachedIconPath, IconPath, StringComparison.Ordinal))
            {
                cachedIconPath = IconPath;
                cachedIconGeometry = StreamGeometry.Parse(IconPath);
            }

            return cachedIconGeometry;
        }

        /// <summary>
        /// Measures the given text with the specified typeface and font size.
        /// </summary>
        private static FormattedText CreateFormattedText(string text, IBrush brush, Typeface typeface, double fontSize)
        {
            Typeface face = typeface;

            if (face.FontFamily == null)
            {
                face = new Typeface(FontFamily.Default, face.Style, face.Weight);
            }

            return new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                face,
                fontSize,
                brush);
        }

        /// <summary>
        /// Draws an already measured text inside the given rectangle, honouring the alignments
        /// and the <see cref="Underline"/> flag, clipped to the rectangle.
        /// </summary>
        private void DrawAlignedText(DrawingContext context, FormattedText fmtText, Rect rect, TextAlignment horAlign, VerticalAlignment verAlign, IBrush brush)
        {
            double textWidth = fmtText.Width;
            double textHeight = fmtText.Height;

            double textX = rect.X + 3;
            double textY;

            if (verAlign == VerticalAlignment.Top)
            {
                textY = rect.Y + 1;
            }
            else if (verAlign == VerticalAlignment.Bottom)
            {
                textY = rect.Y + rect.Height - 1 - textHeight;
            }
            else
            {
                textY = rect.Y + rect.Height * 0.5 - textHeight * 0.5;
            }

            if (horAlign == TextAlignment.Center)
            {
                textX = rect.X + rect.Width * 0.5 - textWidth * 0.5;
            }
            else if (horAlign == TextAlignment.Right)
            {
                textX = rect.X + rect.Width - 3 - textWidth;
            }

            // Make sure the text never escapes the rectangle.
            double left = rect.X + 3;
            double right = rect.X + rect.Width - 3;
            textX = Math.Max(left, Math.Min(textX, right - textWidth));

            using (context.PushClip(rect))
            {
                context.DrawText(fmtText, new Point(textX, textY));

                if (Underline)
                {
                    double strikeY = textY + textHeight + 0.5;
                    context.DrawLine(new Pen(brush, 1), new Point(textX, strikeY), new Point(textX + textWidth, strikeY));
                }
            }
        }

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

            if (Mode == ButtonCellMode.Icon)
            {
                RenderIcon(context, bounds, defaultForeground, typeface, fontSize, horAlign, verAlign, rawData);
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

            FormattedText fmtText;

            try
            {
                fmtText = CreateFormattedText(text, brush, typeface, fontSize);
            }
            catch
            {
                HitRect = buttonBounds;
                return;
            }

            DrawAlignedText(context, fmtText, buttonBounds, horAlign, verAlign, brush);

            HitRect = buttonBounds;
        }

        /// <summary>
        /// Renders the icon mode: the cell text is drawn normally to the left of a small icon
        /// placed at the right edge of the cell. Only the icon is clickable, so the rest of the
        /// cell keeps the normal spreadsheet behaviour.
        /// </summary>
        private void RenderIcon(DrawingContext context, Rect bounds, IBrush defaultForeground, Typeface typeface, double fontSize, TextAlignment horAlign, VerticalAlignment verAlign, string rawData)
        {
            double iconSize = Math.Max(8, Math.Min(IconSize, Math.Min(bounds.Width, bounds.Height)));
            double iconX = bounds.Right - iconSize - 3;
            double iconY = bounds.Y + (bounds.Height - iconSize) * 0.5;
            Rect iconRect = new Rect(iconX, iconY, iconSize, iconSize);

            // The text occupies the remaining area to the left of the icon, exactly like the
            // RefCell of the WPF project.
            Rect textRect = new Rect(bounds.X, bounds.Y, Math.Max(0, iconRect.Left - 2 - bounds.X), bounds.Height);

            string text = GetDisplayText(rawData);

            if (!string.IsNullOrEmpty(text) && textRect.Width > 3)
            {
                IBrush brush = Foreground ?? defaultForeground ?? Brushes.Black;

                FormattedText fmtText;

                try
                {
                    fmtText = CreateFormattedText(text, brush, typeface, fontSize);
                }
                catch
                {
                    fmtText = null;
                }

                if (fmtText != null)
                {
                    DrawAlignedText(context, fmtText, textRect, horAlign, verAlign, brush);
                }
            }

            if (iconRect.Width > 0 && iconRect.Height > 0)
            {
                bool active = IsHovered || IsPressed;

                if (active)
                {
                    IBrush hoverBackground = IconHoverBackground ?? new SolidColorBrush(Color.Parse("#FFE0E0E0"));
                    context.FillRectangle(hoverBackground, iconRect);
                }

                StreamGeometry geometry = GetIconGeometry();

                if (geometry != null)
                {
                    Rect geometryBounds = geometry.Bounds;

                    if (geometryBounds.Width > 0 && geometryBounds.Height > 0)
                    {
                        double scale = Math.Min((iconRect.Width - 2) / geometryBounds.Width, (iconRect.Height - 2) / geometryBounds.Height);

                        if (scale > 0 && !double.IsInfinity(scale))
                        {
                            double tx = iconRect.X + (iconRect.Width - geometryBounds.Width * scale) * 0.5 - geometryBounds.X * scale;
                            double ty = iconRect.Y + (iconRect.Height - geometryBounds.Height * scale) * 0.5 - geometryBounds.Y * scale;

                            // A single matrix combining the scale and the translation is enough:
                            // p' = (scale * x + tx, scale * y + ty).
                            Matrix transform = new Matrix(scale, 0, 0, scale, tx, ty);

                            IBrush iconBrush = active
                                ? (IconHoverFill ?? new SolidColorBrush(Color.Parse("#FF777777")))
                                : (IconFill ?? new SolidColorBrush(Color.Parse("#FFCCCCCC")));

                            using (context.PushTransform(transform))
                            {
                                context.DrawGeometry(iconBrush, null, geometry);
                            }
                        }
                    }
                }

                // Only the icon is clickable; the text area behaves like a normal cell.
                HitRect = iconRect;
            }
            else
            {
                HitRect = default;
            }
        }
    }
}
