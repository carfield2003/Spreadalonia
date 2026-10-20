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

using Avalonia.Layout;
using Avalonia.Media;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Spreadalonia
{
    /// <summary>
    /// Options controlling the behaviour of the xlsx import performed by
    /// <see cref="Spreadsheet.LoadXlsx(System.IO.Stream, XlsxImportOptions)"/>.
    /// </summary>
    public sealed class XlsxImportOptions
    {
        /// <summary>
        /// Whether the worksheets that are hidden in the workbook are imported as well. They are,
        /// by default: a hidden worksheet keeps its position, so that the index of a
        /// <see cref="SpreadsheetSheet"/> keeps matching the index of the worksheet it was imported
        /// from. Turning this off drops the hidden worksheets instead, which shifts the index of
        /// every worksheet that follows them.
        /// </summary>
        public bool ImportHiddenSheets { get; set; } = true;

        /// <summary>
        /// Whether the formulas of the workbook are imported as Spreadalonia formulas (the text of
        /// a formula cell becomes e.g. <c>=SUM(A1:A3)</c> and is evaluated by the formula engine of
        /// the worksheet). When this is <see langword="false"/> the last value Excel cached for the
        /// cell is imported instead. <see langword="true"/> by default.
        /// </summary>
        public bool ImportFormulas { get; set; } = true;

        /// <summary>
        /// Whether the cell styles of the workbook are imported (bold / italic / font name, font
        /// size, font colour, background fill and text alignment). <see langword="true"/> by
        /// default.
        /// </summary>
        public bool ImportStyles { get; set; } = true;

        /// <summary>
        /// Whether the merged ranges of the workbook are imported. <see langword="true"/> by
        /// default.
        /// </summary>
        public bool ImportMergedRanges { get; set; } = true;

        /// <summary>
        /// Whether the column widths of the workbook are imported. <see langword="true"/> by
        /// default.
        /// </summary>
        public bool ImportColumnWidths { get; set; } = true;

        /// <summary>
        /// Whether the row heights of the workbook are imported. <see langword="true"/> by default.
        /// </summary>
        public bool ImportRowHeights { get; set; } = true;

        /// <summary>
        /// Whether the rows and the columns that are hidden in the workbook are hidden in the
        /// imported worksheets too. <see langword="true"/> by default.
        /// </summary>
        public bool ImportHiddenRowsColumns { get; set; } = true;

        /// <summary>
        /// Whether the worksheets the spreadsheet already contains are dropped before the import.
        /// <see langword="true"/> by default, which makes the imported collection match the file
        /// exactly, index by index.
        /// </summary>
        public bool ClearExistingSheets { get; set; } = true;

        /// <summary>
        /// The maximum number of rows imported from each worksheet, as a safeguard against huge
        /// workbooks. 0 (the default) means no limit.
        /// </summary>
        public int MaxRowsPerSheet { get; set; } = 0;

        /// <summary>
        /// The maximum number of columns imported from each worksheet. 0 (the default) means no
        /// limit.
        /// </summary>
        public int MaxColumnsPerSheet { get; set; } = 0;
    }

    /// <summary>
    /// The content of a worksheet of an xlsx workbook, read by <see cref="XlsxImporter"/> and
    /// applied to a <see cref="SpreadsheetSheet"/> by the <see cref="Spreadsheet"/> that owns it.
    /// </summary>
    internal sealed class XlsxImportedSheet
    {
        /// <summary>
        /// The name of the worksheet.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Whether the worksheet is hidden in the workbook.
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// The cell values, indexed by (column, row).
        /// </summary>
        public Dictionary<(int, int), string> Data { get; } = new Dictionary<(int, int), string>();

        /// <summary>
        /// The width of the columns whose width differs from the default one.
        /// </summary>
        public Dictionary<int, double> ColumnWidths { get; } = new Dictionary<int, double>();

        /// <summary>
        /// The height of the rows whose height differs from the default one.
        /// </summary>
        public Dictionary<int, double> RowHeights { get; } = new Dictionary<int, double>();

        /// <summary>
        /// The merged ranges of the worksheet.
        /// </summary>
        public List<SelectionRange> MergedRanges { get; } = new List<SelectionRange>();

        /// <summary>
        /// The rows that are hidden in the workbook.
        /// </summary>
        public HashSet<int> HiddenRows { get; } = new HashSet<int>();

        /// <summary>
        /// The columns that are hidden in the workbook.
        /// </summary>
        public HashSet<int> HiddenColumns { get; } = new HashSet<int>();

        /// <summary>
        /// The typeface of the cells whose font differs from the default one.
        /// </summary>
        public Dictionary<(int, int), Typeface> CellTypefaces { get; } = new Dictionary<(int, int), Typeface>();

        /// <summary>
        /// The font size of the cells whose font size differs from the default one, in DIP.
        /// </summary>
        public Dictionary<(int, int), double> CellFontSize { get; } = new Dictionary<(int, int), double>();

        /// <summary>
        /// The foreground brush of the cells whose font colour differs from the default one.
        /// </summary>
        public Dictionary<(int, int), IBrush> CellForeground { get; } = new Dictionary<(int, int), IBrush>();

        /// <summary>
        /// The horizontal text alignment of the cells whose alignment differs from the default one.
        /// </summary>
        public Dictionary<(int, int), TextAlignment> CellTextAlignment { get; } = new Dictionary<(int, int), TextAlignment>();

        /// <summary>
        /// The vertical text alignment of the cells whose alignment differs from the default one.
        /// </summary>
        public Dictionary<(int, int), VerticalAlignment> CellVerticalAlignment { get; } = new Dictionary<(int, int), VerticalAlignment>();

        /// <summary>
        /// The solid background fills of the cells.
        /// </summary>
        public List<CellBackground> CellBackgrounds { get; } = new List<CellBackground>();

        /// <summary>
        /// The number of columns the worksheet has content in (after the
        /// <see cref="XlsxImportOptions.MaxColumnsPerSheet"/> truncation).
        /// </summary>
        public int ColumnCount { get; set; }

        /// <summary>
        /// The number of rows the worksheet has content in (after the
        /// <see cref="XlsxImportOptions.MaxRowsPerSheet"/> truncation).
        /// </summary>
        public int RowCount { get; set; }
    }

    /// <summary>
    /// Reads the worksheets of an Excel .xlsx workbook using ClosedXML and turns each of them into
    /// the state of a <see cref="SpreadsheetSheet"/>.
    /// <para>
    /// Mapping rules: a worksheet becomes one <see cref="SpreadsheetSheet"/> with the same name,
    /// index and visibility; numbers, dates, booleans, text and (optionally) formulas are imported
    /// as cell text, so that formulas are re-evaluated by the formula engine of the worksheet;
    /// column widths and row heights are converted from Excel characters / points to DIP.
    /// </para>
    /// </summary>
    internal static class XlsxImporter
    {
        // The default geometry of an Excel worksheet, used to tell an explicitly sized column or
        // row from one that simply has the default size.
        private const double DefaultColumnWidthChars = 8.43;
        private const double DefaultRowHeightPoints = 15.0;

        /// <summary>
        /// Reads every worksheet of the workbook contained in the specified stream.
        /// </summary>
        /// <param name="stream">A stream containing an .xlsx document.</param>
        /// <param name="options">The options controlling the import, or <see langword="null"/> to use the defaults.</param>
        /// <returns>One entry per imported worksheet, in the order of the workbook.</returns>
        public static List<XlsxImportedSheet> Read(Stream stream, XlsxImportOptions options)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            XlsxImportOptions importOptions = options ?? new XlsxImportOptions();
            List<XlsxImportedSheet> sheets = new List<XlsxImportedSheet>();

            using (XLWorkbook workbook = new XLWorkbook(stream))
            {
                foreach (IXLWorksheet worksheet in workbook.Worksheets)
                {
                    bool isHidden = worksheet.Visibility != XLWorksheetVisibility.Visible;

                    if (isHidden && !importOptions.ImportHiddenSheets)
                    {
                        continue;
                    }

                    sheets.Add(ReadWorksheet(worksheet, importOptions, isHidden));
                }
            }

            return sheets;
        }

        /// <summary>
        /// Reads the content of a single worksheet of the workbook.
        /// </summary>
        /// <param name="worksheet">The worksheet to read.</param>
        /// <param name="options">The options controlling the import.</param>
        /// <param name="isHidden">Whether the worksheet is hidden in the workbook.</param>
        private static XlsxImportedSheet ReadWorksheet(IXLWorksheet worksheet, XlsxImportOptions options, bool isHidden)
        {
            XlsxImportedSheet sheet = new XlsxImportedSheet { Name = worksheet.Name, IsHidden = isHidden };

            IXLRange usedRange = worksheet.RangeUsed(XLCellsUsedOptions.Contents);

            if (usedRange == null)
            {
                return sheet;
            }

            int lastColumn = usedRange.LastColumn().ColumnNumber();
            int lastRow = usedRange.LastRow().RowNumber();

            int maxColumns = options.MaxColumnsPerSheet > 0 ? Math.Min(lastColumn, options.MaxColumnsPerSheet) : lastColumn;
            int maxRows = options.MaxRowsPerSheet > 0 ? Math.Min(lastRow, options.MaxRowsPerSheet) : lastRow;

            if (maxColumns <= 0 || maxRows <= 0)
            {
                return sheet;
            }

            sheet.ColumnCount = maxColumns;
            sheet.RowCount = maxRows;

            for (int c = 1; c <= maxColumns; c++)
            {
                IXLColumn column = worksheet.Column(c);

                if (options.ImportColumnWidths && Math.Abs(column.Width - DefaultColumnWidthChars) > 0.01)
                {
                    // Excel character widths -> DIP (the inverse of what the exporter does).
                    sheet.ColumnWidths[c - 1] = Math.Max(1, column.Width * 7.0 + 5.0);
                }

                if (options.ImportHiddenRowsColumns && column.IsHidden)
                {
                    sheet.HiddenColumns.Add(c - 1);
                }
            }

            for (int r = 1; r <= maxRows; r++)
            {
                IXLRow row = worksheet.Row(r);

                if (options.ImportRowHeights && Math.Abs(row.Height - DefaultRowHeightPoints) > 0.01)
                {
                    // Excel points -> DIP.
                    sheet.RowHeights[r - 1] = Math.Max(1, row.Height / 0.75);
                }

                if (options.ImportHiddenRowsColumns && row.IsHidden)
                {
                    sheet.HiddenRows.Add(r - 1);
                }
            }

            if (options.ImportMergedRanges)
            {
                foreach (IXLRange mergedRange in worksheet.MergedRanges)
                {
                    int left = mergedRange.FirstColumn().ColumnNumber() - 1;
                    int top = mergedRange.FirstRow().RowNumber() - 1;

                    if (left >= maxColumns || top >= maxRows)
                    {
                        continue;
                    }

                    int right = Math.Min(mergedRange.LastColumn().ColumnNumber() - 1, maxColumns - 1);
                    int bottom = Math.Min(mergedRange.LastRow().RowNumber() - 1, maxRows - 1);

                    sheet.MergedRanges.Add(new SelectionRange(left, top, right, bottom));
                }
            }

            IXLStyle defaultStyle = options.ImportStyles ? worksheet.Style : null;

            foreach (IXLCell cell in usedRange.CellsUsed(XLCellsUsedOptions.Contents))
            {
                int column = cell.Address.ColumnNumber - 1;
                int row = cell.Address.RowNumber - 1;

                if (column >= maxColumns || row >= maxRows)
                {
                    continue;
                }

                string text = GetCellText(cell, options);

                if (!string.IsNullOrEmpty(text))
                {
                    sheet.Data[(column, row)] = text;
                }

                if (options.ImportStyles && defaultStyle != null)
                {
                    ImportCellStyle(cell, defaultStyle, sheet, column, row);
                }
            }

            return sheet;
        }

        /// <summary>
        /// Returns the text a cell is imported with: its formula when formulas are imported and the
        /// cell has one, or its value converted to an invariant string otherwise.
        /// </summary>
        /// <param name="cell">The cell to read.</param>
        /// <param name="options">The options controlling the import.</param>
        private static string GetCellText(IXLCell cell, XlsxImportOptions options)
        {
            if (options.ImportFormulas && cell.HasFormula)
            {
                string formula = cell.FormulaA1;

                if (!string.IsNullOrEmpty(formula))
                {
                    return "=" + formula;
                }
            }

            switch (cell.DataType)
            {
                case XLDataType.Number:
                    return cell.GetValue<double>().ToString("0.############", CultureInfo.InvariantCulture);

                case XLDataType.DateTime:
                {
                    DateTime date = cell.GetValue<DateTime>();
                    string format = date.TimeOfDay == TimeSpan.Zero ? "yyyy-MM-dd" : "yyyy-MM-dd HH:mm:ss";
                    return date.ToString(format, CultureInfo.InvariantCulture);
                }

                case XLDataType.Boolean:
                    return cell.GetValue<bool>() ? "TRUE" : "FALSE";

                default:
                    return cell.GetString();
            }
        }

        /// <summary>
        /// Imports the style of a cell, but only the attributes that differ from the default style
        /// of the worksheet, so that a workbook that uses plain formatting costs nothing.
        /// </summary>
        /// <param name="cell">The cell to read.</param>
        /// <param name="defaultStyle">The default style of the worksheet.</param>
        /// <param name="sheet">The imported state the style is written to.</param>
        /// <param name="column">The zero-based column of the cell.</param>
        /// <param name="row">The zero-based row of the cell.</param>
        private static void ImportCellStyle(IXLCell cell, IXLStyle defaultStyle, XlsxImportedSheet sheet, int column, int row)
        {
            IXLStyle style = cell.Style;

            bool bold = style.Font.Bold;
            bool italic = style.Font.Italic;
            string fontName = style.Font.FontName;
            bool customFont = !string.IsNullOrEmpty(fontName) && !string.Equals(fontName, defaultStyle.Font.FontName, StringComparison.OrdinalIgnoreCase);

            if (bold != defaultStyle.Font.Bold || italic != defaultStyle.Font.Italic || customFont)
            {
                FontFamily family = customFont ? new FontFamily(fontName) : FontFamily.Default;

                sheet.CellTypefaces[(column, row)] = new Typeface(family, italic ? FontStyle.Italic : FontStyle.Normal, bold ? FontWeight.Bold : FontWeight.Normal);
            }

            double fontSize = style.Font.FontSize;

            if (fontSize > 0 && Math.Abs(fontSize - defaultStyle.Font.FontSize) > 0.01)
            {
                // Excel points -> DIP.
                sheet.CellFontSize[(column, row)] = fontSize / 0.75;
            }

            Avalonia.Media.Color? fontColor = ToAvaloniaColor(style.Font.FontColor);
            Avalonia.Media.Color? defaultFontColor = ToAvaloniaColor(defaultStyle.Font.FontColor);

            if (fontColor.HasValue && (!defaultFontColor.HasValue || fontColor.Value != defaultFontColor.Value))
            {
                sheet.CellForeground[(column, row)] = new SolidColorBrush(fontColor.Value);
            }

            if (style.Fill.PatternType == XLFillPatternValues.Solid)
            {
                Avalonia.Media.Color? background = ToAvaloniaColor(style.Fill.BackgroundColor);

                // The workbook default fill is an opaque white one: importing it would add a
                // background to every single cell without changing anything on screen.
                if (background.HasValue && background.Value.A > 0 && !(background.Value.R == 255 && background.Value.G == 255 && background.Value.B == 255))
                {
                    sheet.CellBackgrounds.Add(new CellBackground(new SelectionRange(column, row), new SolidColorBrush(background.Value)));
                }
            }

            TextAlignment? horizontal = MapHorizontal(style.Alignment.Horizontal);

            if (horizontal.HasValue && style.Alignment.Horizontal != defaultStyle.Alignment.Horizontal)
            {
                sheet.CellTextAlignment[(column, row)] = horizontal.Value;
            }

            VerticalAlignment? vertical = MapVertical(style.Alignment.Vertical);

            if (vertical.HasValue && style.Alignment.Vertical != defaultStyle.Alignment.Vertical)
            {
                sheet.CellVerticalAlignment[(column, row)] = vertical.Value;
            }
        }

        /// <summary>
        /// Converts a ClosedXML colour into an Avalonia colour, or returns <see langword="null"/>
        /// when the colour is a theme / indexed one that carries no explicit ARGB value.
        /// </summary>
        /// <param name="color">The colour to convert.</param>
        private static Avalonia.Media.Color? ToAvaloniaColor(XLColor color)
        {
            try
            {
                System.Drawing.Color drawingColor = color.Color;
                return Avalonia.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Maps an Excel horizontal alignment onto an Avalonia one, or returns <see langword="null"/>
        /// for the alignments that have no Avalonia counterpart.
        /// </summary>
        /// <param name="alignment">The Excel alignment.</param>
        private static TextAlignment? MapHorizontal(XLAlignmentHorizontalValues alignment)
        {
            switch (alignment)
            {
                case XLAlignmentHorizontalValues.Left:
                    return TextAlignment.Left;
                case XLAlignmentHorizontalValues.Center:
                case XLAlignmentHorizontalValues.CenterContinuous:
                    return TextAlignment.Center;
                case XLAlignmentHorizontalValues.Right:
                    return TextAlignment.Right;
                case XLAlignmentHorizontalValues.Justify:
                    return TextAlignment.Justify;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Maps an Excel vertical alignment onto an Avalonia one, or returns <see langword="null"/>
        /// for the alignments that have no Avalonia counterpart.
        /// </summary>
        /// <param name="alignment">The Excel alignment.</param>
        private static VerticalAlignment? MapVertical(XLAlignmentVerticalValues alignment)
        {
            switch (alignment)
            {
                case XLAlignmentVerticalValues.Top:
                    return VerticalAlignment.Top;
                case XLAlignmentVerticalValues.Center:
                    return VerticalAlignment.Center;
                case XLAlignmentVerticalValues.Bottom:
                    return VerticalAlignment.Bottom;
                case XLAlignmentVerticalValues.Justify:
                    return VerticalAlignment.Stretch;
                default:
                    return null;
            }
        }
    }
}
