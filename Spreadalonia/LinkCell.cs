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

        /// <summary>
        /// How large the area that reacts to hovering and clicking is. The default
        /// (<see cref="LinkHitMode.TextOnly"/>) keeps the historical behaviour of reacting to the
        /// rendered text only — which for an empty-value placeholder ("…") is just a few pixels
        /// wide and therefore almost impossible to hit. <see cref="LinkHitMode.EntireCell"/> makes
        /// the whole cell (or merged range) reactive, and
        /// <see cref="LinkHitMode.EntireCellWhenEmpty"/> does so only while the cell shows its
        /// empty-value placeholder. Both the hand cursor and the click use this rectangle, so the
        /// two can never disagree.
        /// </summary>
        public LinkHitMode HitMode { get; set; } = LinkHitMode.TextOnly;

        /// <summary>
        /// The placeholder drawn when the cell has nothing to show — e.g. a
        /// <see cref="NumericLinkCell"/> whose value is zero, empty or not a number — so that the
        /// cell keeps a clickable target. When this is <see langword="null"/> or empty the cell
        /// falls back to <see cref="Table.EmptyNumericLinkText"/> and finally to
        /// <see cref="NumericLinkCell.DefaultEmptyText"/> (both "…" by default). When every level
        /// is empty the cell shows nothing and cannot be clicked.
        /// </summary>
        public string EmptyText { get; set; }

        /// <summary>
        /// The brush used to draw the empty-value placeholder (see <see cref="EmptyText"/>). When
        /// this is <see langword="null"/> (the default) the normal link colour is used, so that
        /// the placeholder stays recognisable as a link.
        /// </summary>
        public IBrush EmptyBrush { get; set; }

        internal bool IsPressed { get; set; }

        internal Rect HitRect { get; set; }

        /// <summary>
        /// The table the cell was last rendered by. Used to resolve table level defaults such as
        /// <see cref="Table.EmptyNumericLinkText"/>. Assigned by <see cref="Table"/> right before
        /// the cell is rendered; it is <see langword="null"/> for a cell that has never been drawn.
        /// </summary>
        internal Table OwnerTable { get; set; }

        /// <summary>
        /// Whether the display text is truncated with a trailing ellipsis ("…") when it does not
        /// fit into the cell, instead of being clipped mid-glyph.
        /// </summary>
        protected virtual bool UseEllipsisWhenClipped => false;

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
        /// Computes the text to draw. The base implementation returns <see cref="DisplayText"/>
        /// (or <see cref="EmptyText"/> when there is nothing to show); derived classes (e.g.
        /// <see cref="NumericLinkCell"/>) may format a raw cell value instead.
        /// </summary>
        /// <param name="rawData">The raw string stored in the cell, or <see langword="null"/>.</param>
        /// <param name="isEmptyPlaceholder">
        /// When this method returns, <see langword="true"/> if the returned text is the
        /// empty-value placeholder rather than a real value.
        /// </param>
        internal virtual string GetDisplayText(string rawData, out bool isEmptyPlaceholder)
        {
            if (string.IsNullOrEmpty(DisplayText) && !string.IsNullOrEmpty(EmptyText))
            {
                isEmptyPlaceholder = true;
                return EmptyText;
            }

            isEmptyPlaceholder = false;
            return DisplayText;
        }

        /// <summary>
        /// Computes the text to draw (see <see cref="GetDisplayText(string, out bool)"/>).
        /// </summary>
        /// <param name="rawData">The raw string stored in the cell, or <see langword="null"/>.</param>
        internal string GetDisplayText(string rawData)
        {
            return GetDisplayText(rawData, out _);
        }

        /// <summary>
        /// Computes the text that leaves the control: xlsx export, the clipboard and printed
        /// output must see the underlying value, never the display-only empty-value placeholder.
        /// The base implementation returns <see cref="DisplayText"/>.
        /// </summary>
        /// <param name="rawData">The raw string stored in the cell, or <see langword="null"/>.</param>
        internal virtual string GetExportText(string rawData)
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

            string text = GetDisplayText(rawData, out bool isEmptyPlaceholder);

            if (string.IsNullOrEmpty(text))
            {
                HitRect = default;
                return;
            }

            IBrush brush = IsPressed ? (ActivateColor ?? LinkColor) : (IsVisited ? (VisitedColor ?? LinkColor) : LinkColor);

            // The placeholder is drawn like the link itself (same colour unless the host overrides
            // EmptyBrush) so that the user can see that the cell is an entry point.
            if (isEmptyPlaceholder && EmptyBrush != null)
            {
                brush = EmptyBrush;
            }

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

            // When the display text does not fit into the cell, truncate it with a trailing
            // ellipsis ("1,120,7…") instead of letting the regular clipping cut it mid-glyph.
            double availableWidth = Math.Max(0, bounds.Width - pad.Left - pad.Right);

            if (textWidth > availableWidth && UseEllipsisWhenClipped)
            {
                string truncated = TextTruncation.TruncateWithEllipsis(text, typeface, fontSize, brush, availableWidth);

                if (truncated != text)
                {
                    text = truncated;

                    try
                    {
                        fmtText = CreateFormattedText(text, typeface, fontSize, brush);
                        textWidth = fmtText.Width;
                        textHeight = fmtText.Height;
                    }
                    catch
                    {
                        // Measurement of the truncated text failed; keep the original one.
                    }
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

            HitRect = GetHitRect(bounds, new Rect(textX, textY, textWidth, textHeight), isEmptyPlaceholder);

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
        /// Computes the rectangle that reacts to hovering and clicking (see <see cref="HitMode"/>).
        /// </summary>
        /// <param name="bounds">The bounds of the cell (or of the merged range).</param>
        /// <param name="textRect">The rectangle actually covered by the rendered text.</param>
        /// <param name="isEmptyPlaceholder">Whether the rendered text is the empty-value placeholder.</param>
        private Rect GetHitRect(Rect bounds, Rect textRect, bool isEmptyPlaceholder)
        {
            bool entireCell = HitMode == LinkHitMode.EntireCell ||
                (HitMode == LinkHitMode.EntireCellWhenEmpty && isEmptyPlaceholder);

            if (!entireCell)
            {
                return textRect;
            }

            // The bottom-right corner of a selected cell is the fill handle: it stays outside the
            // hit rectangle so that the host can still drag-fill from a link cell.
            return new Rect(
                bounds.X,
                bounds.Y,
                Math.Max(0, bounds.Width - FillHandleMargin),
                Math.Max(0, bounds.Height - FillHandleMargin));
        }

        /// <summary>
        /// The size of the corner kept free for the fill handle when the hit area covers the whole
        /// cell. The fill handle itself is detected within 3 pixels of the corner.
        /// </summary>
        private const double FillHandleMargin = 4;

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
    /// as empty text; when <see cref="ResponseEmpty"/> is set (the default) the empty display
    /// becomes the placeholder "…" and the cell remains clickable, so that a zero amount is
    /// still a usable entry point.
    /// </summary>
    public class NumericLinkCell : LinkCell
    {
        /// <summary>
        /// The number of decimal digits shown.
        /// </summary>
        public int Digits { get; set; }

        /// <summary>
        /// Whether an empty (zero / invalid) value is displayed as a placeholder and stays
        /// clickable. Defaults to <see langword="true"/>.
        /// </summary>
        public bool ResponseEmpty { get; set; } = true;

        /// <summary>
        /// The placeholder used by every <see cref="NumericLinkCell"/> that does not set
        /// <see cref="LinkCell.EmptyText"/> itself and that is not attached to a table (or whose
        /// <see cref="Table.EmptyNumericLinkText"/> is not set). Defaults to "…"; set it to
        /// <see langword="null"/> or "" to switch the placeholder off globally.
        /// </summary>
        public static string DefaultEmptyText { get; set; } = "…";

        /// <summary>
        /// Creates a new <see cref="NumericLinkCell"/> with two decimal digits.
        /// </summary>
        public NumericLinkCell() : this("{}", true, false, 2)
        {
        }

        /// <summary>
        /// Creates a new <see cref="NumericLinkCell"/> with the specified link parameter.
        /// </summary>
        /// <param name="linkPara">The navigation parameter carried by the click event.</param>
        public NumericLinkCell(string linkPara) : this(linkPara, true, false, 2)
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

            // A numeric link cell is (right) aligned inside a usually wide amount column, so the
            // placeholder "…" would otherwise be a 3–5 pixel wide target. The whole cell reacts
            // while the placeholder is shown; the normal value keeps the historical text-only
            // behaviour. Set HitMode to LinkHitMode.EntireCell to react to the whole cell always.
            HitMode = LinkHitMode.EntireCellWhenEmpty;
        }

        /// <summary>
        /// Creates a new <see cref="NumericLinkCell"/> with full configuration.
        /// </summary>
        /// <param name="linkPara">The navigation parameter carried by the click event.</param>
        /// <param name="responseEmpty">Whether an empty value is displayed as a placeholder and stays clickable.</param>
        /// <param name="strikethrough">Whether a strikethrough line is drawn through the text.</param>
        /// <param name="digits">The number of decimal digits shown.</param>
        /// <param name="hitMode">How large the area that reacts to hovering and clicking is.</param>
        public NumericLinkCell(string linkPara, bool responseEmpty, bool strikethrough, int digits, LinkHitMode hitMode)
            : this(linkPara, responseEmpty, strikethrough, digits)
        {
            HitMode = hitMode;
        }

        /// <inheritdoc/>
        protected override bool UseEllipsisWhenClipped => true;

        /// <inheritdoc/>
        internal override string GetDisplayText(string rawData, out bool isEmptyPlaceholder)
        {
            isEmptyPlaceholder = false;

            // Shares the implementation with NumericCell so that the two can never drift apart.
            string text = NumberFormatter.Format(rawData, Digits, true);

            if (ResponseEmpty && text.Length == 0)
            {
                string placeholder = ResolveEmptyText();

                if (!string.IsNullOrEmpty(placeholder))
                {
                    isEmptyPlaceholder = true;
                    return placeholder;
                }
            }

            return text;
        }

        /// <inheritdoc/>
        internal override string GetExportText(string rawData)
        {
            // The placeholder is display only: an export must see the (empty) value instead.
            return NumberFormatter.Format(rawData, Digits, true);
        }

        /// <summary>
        /// Resolves the placeholder to draw: the cell's own <see cref="LinkCell.EmptyText"/> wins,
        /// then the table level <see cref="Table.EmptyNumericLinkText"/>, then
        /// <see cref="DefaultEmptyText"/>.
        /// </summary>
        private string ResolveEmptyText()
        {
            if (!string.IsNullOrEmpty(EmptyText))
            {
                return EmptyText;
            }

            if (OwnerTable != null)
            {
                return OwnerTable.EmptyNumericLinkText;
            }

            return DefaultEmptyText;
        }
    }

    /// <summary>
    /// Determines how large the area of a <see cref="LinkCell"/> that reacts to hovering and
    /// clicking is.
    /// </summary>
    public enum LinkHitMode
    {
        /// <summary>
        /// Only the rectangle covered by the rendered text reacts. This is the historical
        /// behaviour and the default, kept so that existing sheets do not change.
        /// </summary>
        TextOnly,

        /// <summary>
        /// The whole cell (or merged range) reacts, including the empty space next to the text.
        /// </summary>
        EntireCell,

        /// <summary>
        /// The whole cell reacts while the cell shows its empty-value placeholder; the text
        /// rectangle reacts otherwise. This is what an amount column needs: the placeholder "…"
        /// is only a few pixels wide, but the cell has to stay clickable everywhere.
        /// </summary>
        EntireCellWhenEmpty
    }
}
