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
using System.Collections.Generic;
using System.Globalization;

namespace Spreadalonia
{
    /// <summary>
    /// A custom cell body that renders multiple formatted text chunks inside a single cell.
    /// It is typically used for report captions (e.g. a title line "总分类账" followed by
    /// a subtitle line "2024.1-2024.1") that must be scaled to fit the merged cell bounds.
    /// </summary>
    public class TitleCell
    {
        /// <summary>
        /// The chunks of text that make up the cell content, drawn top-to-bottom in the order
        /// they appear in this list.
        /// </summary>
        public List<Chunk> Contents { get; set; }

        /// <summary>
        /// The original design height of the title cell, in pixels. The chunk positions and
        /// sizes are scaled proportionally when the actual cell height differs from this value.
        /// </summary>
        public double OrgHeight { get; set; }

        /// <summary>
        /// Creates a new empty <see cref="TitleCell"/>.
        /// </summary>
        public TitleCell()
        {
            Contents = new List<Chunk>();
        }

        /// <summary>
        /// Creates a new <see cref="TitleCell"/> with the specified content chunks.
        /// </summary>
        /// <param name="chunks">The chunks of text to render.</param>
        public TitleCell(List<Chunk> chunks)
        {
            Contents = chunks;
        }

        /// <summary>
        /// Renders the title cell into the specified drawing context.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <param name="bounds">The bounds available to the cell (usually the cell or merged range rectangle).</param>
        /// <param name="background">An optional background brush; if supplied, the bounds are filled before drawing the text.</param>
        /// <param name="margin">An optional margin; if <see langword="null"/> a scaled uniform margin of 3 pixels is used.</param>
        public void Render(DrawingContext context, Rect bounds, IBrush background, Thickness? margin)
        {
            if (Contents == null || Contents.Count == 0 || OrgHeight <= 0)
            {
                return;
            }

            if (background != null)
            {
                context.FillRectangle(background, bounds);
            }

            double scale = bounds.Height / OrgHeight;
            Thickness cellPad = margin ?? new Thickness(3 * scale);

            using (context.PushClip(new Rect(
                bounds.X + cellPad.Left,
                bounds.Y + cellPad.Top,
                Math.Max(0, bounds.Width - cellPad.Left - cellPad.Right),
                Math.Max(0, bounds.Height - cellPad.Top - cellPad.Bottom))))
            {
                foreach (Chunk chunk in Contents)
                {
                    if (chunk == null || string.IsNullOrEmpty(chunk.Text))
                    {
                        continue;
                    }

                    double chunkTop = chunk.Top * scale;
                    double chunkHeight = chunk.Height * scale;

                    double availableX = bounds.X + cellPad.Left;
                    double availableY = bounds.Y + chunkTop + cellPad.Top;
                    double availableWidth = Math.Max(0, bounds.Width - cellPad.Left - cellPad.Right);
                    double availableHeight = Math.Max(0, chunkHeight - cellPad.Top - cellPad.Bottom);

                    if (availableWidth <= 0 || availableHeight <= 0)
                    {
                        continue;
                    }

                    Typeface face = chunk.Typeface;
                    if (face == null)
                    {
                        FontFamily family = FontFamily.Default;
                        if (!string.IsNullOrEmpty(chunk.FontName))
                        {
                            try
                            {
                                family = new FontFamily(chunk.FontName);
                            }
                            catch
                            {
                                family = FontFamily.Default;
                            }
                        }

                        face = new Typeface(family, FontStyle.Normal, FontWeight.Normal);
                    }

                    double fontSize = chunk.FontSize > 0 ? chunk.FontSize : 12;
                    IBrush brush = chunk.Color ?? Brushes.Black;

                    // Some Avalonia versions throw when the font family cannot be resolved
                    // (e.g. a family name that is not installed on the machine, or a default
                    // Typeface whose FontFamily is null). Fall back to the default font so
                    // that an unresolvable font name never crashes the UI.
                    FormattedText fmtText = null;
                    double textWidth = 0;
                    double textHeight = 0;

                    try
                    {
                        fmtText = CreateFormattedText(chunk.Text, face, fontSize, brush);
                        textWidth = fmtText.Width;
                        textHeight = fmtText.Height;
                    }
                    catch
                    {
                        try
                        {
                            fmtText = CreateFormattedText(chunk.Text, new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Normal), fontSize, brush);
                            textWidth = fmtText.Width;
                            textHeight = fmtText.Height;
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    double textX = availableX;
                    double textY = availableY;

                    TextAlignment horAlign = chunk.HorAlign;
                    VerticalAlignment verAlign = chunk.VerAlign;

                    if (horAlign == TextAlignment.Center)
                    {
                        textX = availableX + (availableWidth - textWidth) * 0.5;
                    }
                    else if (horAlign == TextAlignment.Right)
                    {
                        textX = availableX + availableWidth - textWidth;
                    }

                    if (verAlign == VerticalAlignment.Center || verAlign == VerticalAlignment.Stretch)
                    {
                        textY = availableY + (availableHeight - textHeight) * 0.5;
                    }
                    else if (verAlign == VerticalAlignment.Bottom)
                    {
                        textY = availableY + availableHeight - textHeight;
                    }

                    context.DrawText(fmtText, new Point(textX, textY));
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

        /// <summary>
        /// A single formatted text chunk inside a <see cref="TitleCell"/>.
        /// </summary>
        public class Chunk
        {
            /// <summary>
            /// The text of the chunk.
            /// </summary>
            public string Text { get; set; }

            /// <summary>
            /// The font family name. If empty or invalid, the default font family is used.
            /// </summary>
            public string FontName { get; set; }

            /// <summary>
            /// An optional explicit typeface. If set, it takes precedence over <see cref="FontName"/>.
            /// </summary>
            public Typeface Typeface { get; set; }

            /// <summary>
            /// The font size, in device-independent pixels.
            /// </summary>
            public double FontSize { get; set; }

            /// <summary>
            /// The foreground brush used to draw the text.
            /// </summary>
            public IBrush Color { get; set; }

            /// <summary>
            /// The horizontal alignment of the chunk within its band.
            /// </summary>
            public TextAlignment HorAlign { get; set; }

            /// <summary>
            /// The vertical alignment of the chunk within its band.
            /// </summary>
            public VerticalAlignment VerAlign { get; set; }

            /// <summary>
            /// The top offset of the chunk, measured in the original design coordinates.
            /// </summary>
            public double Top { get; set; }

            /// <summary>
            /// The height of the chunk, measured in the original design coordinates.
            /// </summary>
            public double Height { get; set; }
        }
    }
}
