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
    /// A custom cell body that renders a clickable hyperlink (coloured text, optionally
    /// underlined with a strikethrough line). Hovering over the link text changes the cursor
    /// to a hand; pressing and releasing the left button (or pressing/releasing Space while
    /// the cell is selected) raises the <see cref="Spreadsheet.LinkClick"/> event.
    /// </summary>
    public class LinkCell
    {
        /// <summary>
        /// The text displayed in the cell.
        /// </summary>
        public string DisplayText { get; set; }

        /// <summary>
        /// The navigation parameter carried by the <see cref="Spreadsheet.LinkClick"/> event
        /// (e.g. the URL to open).
        /// </summary>
        public string LinkPara { get; set; }

        /// <summary>
        /// The colour of the normal hyperlink.
        /// </summary>
        public IBrush LinkColor { get; set; }

        /// <summary>
        /// The colour of the hyperlink while it is pressed.
        /// </summary>
        public IBrush ActivateColor { get; set; }

        /// <summary>
        /// The colour of the hyperlink after it has been visited.
        /// </summary>
        public IBrush VisitedColor { get; set; }

        /// <summary>
        /// Whether a strikethrough line is drawn through the text.
        /// </summary>
        public bool Strikethrough { get; set; }

        /// <summary>
        /// Whether the link has been clicked (visited) before. Visited links are drawn with
        /// <see cref="VisitedColor"/>.
        /// </summary>
        public bool IsVisited { get; set; }

        internal bool IsPressed { get; set; }

        internal Rect HitRect { get; set; }

        /// <summary>
        /// Creates a new empty <see cref="LinkCell"/>.
        /// </summary>
        public LinkCell() : this("", "{}", false)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LinkCell"/> with the specified display text and link parameter.
        /// </summary>
        /// <param name="displayText">The text displayed in the cell.</param>
        /// <param name="linkPara">The navigation parameter carried by the click event.</param>
        public LinkCell(string displayText, string linkPara) : this(displayText, linkPara, false)
        {
        }

        /// <summary>
        /// Creates a new <see cref="LinkCell"/> with the specified display text, link parameter
        /// and strikethrough style.
        /// </summary>
        /// <param name="displayText">The text displayed in the cell.</param>
        /// <param name="linkPara">The navigation parameter carried by the click event.</param>
        /// <param name="strikethrough">Whether a strikethrough line is drawn through the text.</param>
        public LinkCell(string displayText, string linkPara, bool strikethrough)
        {
            DisplayText = displayText;
            LinkPara = linkPara;
            Strikethrough = strikethrough;
            LinkColor = new SolidColorBrush(Color.Parse("#337ab7"));
            ActivateColor = new SolidColorBrush(Color.Parse("#23527c"));
            VisitedColor = new SolidColorBrush(Color.Parse("#337ab7"));
        }

        /// <summary>
        /// Computes the text to draw. The base implementation returns <see cref="DisplayText"/>;
        /// derived classes (e.g. <see cref="NumericLinkCell"/>) may format a raw cell value instead.
        /// </summary>
        /// <param name="rawData">The raw string stored in the cell, or <see langword="null"/>.</param>
        internal virtual string GetDisplayText(string rawData)
        {
            return DisplayText;
        }

        /// <summary>
        /// Renders the link cell into the specified drawing context and updates the internal
        /// hit-test rectangle used to detect hover and click on the link text.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <param name="bounds">The bounds available to the cell (usually the cell or merged range rectangle).</param>
        /// <param name="background">An optional background brush; if supplied, the bounds are filled before drawing the text.</param>
        /// <param name="margin">An optional margin; if <see langword="null"/> a margin of 3 pixels is used.</param>
        /// <param name="rawData">The raw string stored in the cell (used by derived classes for formatting).</param>
        /// <param name="typeface">The typeface used to draw the text.</param>
        /// <param name="fontSize">The font size used to draw the text.</param>
        /// <param name="horAlign">The horizontal alignment of the text within the cell.</param>
        /// <param name="verAlign">The vertical alignment of the text within the cell.</param>
        public void Render(DrawingContext context, Rect bounds, IBrush background, Thickness? margin, string rawData, Typeface typeface, double fontSize, TextAlignment horAlign, VerticalAlignment verAlign)
        {
            if (background != null)
            {
                context.FillRectangle(background, bounds);
            }

            string text = GetDisplayText(rawData);

            if (string.IsNullOrEmpty(text))
            {
                HitRect = default;
                return;
            }

            IBrush brush = IsPressed ? (ActivateColor ?? LinkColor) : (IsVisited ? (VisitedColor ?? LinkColor) : LinkColor);
            brush ??= Brushes.Black;

            Thickness pad = margin ?? new Thickness(3);

            // Guard against unresolvable fonts / uninitialised Typefaces, same as TitleCell.
            FormattedText fmtText = null;
            double textWidth = 0;
            double textHeight = 0;

            try
            {
                fmtText = CreateFormattedText(text, typeface, fontSize, brush);
                textWidth = fmtText.Width;
                textHeight = fmtText.Height;
            }
            catch
            {
                try
                {
                    fmtText = CreateFormattedText(text, new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal), fontSize, brush);
                    textWidth = fmtText.Width;
                    textHeight = fmtText.Height;
                }
                catch
                {
                    HitRect = default;
                    return;
                }
            }

            double textX = bounds.X;
            double textY = bounds.Y;

            if (verAlign == VerticalAlignment.Top)
            {
                textY = bounds.Y + pad.Top;
            }
            else if (verAlign == VerticalAlignment.Bottom)
            {
                textY = bounds.Y + bounds.Height - pad.Bottom - textHeight;
            }
            else
            {
                textY = (bounds.Y + pad.Top + bounds.Y + bounds.Height - pad.Bottom) * 0.5 - textHeight * 0.5;
            }

            if (horAlign == TextAlignment.Left)
            {
                textX = bounds.X + pad.Left;
            }
            else if (horAlign == TextAlignment.Center)
            {
                textX = (bounds.X + pad.Left + bounds.X + bounds.Width - pad.Right) * 0.5 - textWidth * 0.5;
            }
            else if (horAlign == TextAlignment.Right)
            {
                textX = bounds.X + bounds.Width - pad.Right - textWidth;
            }

            HitRect = new Rect(textX, textY, textWidth, textHeight);

            using (context.PushClip(new Rect(
                bounds.X + pad.Left,
                bounds.Y + pad.Top,
                Math.Max(0, bounds.Width - pad.Left - pad.Right),
                Math.Max(0, bounds.Height - pad.Top - pad.Bottom))))
            {
                context.DrawText(fmtText, new Point(textX, textY));

                if (Strikethrough)
                {
                    double strikeY = textY + textHeight * 0.5;
                    context.DrawLine(new Pen(brush, 1), new Point(textX, strikeY), new Point(textX + textWidth, strikeY));
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="FormattedText"/> for the given text, guarding against an
        /// uninitialised (default) <see cref="Typeface"/> whose <see cref="Typeface.FontFamily"/>
        /// is null. Passing such a typeface to <see cref="FormattedText"/> makes the text
        /// formatter throw a <see cref="NullReferenceException"/> while measuring the text.
        /// </summary>
        private static FormattedText CreateFormattedText(string text, Typeface face, double fontSize, IBrush brush)
        {
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
    }

    /// <summary>
    /// A <see cref="LinkCell"/> that formats a numeric value with thousand separators and a
    /// fixed number of decimal digits. Values of zero (and non-numeric values) are displayed
    /// as empty text; when <see cref="ResponseEmpty"/> is set the empty display becomes "…"
    /// and the cell remains clickable.
    /// </summary>
    public class NumericLinkCell : LinkCell
    {
        /// <summary>
        /// The number of decimal digits shown.
        /// </summary>
        public int Digits { get; set; }

        /// <summary>
        /// Whether an empty (zero / invalid) value is displayed as "…" and stays clickable.
        /// </summary>
        public bool ResponseEmpty { get; set; }

        /// <summary>
        /// Creates a new <see cref="NumericLinkCell"/> with two decimal digits.
        /// </summary>
        public NumericLinkCell() : this("{}", false, false, 2)
        {
        }

        /// <summary>
        /// Creates a new <see cref="NumericLinkCell"/> with the specified link parameter.
        /// </summary>
        /// <param name="linkPara">The navigation parameter carried by the click event.</param>
        public NumericLinkCell(string linkPara) : this(linkPara, false, false, 2)
        {
        }

        /// <summary>
        /// Creates a new <see cref="NumericLinkCell"/> with the specified link parameter and
        /// empty-value response.
        /// </summary>
        /// <param name="linkPara">The navigation parameter carried by the click event.</param>
        /// <param name="responseEmpty">Whether an empty value is displayed as "…" and stays clickable.</param>
        public NumericLinkCell(string linkPara, bool responseEmpty) : this(linkPara, responseEmpty, false, 2)
        {
        }

        /// <summary>
        /// Creates a new <see cref="NumericLinkCell"/> with full configuration.
        /// </summary>
        /// <param name="linkPara">The navigation parameter carried by the click event.</param>
        /// <param name="responseEmpty">Whether an empty value is displayed as "…" and stays clickable.</param>
        /// <param name="strikethrough">Whether a strikethrough line is drawn through the text.</param>
        /// <param name="digits">The number of decimal digits shown.</param>
        public NumericLinkCell(string linkPara, bool responseEmpty, bool strikethrough, int digits)
        {
            LinkPara = linkPara;
            ResponseEmpty = responseEmpty;
            Strikethrough = strikethrough;
            Digits = digits;
        }

        /// <inheritdoc/>
        internal override string GetDisplayText(string rawData)
        {
            string text = string.Empty;

            if (!string.IsNullOrWhiteSpace(rawData) &&
                decimal.TryParse(rawData.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out decimal value) &&
                value != 0m)
            {
                value = Math.Floor(value * (decimal)Math.Pow(10, Digits) + 0.5m) / (decimal)Math.Pow(10, Digits);
                string format = "#,#0." + new string('0', Digits);
                text = value.ToString(format, CultureInfo.CurrentCulture);
            }

            if (ResponseEmpty && text.Length == 0)
            {
                return "…";
            }

            return text;
        }
    }
}
