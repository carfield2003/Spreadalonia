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

using Avalonia.Media;
using System;
using System.Globalization;

namespace Spreadalonia
{
    /// <summary>
    /// Truncates text with a trailing ellipsis so that it fits into a given width. Used by
    /// numeric cells whose formatted value does not fit into the column, which are then drawn
    /// as e.g. "1,120,7…" instead of being clipped mid-glyph.
    /// </summary>
    internal static class TextTruncation
    {
        /// <summary>
        /// Returns the longest prefix of the text followed by "…" that fits into the specified
        /// width, measured with the given typeface, font size and brush. When the text already
        /// fits, when it cannot be measured, or when not even the ellipsis fits into the width
        /// (in which case the regular clipping takes over), the text is returned unchanged.
        /// </summary>
        /// <param name="text">The text to truncate.</param>
        /// <param name="face">The typeface used to measure the text.</param>
        /// <param name="fontSize">The font size used to measure the text.</param>
        /// <param name="brush">The brush passed to <see cref="FormattedText"/> (it does not affect the measurement).</param>
        /// <param name="maxWidth">The available width.</param>
        internal static string TruncateWithEllipsis(string text, Typeface face, double fontSize, IBrush brush, double maxWidth)
        {
            if (string.IsNullOrEmpty(text) || maxWidth <= 0 || face.FontFamily == null)
            {
                return text;
            }

            try
            {
                double ellipsisWidth = new FormattedText("…", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, face, fontSize, brush).Width;

                // Not even the ellipsis fits into the cell: keep the text and let the regular
                // clipping handle it.
                if (ellipsisWidth >= maxWidth)
                {
                    return text;
                }

                int lo = 0;
                int hi = text.Length;

                // Binary search for the longest prefix that still leaves room for the ellipsis.
                while (lo < hi)
                {
                    int mid = (lo + hi + 1) / 2;

                    double width = new FormattedText(text.Substring(0, mid), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, face, fontSize, brush).Width;

                    if (width + ellipsisWidth <= maxWidth)
                    {
                        lo = mid;
                    }
                    else
                    {
                        hi = mid - 1;
                    }
                }

                if (lo <= 0)
                {
                    return "…";
                }

                return text.Substring(0, lo) + "…";
            }
            catch
            {
                return text;
            }
        }
    }
}
