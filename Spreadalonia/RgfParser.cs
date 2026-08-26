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
using System.IO;
using System.Xml.Linq;

namespace Spreadalonia
{
    /// <summary>
    /// A parser for the RGF (Report Generator Format) XML format used by
    /// FastReport / ReportGenerator based reports.
    /// </summary>
    public static class RgfParser
    {
        /// <summary>
        /// Parses an RGF document from the specified stream.
        /// </summary>
        /// <param name="stream">The stream containing the RGF XML document.</param>
        /// <returns>A <see cref="RgfData"/> instance with the parsed content.</returns>
        public static RgfData Load(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            XDocument document = XDocument.Load(stream);
            XElement root = document.Root;

            if (root == null || root.Name.LocalName != "rgf")
            {
                throw new InvalidDataException("The stream does not contain a valid RGF document (expected a <rgf> root element).");
            }

            int width = GetInt(root, "width", 0);
            int height = GetInt(root, "height", 0);

            RgfData data = new RgfData() { Width = width, Height = height };

            // ---- styles ----
            Dictionary<string, RgfStyle> styles = new Dictionary<string, RgfStyle>();
            XElement stylesElement = root.Element("styles");
            if (stylesElement != null)
            {
                foreach (XElement styleElement in stylesElement.Elements("style"))
                {
                    string name = GetAttribute(styleElement, "name");
                    if (!string.IsNullOrEmpty(name))
                    {
                        styles[name] = ParseStyle(styleElement);
                    }
                }
            }

            // ---- cols ----
            XElement colsElement = root.Element("cols");
            if (colsElement != null)
            {
                foreach (XElement colElement in colsElement.Elements("col"))
                {
                    int from = GetInt(colElement, "from", 0);
                    int to = GetInt(colElement, "to", from);
                    double colWidth = GetDouble(colElement, "width", 0);
                    for (int c = from; c <= to; c++)
                    {
                        data.ColumnWidths[c] = colWidth;
                    }
                }
            }

            // ---- rows ----
            XElement rowsElement = root.Element("rows");
            if (rowsElement != null)
            {
                foreach (XElement rowElement in rowsElement.Elements("row"))
                {
                    int from = GetInt(rowElement, "from", 0);
                    int to = GetInt(rowElement, "to", from);
                    double rowHeight = GetDouble(rowElement, "height", 0);
                    for (int r = from; r <= to; r++)
                    {
                        data.RowHeights[r] = rowHeight;
                    }
                }
            }

            // ---- spans (merged cells) ----
            XElement spansElement = root.Element("spans");
            if (spansElement != null)
            {
                foreach (XElement spanElement in spansElement.Elements("span"))
                {
                    int rowFrom = GetInt(spanElement, "from", 0);
                    int rowSize = Math.Max(1, GetInt(spanElement, "size", 1));

                    foreach (XElement cellElement in spanElement.Elements("cell"))
                    {
                        int colFrom = GetInt(cellElement, "from", 0);
                        int colSize = Math.Max(1, GetInt(cellElement, "size", 1));
                        int colspan = Math.Max(1, GetInt(cellElement, "colspan", colSize));
                        int rowspan = Math.Max(1, GetInt(cellElement, "rowspan", rowSize));

                        int colRight = colFrom + colspan - 1;
                        int rowBottom = rowFrom + rowspan - 1;

                        if (colspan > 1 || rowspan > 1)
                        {
                            data.MergedRanges.Add(new SelectionRange(colFrom, rowFrom, colRight, rowBottom));
                        }

                        string text = cellElement.Value ?? string.Empty;
                        if (text.Length > 0)
                        {
                            data.Data[(colFrom, rowFrom)] = text;
                        }

                        ApplyCellElementStyle(data, styles, cellElement, colFrom, rowFrom);
                    }
                }
            }

            // ---- texts ----
            XElement textsElement = root.Element("texts");
            if (textsElement != null)
            {
                foreach (XElement textElement in textsElement.Elements("text"))
                {
                    int rowFrom = GetInt(textElement, "from", 0);
                    int rowSize = Math.Max(1, GetInt(textElement, "size", 1));

                    RgfStyle textStyle = null;
                    string textStyleName = GetAttribute(textElement, "style");
                    if (!string.IsNullOrEmpty(textStyleName))
                    {
                        styles.TryGetValue(textStyleName, out textStyle);
                    }

                    foreach (XElement cellElement in textElement.Elements("cell"))
                    {
                        int colFrom = GetInt(cellElement, "from", 0);
                        int colSize = Math.Max(1, GetInt(cellElement, "size", 1));

                        string text = cellElement.Value ?? string.Empty;
                        if (text.Length == 0)
                        {
                            continue;
                        }

                        RgfStyle cellStyle = textStyle;
                        string cellStyleName = GetAttribute(cellElement, "style");
                        if (!string.IsNullOrEmpty(cellStyleName) && styles.TryGetValue(cellStyleName, out RgfStyle parsedStyle))
                        {
                            cellStyle = parsedStyle;
                        }

                        for (int r = rowFrom; r < rowFrom + rowSize; r++)
                        {
                            for (int c = colFrom; c < colFrom + colSize; c++)
                            {
                                data.Data[(c, r)] = text;
                                ApplyStyle(data, cellStyle, c, r);
                            }
                        }

                        string background = GetAttribute(cellElement, "background");
                        if (!string.IsNullOrEmpty(background))
                        {
                            IBrush backgroundBrush = ParseBrush(background);
                            if (backgroundBrush != null)
                            {
                                data.CellBackgrounds.Add(new CellBackground(new SelectionRange(colFrom, rowFrom, colFrom + colSize - 1, rowFrom + rowSize - 1), backgroundBrush));
                            }
                        }
                    }
                }
            }

            // ---- v-borders ----
            XElement vBordersElement = root.Element("v-borders");
            if (vBordersElement != null)
            {
                foreach (XElement spanElement in vBordersElement.Elements("span"))
                {
                    int colFrom = GetInt(spanElement, "from", 0);
                    int colSize = Math.Max(1, GetInt(spanElement, "size", 1));

                    foreach (XElement borderElement in spanElement.Elements("border"))
                    {
                        BorderSides sides = ParseSides(GetAttribute(borderElement, "sides"), false);
                        IBrush brush = ParseBrush(GetAttribute(borderElement, "color"));
                        if (brush == null)
                        {
                            brush = Brushes.Black;
                        }
                        double thickness = GetDouble(borderElement, "width", 1);

                        int lastRow = Math.Max(0, height - 1);

                        for (int c = colFrom; c < colFrom + colSize; c++)
                        {
                            if ((sides & BorderSides.Left) != 0)
                            {
                                data.CellBorders.Add(new CellBorder(false, c, 0, lastRow, brush, thickness));
                            }
                            if ((sides & BorderSides.Right) != 0)
                            {
                                data.CellBorders.Add(new CellBorder(false, c + 1, 0, lastRow, brush, thickness));
                            }
                        }
                    }
                }
            }

            // ---- h-borders ----
            XElement hBordersElement = root.Element("h-borders");
            if (hBordersElement != null)
            {
                foreach (XElement spanElement in hBordersElement.Elements("span"))
                {
                    int rowFrom = GetInt(spanElement, "from", 0);
                    int rowSize = Math.Max(1, GetInt(spanElement, "size", 1));

                    foreach (XElement borderElement in spanElement.Elements("border"))
                    {
                        BorderSides sides = ParseSides(GetAttribute(borderElement, "sides"), true);
                        IBrush brush = ParseBrush(GetAttribute(borderElement, "color"));
                        if (brush == null)
                        {
                            brush = Brushes.Black;
                        }
                        double thickness = GetDouble(borderElement, "width", 1);

                        int lastColumn = Math.Max(0, width - 1);

                        for (int r = rowFrom; r < rowFrom + rowSize; r++)
                        {
                            if ((sides & BorderSides.Top) != 0)
                            {
                                data.CellBorders.Add(new CellBorder(true, r, 0, lastColumn, brush, thickness));
                            }
                            if ((sides & BorderSides.Bottom) != 0)
                            {
                                data.CellBorders.Add(new CellBorder(true, r + 1, 0, lastColumn, brush, thickness));
                            }
                        }
                    }
                }
            }

            return data;
        }

        private static void ApplyCellElementStyle(RgfData data, Dictionary<string, RgfStyle> styles, XElement cellElement, int col, int row)
        {
            string styleName = GetAttribute(cellElement, "style");
            if (string.IsNullOrEmpty(styleName))
            {
                return;
            }

            if (styles.TryGetValue(styleName, out RgfStyle style))
            {
                ApplyStyle(data, style, col, row);
            }
        }

        private static void ApplyStyle(RgfData data, RgfStyle style, int col, int row)
        {
            if (style == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(style.FontFamily))
            {
                FontFamily family;
                try
                {
                    family = new FontFamily(style.FontFamily);
                }
                catch
                {
                    family = FontFamily.Default;
                }

                data.CellTypefaces[(col, row)] = new Typeface(family, style.FontStyle, style.FontWeight);
            }

            if (style.FontSize > 0)
            {
                data.CellFontSize[(col, row)] = style.FontSize;
            }

            if (style.Foreground != null)
            {
                data.CellForeground[(col, row)] = style.Foreground;
            }

            if (style.Horizontal.HasValue)
            {
                data.CellTextAlignment[(col, row)] = style.Horizontal.Value;
            }

            if (style.Vertical.HasValue)
            {
                data.CellVerticalAlignment[(col, row)] = style.Vertical.Value;
            }
        }

        private static RgfStyle ParseStyle(XElement styleElement)
        {
            RgfStyle style = new RgfStyle();

            XElement fontElement = styleElement.Element("font");
            if (fontElement != null)
            {
                style.FontFamily = GetAttribute(fontElement, "family");
                style.FontSize = GetDouble(fontElement, "size", 0);
                int bold = GetInt(fontElement, "bold", 0);
                int italic = GetInt(fontElement, "italic", 0);
                int underline = GetInt(fontElement, "underline", 0);

                style.FontStyle = italic != 0 ? FontStyle.Italic : FontStyle.Normal;
                style.FontWeight = bold != 0 ? FontWeight.Bold : FontWeight.Normal;
            }

            XElement alignElement = styleElement.Element("align");
            if (alignElement != null)
            {
                string h = GetAttribute(alignElement, "h");
                if (!string.IsNullOrEmpty(h))
                {
                    switch (h)
                    {
                        case "left":
                            style.Horizontal = TextAlignment.Left;
                            break;
                        case "right":
                            style.Horizontal = TextAlignment.Right;
                            break;
                        case "center":
                            style.Horizontal = TextAlignment.Center;
                            break;
                        default:
                            style.Horizontal = TextAlignment.Justify;
                            break;
                    }
                }

                string v = GetAttribute(alignElement, "v");
                if (!string.IsNullOrEmpty(v))
                {
                    switch (v)
                    {
                        case "top":
                            style.Vertical = VerticalAlignment.Top;
                            break;
                        case "bottom":
                            style.Vertical = VerticalAlignment.Bottom;
                            break;
                        case "center":
                            style.Vertical = VerticalAlignment.Center;
                            break;
                        default:
                            style.Vertical = VerticalAlignment.Stretch;
                            break;
                    }
                }
            }

            string foreground = GetAttribute(styleElement, "foreground");
            if (string.IsNullOrEmpty(foreground))
            {
                XElement textElement = styleElement.Element("text");
                if (textElement != null)
                {
                    foreground = GetAttribute(textElement, "color");
                }
            }

            if (!string.IsNullOrEmpty(foreground))
            {
                style.Foreground = ParseBrush(foreground);
            }

            return style;
        }

        private static BorderSides ParseSides(string sides, bool isHorizontal)
        {
            if (string.IsNullOrEmpty(sides))
            {
                return isHorizontal ? BorderSides.TopBottom : BorderSides.LeftRight;
            }

            BorderSides result = BorderSides.None;

            switch (sides.Trim().ToLowerInvariant())
            {
                case "left":
                    result = BorderSides.Left;
                    break;
                case "right":
                    result = BorderSides.Right;
                    break;
                case "top":
                    result = BorderSides.Top;
                    break;
                case "bottom":
                    result = BorderSides.Bottom;
                    break;
                case "both":
                case "left-right":
                case "leftright":
                case "lr":
                case "top-bottom":
                case "topbottom":
                case "tb":
                    result = isHorizontal ? BorderSides.TopBottom : BorderSides.LeftRight;
                    break;
                case "all":
                case "allborders":
                    result = BorderSides.All;
                    break;
                default:
                    if (sides.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result |= BorderSides.Left;
                    }
                    if (sides.IndexOf("right", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result |= BorderSides.Right;
                    }
                    if (sides.IndexOf("top", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result |= BorderSides.Top;
                    }
                    if (sides.IndexOf("bottom", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        result |= BorderSides.Bottom;
                    }
                    break;
            }

            return result;
        }

        private static string GetAttribute(XElement element, string name)
        {
            XAttribute attribute = element.Attribute(name);
            return attribute?.Value;
        }

        private static int GetInt(XElement element, string name, int defaultValue)
        {
            string value = GetAttribute(element, name);
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            {
                return result;
            }

            return defaultValue;
        }

        private static double GetDouble(XElement element, string name, double defaultValue)
        {
            string value = GetAttribute(element, name);
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            return defaultValue;
        }

        private static IBrush ParseBrush(string color)
        {
            if (string.IsNullOrEmpty(color))
            {
                return null;
            }

            try
            {
                if (color.StartsWith("#", StringComparison.Ordinal))
                {
                    string parsedColor = color;
                    if (parsedColor.Length == 7)
                    {
                        parsedColor = "#FF" + parsedColor.Substring(1);
                    }
                    return new SolidColorBrush(Color.Parse(parsedColor));
                }
                else
                {
                    return Brush.Parse(color);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
