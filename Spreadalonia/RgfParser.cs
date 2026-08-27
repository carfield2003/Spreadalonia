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
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace Spreadalonia
{
    /// <summary>
    /// A parser for RGF documents. It supports two formats:
    /// <list type="bullet">
    /// <item>The legacy Spreadalonia <c>&lt;rgf&gt;</c> XML format.</item>
    /// <item>The native ReoGrid v3 RGF format (plain XML or GZip-compressed, root element <c>&lt;grid&gt;</c>).</item>
    /// </list>
    /// The format is detected automatically from the root element, so files exported by ReoGrid
    /// (e.g. through <c>grid.Load(stream, FileFormat.ReoGridFormat)</c>) can be loaded directly.
    /// </summary>
    public static class RgfParser
    {
        /// <summary>
        /// Parses an RGF document from the specified stream.
        /// </summary>
        /// <param name="stream">The stream containing the RGF document (either the legacy <c>&lt;rgf&gt;</c> format or the native ReoGrid <c>&lt;grid&gt;</c> format).</param>
        /// <returns>A <see cref="RgfData"/> instance with the parsed content.</returns>
        public static RgfData Load(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            Stream xmlStream = stream;

            // The native ReoGrid RGF format is usually GZip-compressed; detect it by its magic bytes.
            if (stream.CanSeek)
            {
                long position = stream.Position;
                int b1 = stream.ReadByte();
                int b2 = stream.ReadByte();
                stream.Position = position;

                if (b1 == 0x1F && b2 == 0x8B)
                {
                    xmlStream = new GZipStream(stream, CompressionMode.Decompress);
                }
            }
            else
            {
                MemoryStream buffer = new MemoryStream();
                stream.CopyTo(buffer);
                buffer.Position = 0;

                byte[] header = new byte[2];
                buffer.Read(header, 0, 2);
                buffer.Position = 0;

                if (header[0] == 0x1F && header[1] == 0x8B)
                {
                    xmlStream = new GZipStream(buffer, CompressionMode.Decompress);
                }
                else
                {
                    xmlStream = buffer;
                }
            }

            XDocument document;
            try
            {
                document = XDocument.Load(xmlStream);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("The stream does not contain a valid RGF document.", ex);
            }

            XElement root = document.Root;

            if (root == null)
            {
                throw new InvalidDataException("The stream does not contain a valid RGF document (the root element is missing).");
            }

            if (root.Name.LocalName == "rgf")
            {
                return LoadLegacyRgf(root);
            }

            if (root.Name.LocalName == "grid")
            {
                return LoadReoGrid(root);
            }

            throw new InvalidDataException("The stream does not contain a valid RGF document (expected a <rgf> or a ReoGrid <grid> root element).");
        }

        private static RgfData LoadLegacyRgf(XElement root)
        {
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

        private static RgfData LoadReoGrid(XElement root)
        {
            XElement headElement = GetChild(root, "head");

            int height = GetElementInt(headElement, "rows", 0);
            int width = GetElementInt(headElement, "cols", 0);

            RgfData data = new RgfData() { Width = width, Height = height };

            double defaultRowHeight = GetElementDouble(headElement, "default-row-height", 0);
            double defaultColWidth = GetElementDouble(headElement, "default-col-width", 0);

            if (defaultRowHeight > 0)
            {
                for (int r = 0; r < height; r++)
                {
                    data.RowHeights[r] = defaultRowHeight;
                }
            }

            if (defaultColWidth > 0)
            {
                for (int c = 0; c < width; c++)
                {
                    data.ColumnWidths[c] = defaultColWidth;
                }
            }

            // ---- global style ----
            RgfStyle globalStyle = ParseReoGridStyle(GetChild(root, "style"), null);

            // ---- rows ----
            Dictionary<int, RgfStyle> rowStyles = new Dictionary<int, RgfStyle>();
            XElement rowsElement = GetChild(root, "rows");
            if (rowsElement != null)
            {
                foreach (XElement rowElement in GetChildren(rowsElement, "row"))
                {
                    int row = GetInt(rowElement, "row", 0);
                    double rowHeight = GetDouble(rowElement, "height", defaultRowHeight);
                    if (rowHeight > 0)
                    {
                        data.RowHeights[row] = rowHeight;
                    }

                    RgfStyle rowStyle = ParseReoGridStyle(GetChild(rowElement, "style"), null);
                    rowStyles[row] = rowStyle;

                    if (rowStyle.Background != null)
                    {
                        data.CellBackgrounds.Add(new CellBackground(new SelectionRange(0, row, Math.Max(0, width - 1), row), rowStyle.Background));
                    }
                }
            }

            // ---- cols ----
            Dictionary<int, RgfStyle> colStyles = new Dictionary<int, RgfStyle>();
            XElement colsElement = GetChild(root, "cols");
            if (colsElement != null)
            {
                foreach (XElement colElement in GetChildren(colsElement, "col"))
                {
                    int col = GetInt(colElement, "col", 0);
                    double colWidth = GetDouble(colElement, "width", defaultColWidth);
                    if (colWidth > 0)
                    {
                        data.ColumnWidths[col] = colWidth;
                    }

                    RgfStyle colStyle = ParseReoGridStyle(GetChild(colElement, "style"), null);
                    colStyles[col] = colStyle;

                    if (colStyle.Background != null)
                    {
                        data.CellBackgrounds.Add(new CellBackground(new SelectionRange(col, 0, col, Math.Max(0, height - 1)), colStyle.Background));
                    }
                }
            }

            // ---- vertical borders ----
            // <v-border row="1" col="0" pos="left" rows="2" />  ->  separator line `col` on the left edge of column `col`
            // <v-border row="1" col="7" pos="right" rows="2" /> ->  separator line `col + 1` on the right edge of column `col`
            //                                                (when col == width it represents the right edge of the table)
            XElement vBordersElement = GetChild(root, "v-borders");
            if (vBordersElement != null)
            {
                foreach (XElement vBorderElement in GetChildren(vBordersElement, "v-border"))
                {
                    int row = GetInt(vBorderElement, "row", 0);
                    int col = GetInt(vBorderElement, "col", 0);
                    int rows = Math.Max(1, GetInt(vBorderElement, "rows", 1));
                    string pos = (GetAttribute(vBorderElement, "pos") ?? "all").ToLowerInvariant();
                    IBrush brush = ParseBrush(GetAttribute(vBorderElement, "color"));
                    if (brush == null)
                    {
                        brush = Brushes.Black;
                    }
                    double thickness = GetDouble(vBorderElement, "width", 1);

                    int fromRow = row;
                    int toRow = row + rows - 1;

                    if (pos == "all" || pos == "both" || pos == "left" || pos == "middle")
                    {
                        data.CellBorders.Add(new CellBorder(false, col, fromRow, toRow, brush, thickness));
                    }
                    if (pos == "all" || pos == "both" || pos == "right" || pos == "middle")
                    {
                        data.CellBorders.Add(new CellBorder(false, Math.Min(col + 1, Math.Max(0, width)), fromRow, toRow, brush, thickness));
                    }
                }
            }

            // ---- horizontal borders ----
            // <h-border row="1" col="0" pos="top" cols="7" />    ->  separator line `row` on the top edge of row `row`
            // <h-border row="3" col="0" pos="bottom" cols="7" /> ->  separator line `row + 1` on the bottom edge of row `row`
            //                                                (when row == height it represents the bottom edge of the table)
            XElement hBordersElement = GetChild(root, "h-borders");
            if (hBordersElement != null)
            {
                foreach (XElement hBorderElement in GetChildren(hBordersElement, "h-border"))
                {
                    int row = GetInt(hBorderElement, "row", 0);
                    int col = GetInt(hBorderElement, "col", 0);
                    int cols = Math.Max(1, GetInt(hBorderElement, "cols", 1));
                    string pos = (GetAttribute(hBorderElement, "pos") ?? "all").ToLowerInvariant();
                    IBrush brush = ParseBrush(GetAttribute(hBorderElement, "color"));
                    if (brush == null)
                    {
                        brush = Brushes.Black;
                    }
                    double thickness = GetDouble(hBorderElement, "width", 1);

                    int fromCol = col;
                    int toCol = col + cols - 1;

                    if (pos == "all" || pos == "both" || pos == "top" || pos == "middle")
                    {
                        data.CellBorders.Add(new CellBorder(true, row, fromCol, toCol, brush, thickness));
                    }
                    if (pos == "all" || pos == "both" || pos == "bottom" || pos == "middle")
                    {
                        data.CellBorders.Add(new CellBorder(true, Math.Min(row + 1, Math.Max(0, height)), fromCol, toCol, brush, thickness));
                    }
                }
            }

            // ---- cells ----
            XElement cellsElement = GetChild(root, "cells");
            if (cellsElement != null)
            {
                foreach (XElement cellElement in GetChildren(cellsElement, "cell"))
                {
                    int row = GetInt(cellElement, "row", 0);
                    int col = GetInt(cellElement, "col", 0);
                    int colspan = Math.Max(1, GetInt(cellElement, "colspan", 1));
                    int rowspan = Math.Max(1, GetInt(cellElement, "rowspan", 1));

                    if (colspan > 1 || rowspan > 1)
                    {
                        data.MergedRanges.Add(new SelectionRange(col, row, col + colspan - 1, row + rowspan - 1));
                    }

                    string text = GetCellText(cellElement);
                    if (text.Length > 0)
                    {
                        data.Data[(col, row)] = text;
                    }

                    // Style inheritance: global -> row -> column -> cell
                    RgfStyle style = globalStyle;
                    if (rowStyles.TryGetValue(row, out RgfStyle rowStyle))
                    {
                        style = MergeStyles(style, rowStyle);
                    }
                    if (colStyles.TryGetValue(col, out RgfStyle colStyle))
                    {
                        style = MergeStyles(style, colStyle);
                    }
                    RgfStyle cellStyle = ParseReoGridStyle(cellElement, null);
                    cellStyle = ParseReoGridStyle(GetChild(cellElement, "style"), cellStyle);
                    style = MergeStyles(style, cellStyle);

                    ApplyStyle(data, style, col, row);

                    if (cellStyle.Background != null)
                    {
                        data.CellBackgrounds.Add(new CellBackground(new SelectionRange(col, row, col + colspan - 1, row + rowspan - 1), cellStyle.Background));
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

                data.CellTypefaces[(col, row)] = new Typeface(family, style.FontStyle ?? FontStyle.Normal, style.FontWeight ?? FontWeight.Normal);
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

                style.FontStyle = italic != 0 ? FontStyle.Italic : (FontStyle?)FontStyle.Normal;
                style.FontWeight = bold != 0 ? FontWeight.Bold : (FontWeight?)FontWeight.Normal;
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

        /// <summary>
        /// Parses a ReoGrid style element (or a cell element carrying style attributes).
        /// Attributes present on the element override the corresponding values of <paramref name="baseStyle"/>.
        /// </summary>
        private static RgfStyle ParseReoGridStyle(XElement element, RgfStyle baseStyle)
        {
            if (element == null)
            {
                return baseStyle != null ? CloneStyle(baseStyle) : new RgfStyle();
            }

            RgfStyle style = baseStyle != null ? CloneStyle(baseStyle) : new RgfStyle();

            string bgcolor = GetAttribute(element, "bgcolor");
            if (!string.IsNullOrEmpty(bgcolor))
            {
                style.Background = ParseBrush(bgcolor);
            }

            string color = GetAttribute(element, "color");
            if (!string.IsNullOrEmpty(color))
            {
                style.Foreground = ParseBrush(color);
            }

            string font = GetAttribute(element, "font");
            if (!string.IsNullOrEmpty(font))
            {
                style.FontFamily = font;
            }

            double fontSize = GetDouble(element, "font-size", 0);
            if (fontSize > 0)
            {
                style.FontSize = fontSize;
            }

            string bold = GetAttribute(element, "bold");
            if (!string.IsNullOrEmpty(bold))
            {
                style.FontWeight = IsTrue(bold) ? FontWeight.Bold : FontWeight.Normal;
            }

            string italic = GetAttribute(element, "italic");
            if (!string.IsNullOrEmpty(italic))
            {
                style.FontStyle = IsTrue(italic) ? FontStyle.Italic : FontStyle.Normal;
            }

            string align = GetAttribute(element, "align");
            if (!string.IsNullOrEmpty(align))
            {
                switch (align.ToLowerInvariant())
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
                    case "justify":
                    case "distributed":
                        style.Horizontal = TextAlignment.Justify;
                        break;
                    // "general" leaves the alignment unset (default behaviour)
                }
            }

            string valign = GetAttribute(element, "valign");
            if (!string.IsNullOrEmpty(valign))
            {
                switch (valign.ToLowerInvariant())
                {
                    case "top":
                        style.Vertical = VerticalAlignment.Top;
                        break;
                    case "bottom":
                        style.Vertical = VerticalAlignment.Bottom;
                        break;
                    case "middle":
                    case "center":
                        style.Vertical = VerticalAlignment.Center;
                        break;
                }
            }

            return style;
        }

        private static RgfStyle CloneStyle(RgfStyle style)
        {
            return new RgfStyle()
            {
                FontFamily = style.FontFamily,
                FontSize = style.FontSize,
                FontStyle = style.FontStyle,
                FontWeight = style.FontWeight,
                Horizontal = style.Horizontal,
                Vertical = style.Vertical,
                Foreground = style.Foreground,
                Background = style.Background
            };
        }

        private static RgfStyle MergeStyles(RgfStyle baseStyle, RgfStyle overrideStyle)
        {
            if (baseStyle == null)
            {
                return overrideStyle != null ? CloneStyle(overrideStyle) : new RgfStyle();
            }
            if (overrideStyle == null)
            {
                return CloneStyle(baseStyle);
            }

            RgfStyle result = CloneStyle(baseStyle);

            if (!string.IsNullOrEmpty(overrideStyle.FontFamily))
            {
                result.FontFamily = overrideStyle.FontFamily;
            }
            if (overrideStyle.FontSize > 0)
            {
                result.FontSize = overrideStyle.FontSize;
            }
            if (overrideStyle.FontStyle.HasValue)
            {
                result.FontStyle = overrideStyle.FontStyle;
            }
            if (overrideStyle.FontWeight.HasValue)
            {
                result.FontWeight = overrideStyle.FontWeight;
            }
            if (overrideStyle.Horizontal.HasValue)
            {
                result.Horizontal = overrideStyle.Horizontal;
            }
            if (overrideStyle.Vertical.HasValue)
            {
                result.Vertical = overrideStyle.Vertical;
            }
            if (overrideStyle.Foreground != null)
            {
                result.Foreground = overrideStyle.Foreground;
            }
            if (overrideStyle.Background != null)
            {
                result.Background = overrideStyle.Background;
            }

            return result;
        }

        /// <summary>
        /// Extracts the text content of a ReoGrid cell element, ignoring any child element such as <c>&lt;style&gt;</c>.
        /// </summary>
        private static string GetCellText(XElement cellElement)
        {
            if (!cellElement.HasElements)
            {
                return (cellElement.Value ?? string.Empty).Trim();
            }

            return string.Concat(cellElement.Nodes().OfType<XText>().Select(t => t.Value)).Trim();
        }

        private static bool IsTrue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            switch (value.Trim().ToLowerInvariant())
            {
                case "true":
                case "1":
                case "yes":
                case "on":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets the first child element whose local name matches <paramref name="localName"/>,
        /// ignoring any XML namespace.
        /// </summary>
        private static XElement GetChild(XElement parent, string localName)
        {
            return parent?.Elements().FirstOrDefault(e => e.Name.LocalName == localName);
        }

        /// <summary>
        /// Gets all child elements whose local name matches <paramref name="localName"/>,
        /// ignoring any XML namespace.
        /// </summary>
        private static IEnumerable<XElement> GetChildren(XElement parent, string localName)
        {
            return parent?.Elements().Where(e => e.Name.LocalName == localName) ?? Enumerable.Empty<XElement>();
        }

        private static int GetElementInt(XElement parent, string name, int defaultValue)
        {
            XElement element = parent != null ? GetChild(parent, name) : null;
            if (element == null || string.IsNullOrEmpty(element.Value))
            {
                return defaultValue;
            }

            if (int.TryParse(element.Value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
            {
                return result;
            }

            return defaultValue;
        }

        private static double GetElementDouble(XElement parent, string name, double defaultValue)
        {
            XElement element = parent != null ? GetChild(parent, name) : null;
            if (element == null || string.IsNullOrEmpty(element.Value))
            {
                return defaultValue;
            }

            if (double.TryParse(element.Value.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            return defaultValue;
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
