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
using Spreadalonia.Formula;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Spreadalonia
{
    /// <summary>
    /// Options controlling the behaviour of the xlsx export performed by
    /// <see cref="Spreadsheet.SaveXlsx(System.IO.Stream, XlsxExportOptions)"/>.
    /// </summary>
    public sealed class XlsxExportOptions
    {
        /// <summary>
        /// Whether formulas that are compatible with Excel are exported as real formulas.
        /// <see langword="true"/> by default.
        /// </summary>
        public bool ExportFormulas { get; set; } = true;

        /// <summary>
        /// Whether <see cref="LinkCell"/>s whose <see cref="LinkCell.LinkPara"/> is an absolute
        /// URI are also exported as Excel hyperlinks (the cell still contains the formatted
        /// display text). <see langword="false"/> by default: link cells are exported as plain
        /// text with their link styling.
        /// </summary>
        public bool ExportLinkAsHyperlink { get; set; } = false;

        /// <summary>
        /// An optional set of additional function names (case-insensitive) that are considered
        /// Excel-compatible during formula translation. This is useful when formulas use newer
        /// Excel functions that are not part of the built-in list.
        /// </summary>
        public ISet<string> AdditionalExcelFunctions { get; set; } = null;

        /// <summary>
        /// Whether rows that are collapsed in the control (<see cref="Table.HiddenRows"/>) are
        /// written to the worksheet as normal, visible rows. <see langword="false"/> by default:
        /// collapsed rows are hidden in Excel and their content is not written, matching what the
        /// user sees on screen.
        /// </summary>
        public bool ExportHiddenRows { get; set; } = false;

        /// <summary>
        /// Whether columns that are hidden in the control (<see cref="Table.HiddenColumns"/>) are
        /// written to the worksheet as normal, visible columns. <see langword="false"/> by default:
        /// hidden columns are hidden in Excel and their content is not written, matching what the
        /// user sees on screen.
        /// </summary>
        public bool ExportHiddenColumns { get; set; } = false;

        /// <summary>
        /// An optional callback invoked with a human readable warning when a cell is exported
        /// with its cached value instead of its formula (e.g. because it uses a custom function
        /// that Excel does not know, or a single-quoted string literal).
        /// </summary>
        public Action<string> WarningHandler { get; set; } = null;
    }

    /// <summary>
    /// Exports the content of a <see cref="Table"/> to an Excel .xlsx stream using ClosedXML.
    /// <para>
    /// Mapping rules:
    /// - <see cref="LinkCell"/>s are exported as plain text cells keeping their formatted
    ///   display text and colours (optionally as hyperlinks).
    /// - <see cref="TitleCell"/>s are exported as multi-line rich text (each chunk becomes a
    ///   line, with its own font/colour) so that the cell stays editable in Excel.
    /// - Formulas are kept when they only use Excel-compatible functions (AVG is translated to
    ///   AVERAGE); otherwise the cached value is exported so the file never contains #NAME?.
    /// </para>
    /// </summary>
    internal static class XlsxExporter
    {
        // Upper bound of Excel worksheets (the exporter clamps anything beyond this).
        private const int ExcelMaxColumns = 16384;
        private const int ExcelMaxRows = 1048576;

        private static readonly HashSet<string> ExcelFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Spreadalonia built-ins (AVG is intentionally absent; it is translated to AVERAGE).
            "SUM", "AVERAGE", "AVERAGEA", "COUNT", "COUNTA", "MAX", "MAXA", "MIN", "MINA",
            "ABS", "ROUND", "ROUNDUP", "ROUNDDOWN", "SQRT", "POWER", "IF", "AND", "OR", "NOT",
            "CONCAT", "CONCATENATE", "UPPER", "LOWER", "TRIM", "LEN", "LEFT", "RIGHT", "MID",
            "VLOOKUP", "HLOOKUP", "ISBLANK", "ISNUMBER", "ISTEXT",

            // Math & trigonometry
            "ACOS", "ACOSH", "ACOT", "ACOTH", "ASIN", "ASINH", "ATAN", "ATAN2", "ATANH",
            "CEILING", "CEILING.MATH", "CEILING.PRECISE", "COMBIN", "COMBINA", "COS", "COSH",
            "COT", "COTH", "CSC", "CSCH", "DEGREES", "EVEN", "EXP", "FACT", "FACTDOUBLE",
            "FLOOR", "FLOOR.MATH", "FLOOR.PRECISE", "GCD", "INT", "ISO.CEILING", "LCM",
            "LN", "LOG", "LOG10", "MDETERM", "MOD", "MROUND", "MULTINOMIAL", "ODD", "PI",
            "PRODUCT", "QUOTIENT", "RADIANS", "RAND", "RANDBETWEEN", "ROMAN", "SEC", "SECH",
            "SERIESSUM", "SIGN", "SIN", "SINH", "SQRTPI", "SUBTOTAL", "SUMIF", "SUMIFS",
            "SUMPRODUCT", "SUMSQ", "TAN", "TANH", "TRUNC", "AGGREGATE",

            // Statistical
            "AVEDEV", "AVERAGEIF", "AVERAGEIFS", "BETADIST", "BETA.DIST", "BETAINV",
            "BETA.INV", "BINOMDIST", "BINOM.DIST", "BINOM.DIST.RANGE", "BINOM.INV", "CHIDIST",
            "CHIINV", "CHISQ.DIST", "CHISQ.DIST.RT", "CHISQ.INV", "CHISQ.INV.RT", "CHISQ.TEST",
            "CONFIDENCE", "CONFIDENCE.NORM", "CONFIDENCE.T", "CORREL", "COUNTBLANK", "COUNTIF",
            "COUNTIFS", "COVAR", "COVARIANCE.P", "COVARIANCE.S", "CRITBINOM", "EXPON.DIST",
            "EXPONDIST", "F.DIST", "F.DIST.RT", "F.INV", "F.INV.RT", "FDIST", "FINV", "FISHER",
            "FISHERINV", "FORECAST", "FORECAST.LINEAR", "FREQUENCY", "FTEST", "F.TEST", "GAMMA",
            "GAMMA.DIST", "GAMMA.INV", "GAMMADIST", "GAMMAINV", "GAMMALN", "GAUSS", "GEOMEAN",
            "HARMEAN", "HYPGEOM.DIST", "HYPGEOMDIST", "INTERCEPT", "KURT", "LARGE", "LINEST",
            "LOGEST", "LOGINV", "LOGNORM.DIST", "LOGNORMDIST", "LOGNORM.INV", "MAXIFS", "MEDIAN",
            "MINIFS", "MODE", "MODE.MULT", "MODE.SNGL", "NEGBINOM.DIST", "NEGBINOMDIST",
            "NORM.DIST", "NORM.INV", "NORM.S.DIST", "NORM.S.INV", "NORMDIST", "NORMINV",
            "NORMSDIST", "NORMSINV", "PEARSON", "PERCENTILE", "PERCENTILE.EXC",
            "PERCENTILE.INC", "PERCENTRANK", "PERCENTRANK.EXC", "PERCENTRANK.INC", "PERMUT",
            "PERMUTATIONA", "PHI", "POISSON", "POISSON.DIST", "PROB", "QUARTILE",
            "QUARTILE.EXC", "QUARTILE.INC", "RANK", "RANK.AVG", "RANK.EQ", "RSQ", "SKEW",
            "SKEW.P", "SLOPE", "SMALL", "STANDARDIZE", "STDEV", "STDEV.P", "STDEV.S",
            "STDEVA", "STDEVP", "STDEVPA", "STEYX", "T.DIST", "T.DIST.2T", "T.DIST.RT",
            "T.INV", "T.INV.2T", "TDIST", "TINV", "T.TEST", "TTEST", "TREND", "TRIMMEAN",
            "VAR", "VAR.P", "VAR.S", "VARA", "VARP", "VARPA", "WEIBULL", "WEIBULL.DIST",
            "Z.TEST", "ZTEST",

            // Financial
            "ACCRINT", "ACCRINTM", "AMORDEGRC", "AMORLINC", "COUPDAYBS", "COUPDAYS",
            "COUPDAYSNC", "COUPNCD", "COUPNUM", "COUPPCD", "CUMIPMT", "CUMPRINC", "DB", "DDB",
            "DISC", "DOLLARDE", "DOLLARFR", "DURATION", "EFFECT", "FV", "FVSCHEDULE", "INTRATE",
            "IPMT", "IRR", "ISPMT", "MDURATION", "MIRR", "NOMINAL", "NPER", "NPV", "ODDFPRICE",
            "ODDFYIELD", "ODDLPRICE", "ODDLYIELD", "PDURATION", "PMT", "PPMT", "PRICE",
            "PRICEDISC", "PRICEMAT", "PV", "RATE", "RECEIVED", "RRI", "SLN", "SYD", "TBILLEQ",
            "TBILLPRICE", "TBILLYIELD", "VDB", "XIRR", "XNPV", "YIELD", "YIELDDISC", "YIELDMAT",

            // Date & time
            "DATE", "DATEVALUE", "DAY", "DAYS", "DAYS360", "EDATE", "EOMONTH", "HOUR", "ISOWEEKNUM",
            "MINUTE", "MONTH", "NETWORKDAYS", "NETWORKDAYS.INTL", "NOW", "SECOND", "TIME",
            "TIMEVALUE", "TODAY", "WEEKDAY", "WEEKNUM", "WORKDAY", "WORKDAY.INTL", "YEAR", "YEARFRAC",

            // Text
            "ARABIC", "ASC", "BAHTTEXT", "CHAR", "CLEAN", "CODE", "DOLLAR", "EXACT", "FIND",
            "FIXED", "NUMBERVALUE", "PHONETIC", "PROPER", "REPLACE", "REPT", "SEARCH",
            "SUBSTITUTE", "T", "TEXT", "TEXTJOIN", "TRIM", "UNICHAR", "UNICODE", "VALUE",

            // Logical & information
            "ERROR.TYPE", "FALSE", "IFERROR", "IFNA", "IFS", "INFO", "ISERR", "ISERROR",
            "ISEVEN", "ISFORMULA", "ISLOGICAL", "ISNA", "ISNONTEXT", "ISODD", "ISREF",
            "ISTEXT", "NA", "SHEET", "SHEETS", "SWITCH", "TRUE", "TYPE", "XOR",

            // Lookup & reference
            "ADDRESS", "AREAS", "CHOOSE", "COLUMN", "COLUMNS", "FORMULATEXT", "GETPIVOTDATA",
            "HYPERLINK", "INDEX", "INDIRECT", "LOOKUP", "MATCH", "OFFSET", "ROW", "ROWS",
            "TRANSPOSE",

            // Dynamic array (Excel 365)
            "CHOOSECOLS", "CHOOSEROWS", "DROP", "EXPAND", "FILTER", "HSTACK", "LET",
            "MAKEARRAY", "MAP", "RANDARRAY", "REDUCE", "SCAN", "SEQUENCE", "SINGLE", "SORT",
            "SORTBY", "TAKE", "TEXTAFTER", "TEXTBEFORE", "TEXTSPLIT", "TOCOL", "TOROW",
            "UNIQUE", "VSTACK", "WRAPCOLS", "WRAPROWS", "XLOOKUP", "XMATCH",
        };

        public static void Export(Table table, Stream stream, XlsxExportOptions options)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            options = options ?? new XlsxExportOptions();

            HashSet<string> allowedFunctions = new HashSet<string>(ExcelFunctions, StringComparer.OrdinalIgnoreCase);
            if (options.AdditionalExcelFunctions != null)
            {
                foreach (string extra in options.AdditionalExcelFunctions)
                {
                    if (!string.IsNullOrEmpty(extra))
                    {
                        allowedFunctions.Add(extra);
                    }
                }
            }

            using (XLWorkbook workbook = new XLWorkbook())
            {
                IXLWorksheet sheet = workbook.Worksheets.Add("Sheet1");

                // Determine the used bounds.
                (int maxCol, int maxRow) = ComputeBounds(table);
                if (maxCol < 0 || maxRow < 0)
                {
                    // Empty table: still produce a valid workbook.
                    workbook.SaveAs(stream);
                    return;
                }

                maxCol = Math.Min(maxCol, ExcelMaxColumns - 1);
                maxRow = Math.Min(maxRow, ExcelMaxRows - 1);

                // Column widths (Spreadalonia DIP pixels -> Excel character width).
                for (int c = 0; c <= maxCol; c++)
                {
                    double px = table.GetWidth(c);
                    double chars = Math.Max(0, (px - 5) / 7.0);
                    if (chars > 0)
                    {
                        sheet.Column(c + 1).Width = chars;
                    }
                }

                // Row heights (DIP pixels -> Excel points) and collapsed rows / hidden columns.
                bool exportHidden = options != null && options.ExportHiddenRows;
                bool exportHiddenColumns = options != null && options.ExportHiddenColumns;

                for (int c = 0; c <= maxCol; c++)
                {
                    if (table.IsColumnHidden(c))
                    {
                        sheet.Column(c + 1).Hide();
                    }
                }

                for (int r = 0; r <= maxRow; r++)
                {
                    if (table.IsRowHidden(r))
                    {
                        sheet.Row(r + 1).Hide();
                    }

                    double pt = table.GetHeight(r) * 0.75;
                    if (pt > 0)
                    {
                        sheet.Row(r + 1).Height = pt;
                    }
                }

                // Cells that are covered by a merged range but are not its top-left cell.
                HashSet<(int, int)> mergedInnerCells = new HashSet<(int, int)>();
                foreach (SelectionRange mergeRange in table.MergedRanges)
                {
                    int right = Math.Min(mergeRange.Right, maxCol);
                    int bottom = Math.Min(mergeRange.Bottom, maxRow);
                    for (int r = mergeRange.Top; r <= bottom; r++)
                    {
                        for (int c = mergeRange.Left; c <= right; c++)
                        {
                            if (!(c == mergeRange.Left && r == mergeRange.Top))
                            {
                                mergedInnerCells.Add((c, r));
                            }
                        }
                    }
                }

                Typeface defaultTypeface = new Typeface(table.FontFamily, table.FontStyle, table.FontWeight);
                XLColor defaultForeground = BrushToXlColor(table.Foreground) ?? XLColor.Black;

                // Write cell content.
                for (int r = 0; r <= maxRow; r++)
                {
                    // Collapsed rows are not written unless ExportHiddenRows is set.
                    if (!exportHidden && table.IsRowHidden(r))
                    {
                        continue;
                    }

                    for (int c = 0; c <= maxCol; c++)
                    {
                        // Hidden columns are not written unless ExportHiddenColumns is set.
                        if (!exportHiddenColumns && table.IsColumnHidden(c))
                        {
                            continue;
                        }

                        if (mergedInnerCells.Contains((c, r)))
                        {
                            continue;
                        }

                        IXLCell cell = sheet.Cell(r + 1, c + 1);

                        // 1. Title cells -> rich text (one line per chunk).
                        if (table.TitleCells != null && table.TitleCells.TryGetValue((c, r), out TitleCell titleCell))
                        {
                            WriteTitleCell(cell, titleCell, table, c, r);
                            continue;
                        }

                        // 2. Link cells -> plain text with link styling (optionally hyperlink).
                        if (table.LinkCells != null && table.LinkCells.TryGetValue((c, r), out LinkCell linkCell))
                        {
                            WriteLinkCell(cell, linkCell, table, c, r, options);
                            continue;
                        }

                        // 3. Formula cells.
                        if (table.FormulaCells != null && table.FormulaCells.TryGetValue((c, r), out CellData formulaCell))
                        {
                            WriteFormulaCell(cell, formulaCell, table, c, r, allowedFunctions, options);
                            continue;
                        }

                        // 4. Plain data cells (numeric cells are written as real Excel numbers).
                        if (table.Data != null && table.Data.TryGetValue((c, r), out string rawText) && !string.IsNullOrEmpty(rawText))
                        {
                            NumericCell numericCell = null;
                            bool isNumeric = table.NumericCells != null && table.NumericCells.TryGetValue((c, r), out numericCell) && numericCell != null;

                            if (isNumeric && NumberFormatter.TryParse(rawText, out decimal numericValue))
                            {
                                cell.Value = numericValue;
                                cell.Style.NumberFormat.Format = numericCell.Digits > 0
                                    ? "#,##0." + new string('0', numericCell.Digits)
                                    : "#,##0";
                                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                                if (numericCell.NegativeRed && numericValue < 0)
                                {
                                    cell.Style.Font.FontColor = XLColor.Red;
                                }

                                if (numericCell.Strikethrough)
                                {
                                    cell.Style.Font.Strikethrough = true;
                                    cell.Style.Font.FontColor = XLColor.Red;
                                }
                            }
                            else
                            {
                                WriteValueCell(cell, rawText, table, c, r, defaultTypeface, defaultForeground);
                            }
                        }
                    }
                }

                // Merge the ranges (performed after writing content so merged content is preserved).
                foreach (SelectionRange mergeRange in table.MergedRanges)
                {
                    int right = Math.Min(mergeRange.Right, maxCol);
                    int bottom = Math.Min(mergeRange.Bottom, maxRow);
                    if (mergeRange.Left < 0 || mergeRange.Top < 0 || right < mergeRange.Left || bottom < mergeRange.Top)
                    {
                        continue;
                    }

                    if (right > mergeRange.Left || bottom > mergeRange.Top)
                    {
                        sheet.Range(mergeRange.Top + 1, mergeRange.Left + 1, bottom + 1, right + 1).Merge();
                    }
                }

                // Backgrounds.
                if (table.CellBackgrounds != null)
                {
                    foreach (CellBackground background in table.CellBackgrounds)
                    {
                        if (background == null || background.Brush == null || background.Range == null)
                        {
                            continue;
                        }

                        XLColor bgColor = BrushToXlColor(background.Brush);
                        if (bgColor == null)
                        {
                            continue;
                        }

                        int right = Math.Min(background.Range.Right, maxCol);
                        int bottom = Math.Min(background.Range.Bottom, maxRow);
                        for (int r = Math.Max(0, background.Range.Top); r <= bottom; r++)
                        {
                            for (int c = Math.Max(0, background.Range.Left); c <= right; c++)
                            {
                                IXLStyle style = sheet.Cell(r + 1, c + 1).Style;
                                style.Fill.BackgroundColor = bgColor;
                                style.Fill.PatternType = XLFillPatternValues.Solid;
                            }
                        }
                    }
                }

                // Borders (v/h line segment model -> per-cell edges).
                if (table.CellBorders != null)
                {
                    ApplyBorders(sheet, table.CellBorders, maxCol, maxRow);
                }

                workbook.SaveAs(stream);
            }
        }

        private static (int, int) ComputeBounds(Table table)
        {
            int maxCol = -1;
            int maxRow = -1;

            void Consider(int c, int r)
            {
                if (c > maxCol) { maxCol = c; }
                if (r > maxRow) { maxRow = r; }
            }

            if (table.Data != null)
            {
                foreach ((int col, int row) in table.Data.Keys) { Consider(col, row); }
            }
            if (table.FormulaCells != null)
            {
                foreach ((int col, int row) in table.FormulaCells.Keys) { Consider(col, row); }
            }
            if (table.TitleCells != null)
            {
                foreach ((int col, int row) in table.TitleCells.Keys) { Consider(col, row); }
            }
            if (table.LinkCells != null)
            {
                foreach ((int col, int row) in table.LinkCells.Keys) { Consider(col, row); }
            }
            if (table.MergedRanges != null)
            {
                foreach (SelectionRange range in table.MergedRanges)
                {
                    if (range != null)
                    {
                        Consider(range.Right, range.Bottom);
                    }
                }
            }
            if (table.CellBackgrounds != null)
            {
                foreach (CellBackground background in table.CellBackgrounds)
                {
                    if (background != null && background.Range != null)
                    {
                        Consider(background.Range.Right, background.Range.Bottom);
                    }
                }
            }
            if (table.CellBorders != null)
            {
                foreach (CellBorder border in table.CellBorders)
                {
                    if (border == null)
                    {
                        continue;
                    }

                    if (!border.IsHorizontal)
                    {
                        // Vertical line: sits to the left of column LineIndex (LineIndex 0 = left of column 0).
                        Consider(Math.Max(0, border.LineIndex - 1), border.To);
                        if (border.LineIndex == 0)
                        {
                            Consider(0, border.To);
                        }
                    }
                    else
                    {
                        // Horizontal line: sits above row LineIndex (LineIndex 0 = above row 0).
                        Consider(border.To, Math.Max(0, border.LineIndex - 1));
                        if (border.LineIndex == 0)
                        {
                            Consider(border.To, 0);
                        }
                    }
                }
            }

            return (maxCol, maxRow);
        }

        private static void WriteTitleCell(IXLCell cell, TitleCell titleCell, Table table, int c, int r)
        {
            if (titleCell == null || titleCell.Contents == null || titleCell.Contents.Count == 0)
            {
                return;
            }

            List<TitleCell.Chunk> ordered = new List<TitleCell.Chunk>();
            foreach (TitleCell.Chunk chunk in titleCell.Contents)
            {
                if (chunk != null && !string.IsNullOrEmpty(chunk.Text))
                {
                    ordered.Add(chunk);
                }
            }

            if (ordered.Count == 0)
            {
                return;
            }

            ordered.Sort((a, b) => a.Top.CompareTo(b.Top));

            // Replaces the cell value with a new empty rich text object.
            IXLRichText richText = cell.CreateRichText();

            for (int i = 0; i < ordered.Count; i++)
            {
                TitleCell.Chunk chunk = ordered[i];
                string lineText = chunk.Text + (i < ordered.Count - 1 ? "\n" : string.Empty);
                IXLRichString richRun = richText.AddText(lineText);

                // In Avalonia 11 Typeface is a struct; an uninitialised one has a null FontFamily.
                Typeface face = chunk.Typeface;

                string fontName = null;
                bool bold = false;
                bool italic = false;

                if (face.FontFamily != null)
                {
                    fontName = SafeFontFamilyName(face.FontFamily);
                    bold = (int)face.Weight >= 600;
                    italic = face.Style != FontStyle.Normal;
                }
                else if (!string.IsNullOrEmpty(chunk.FontName))
                {
                    fontName = chunk.FontName;
                }

                double sizeDip = chunk.FontSize > 0 ? chunk.FontSize : 12;
                XLColor color = BrushToXlColor(chunk.Color) ?? XLColor.Black;

                if (!string.IsNullOrEmpty(fontName))
                {
                    richRun.FontName = fontName;
                }
                richRun.FontSize = Math.Max(4, sizeDip * 0.75);
                richRun.Bold = bold;
                richRun.Italic = italic;
                richRun.FontColor = color;
            }

            // The first chunk usually defines the overall horizontal placement of the caption.
            TextAlignment hor = ordered[0].HorAlign;
            VerticalAlignment ver = table.DefaultVerticalAlignment;
            if (table.CellVerticalAlignment != null && table.CellVerticalAlignment.TryGetValue((c, r), out VerticalAlignment cellVer))
            {
                ver = cellVer;
            }

            IXLStyle style = cell.Style;
            style.Alignment.WrapText = true;
            style.Alignment.Horizontal = MapHorizontal(hor);
            style.Alignment.Vertical = MapVertical(ver);
        }

        private static void WriteLinkCell(IXLCell cell, LinkCell linkCell, Table table, int c, int r, XlsxExportOptions options)
        {
            if (linkCell == null)
            {
                return;
            }

            string rawData = null;
            if (table.Data != null && table.Data.TryGetValue((c, r), out string raw))
            {
                rawData = raw;
            }

            string text = linkCell.GetDisplayText(rawData);
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            cell.Value = text;

            XLColor linkColor = BrushToXlColor(linkCell.LinkColor) ?? XLColor.Black;

            IXLStyle style = cell.Style;
            style.Font.FontColor = linkColor;
            style.Font.Strikethrough = linkCell.Strikethrough;

            ApplyCommonStyle(style, table, c, r, null, null);

            if (options != null && options.ExportLinkAsHyperlink && !string.IsNullOrEmpty(linkCell.LinkPara) &&
                Uri.TryCreate(linkCell.LinkPara, UriKind.Absolute, out Uri uri))
            {
                try
                {
                    XLHyperlink hyperlink = cell.CreateHyperlink();
                    hyperlink.ExternalAddress = uri;
                }
                catch
                {
                    // Some URI schemes are rejected by ClosedXML; keep the plain text cell then.
                }
            }
        }

        private static void WriteFormulaCell(IXLCell cell, CellData formulaCell, Table table, int c, int r, HashSet<string> allowedFunctions, XlsxExportOptions options)
        {
            if (formulaCell == null)
            {
                return;
            }

            bool wroteFormula = false;

            if (options.ExportFormulas && formulaCell.IsFormula && !string.IsNullOrEmpty(formulaCell.Formula))
            {
                if (TryTranslateFormula(formulaCell.Formula, allowedFunctions, out string translated))
                {
                    cell.FormulaA1 = translated;
                    wroteFormula = true;
                }
                else if (options.WarningHandler != null)
                {
                    options.WarningHandler($"Cell ({c}, {r}): formula \"={formulaCell.Formula}\" is not Excel-compatible; exporting the cached value instead.");
                }
            }

            if (!wroteFormula)
            {
                WriteCachedValue(cell, formulaCell.CachedValue);
            }

            ApplyCommonStyle(cell.Style, table, c, r, null, null);
        }

        private static void WriteValueCell(IXLCell cell, string rawText, Table table, int c, int r, Typeface defaultTypeface, XLColor defaultForeground)
        {
            WriteParsedValue(cell, rawText);

            IXLStyle style = cell.Style;
            style.Font.FontColor = defaultForeground;
            ApplyCommonStyle(style, table, c, r, defaultTypeface, null);
        }

        private static void ApplyCommonStyle(IXLStyle style, Table table, int c, int r, Typeface? typeface, TextAlignment? forceHorizontal)
        {
            if (!typeface.HasValue)
            {
                if ((table.CellTypefaces != null && table.CellTypefaces.TryGetValue((c, r), out Typeface cellFace)))
                {
                    typeface = cellFace;
                }
                else if ((table.RowTypefaces != null && table.RowTypefaces.TryGetValue(r, out Typeface rowFace)))
                {
                    typeface = rowFace;
                }
                else if ((table.ColumnTypefaces != null && table.ColumnTypefaces.TryGetValue(c, out Typeface columnFace)))
                {
                    typeface = columnFace;
                }
                else
                {
                    typeface = new Typeface(table.FontFamily, table.FontStyle, table.FontWeight);
                }
            }

            Typeface resolved = typeface.Value;

            string fontName = SafeFontFamilyName(resolved.FontFamily);
            if (!string.IsNullOrEmpty(fontName))
            {
                style.Font.FontName = fontName;
            }

            double sizeDip = table.FontSize;
            if (table.CellFontSize != null && table.CellFontSize.TryGetValue((c, r), out double customSize) && customSize > 0)
            {
                sizeDip = customSize;
            }
            style.Font.FontSize = Math.Max(4, sizeDip * 0.75);
            style.Font.Bold = (int)resolved.Weight >= 600;
            style.Font.Italic = resolved.Style != FontStyle.Normal;

            if (table.CellForeground != null && table.CellForeground.TryGetValue((c, r), out IBrush foreground))
            {
                XLColor fgColor = BrushToXlColor(foreground);
                if (fgColor != null)
                {
                    style.Font.FontColor = fgColor;
                }
            }

            TextAlignment hor = forceHorizontal ?? TextAlignment.Left;
            if (!forceHorizontal.HasValue)
            {
                if (!(table.CellTextAlignment != null && table.CellTextAlignment.TryGetValue((c, r), out hor)))
                {
                    hor = table.DefaultTextAlignment;
                }
            }

            VerticalAlignment ver = table.DefaultVerticalAlignment;
            if (table.CellVerticalAlignment != null && table.CellVerticalAlignment.TryGetValue((c, r), out VerticalAlignment cellVer))
            {
                ver = cellVer;
            }

            style.Alignment.Horizontal = MapHorizontal(hor);
            style.Alignment.Vertical = MapVertical(ver);
        }

        private static void WriteCachedValue(IXLCell cell, object cachedValue)
        {
            if (cachedValue == null)
            {
                return;
            }

            if (cachedValue is double d)
            {
                cell.Value = d;
            }
            else if (cachedValue is float f)
            {
                cell.Value = (double)f;
            }
            else if (cachedValue is int i)
            {
                cell.Value = (double)i;
            }
            else if (cachedValue is long l)
            {
                cell.Value = (double)l;
            }
            else if (cachedValue is decimal m)
            {
                cell.Value = (double)m;
            }
            else if (cachedValue is bool b)
            {
                cell.Value = b ? "TRUE" : "FALSE";
            }
            else
            {
                string text = cachedValue.ToString();
                if (text == null || text.StartsWith("#", StringComparison.Ordinal))
                {
                    // Error placeholders ("#ERROR: ...", "#DIV/0!", ...) are left blank.
                    return;
                }
                cell.Value = text;
            }
        }

        private static void WriteParsedValue(IXLCell cell, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double numeric))
            {
                cell.Value = numeric;
            }
            else if (string.Equals(text, "TRUE", StringComparison.OrdinalIgnoreCase))
            {
                cell.Value = "TRUE";
            }
            else if (string.Equals(text, "FALSE", StringComparison.OrdinalIgnoreCase))
            {
                cell.Value = "FALSE";
            }
            else
            {
                cell.Value = text;
            }
        }

        private static void ApplyBorders(IXLWorksheet sheet, List<CellBorder> borders, int maxCol, int maxRow)
        {
            foreach (CellBorder border in borders)
            {
                if (border == null || border.Brush == null)
                {
                    continue;
                }

                XLColor borderColor = BrushToXlColor(border.Brush) ?? XLColor.Black;
                XLBorderStyleValues style = BorderStyleFrom(border.Thickness, border.DashStyle);

                if (!border.IsHorizontal)
                {
                    // Vertical line at separator LineIndex.
                    int line = border.LineIndex;
                    int targetCol;

                    if (line == 0)
                    {
                        targetCol = 0; // left of column 0
                    }
                    else if (line <= maxCol)
                    {
                        targetCol = line; // left of column "line"
                    }
                    else if (line == maxCol + 1)
                    {
                        targetCol = maxCol; // right of the last column
                    }
                    else
                    {
                        continue;
                    }

                    int fromRow = Math.Max(0, border.From);
                    int toRow = Math.Min(maxRow, border.To);

                    for (int r = fromRow; r <= toRow; r++)
                    {
                        IXLBorder cellBorder = sheet.Cell(r + 1, targetCol + 1).Style.Border;
                        if (line == 0 || targetCol == maxCol && line == maxCol + 1)
                        {
                            // outer left edge -> LeftBorder, outer right edge -> RightBorder.
                            if (line == 0)
                            {
                                cellBorder.LeftBorder = style;
                                cellBorder.LeftBorderColor = borderColor;
                            }
                            else
                            {
                                cellBorder.RightBorder = style;
                                cellBorder.RightBorderColor = borderColor;
                            }
                        }
                        else
                        {
                            cellBorder.LeftBorder = style;
                            cellBorder.LeftBorderColor = borderColor;
                        }
                    }
                }
                else
                {
                    // Horizontal line at separator LineIndex.
                    int line = border.LineIndex;
                    int targetRow;

                    if (line == 0)
                    {
                        targetRow = 0; // above row 0
                    }
                    else if (line <= maxRow)
                    {
                        targetRow = line; // above row "line"
                    }
                    else if (line == maxRow + 1)
                    {
                        targetRow = maxRow; // below the last row
                    }
                    else
                    {
                        continue;
                    }

                    int fromCol = Math.Max(0, border.From);
                    int toCol = Math.Min(maxCol, border.To);

                    for (int c = fromCol; c <= toCol; c++)
                    {
                        IXLBorder cellBorder = sheet.Cell(targetRow + 1, c + 1).Style.Border;
                        if (line == 0)
                        {
                            cellBorder.TopBorder = style;
                            cellBorder.TopBorderColor = borderColor;
                        }
                        else
                        {
                            cellBorder.BottomBorder = style;
                            cellBorder.BottomBorderColor = borderColor;
                        }
                    }
                }
            }
        }

        private static XLBorderStyleValues BorderStyleFrom(double thickness, IDashStyle dashStyle)
        {
            if (dashStyle != null)
            {
                return XLBorderStyleValues.Dashed;
            }

            if (thickness <= 0.6)
            {
                return XLBorderStyleValues.Hair;
            }
            if (thickness <= 1.75)
            {
                return XLBorderStyleValues.Thin;
            }
            if (thickness <= 3.25)
            {
                return XLBorderStyleValues.Medium;
            }
            return XLBorderStyleValues.Thick;
        }

        /// <summary>
        /// Translates a Spreadalonia formula into an Excel-compatible formula, checking that every
        /// function name is recognised by Excel. Returns <see langword="false"/> when the formula
        /// uses unknown (custom) functions or single-quoted string literals, in which case the
        /// caller should export the cached value instead.
        /// </summary>
        internal static bool TryTranslateFormula(string formula, HashSet<string> allowedFunctions, out string translated)
        {
            translated = null;

            if (string.IsNullOrEmpty(formula))
            {
                translated = string.Empty;
                return true;
            }

            StringBuilder result = new StringBuilder(formula.Length + 8);
            int length = formula.Length;
            int i = 0;

            while (i < length)
            {
                char ch = formula[i];

                // Double-quoted Excel string literal: copy verbatim (with doubled quotes).
                if (ch == '"')
                {
                    result.Append(ch);
                    i++;

                    while (i < length)
                    {
                        if (formula[i] == '"')
                        {
                            if (i + 1 < length && formula[i + 1] == '"')
                            {
                                result.Append("\"\"");
                                i += 2;
                                continue;
                            }
                            result.Append('"');
                            i++;
                            break;
                        }
                        result.Append(formula[i]);
                        i++;
                    }
                    continue;
                }

                // Single-quoted literals are supported by Spreadalonia but not by Excel.
                if (ch == '\'')
                {
                    return false;
                }

                if (char.IsLetter(ch) || ch == '_')
                {
                    int start = i;
                    while (i < length && (char.IsLetterOrDigit(formula[i]) || formula[i] == '_'))
                    {
                        i++;
                    }

                    string identifier = formula.Substring(start, i - start);

                    // Only identifiers directly followed by "(" are function calls.
                    int j = i;
                    while (j < length && char.IsWhiteSpace(formula[j]))
                    {
                        j++;
                    }

                    if (j < length && formula[j] == '(')
                    {
                        string upper = identifier.ToUpperInvariant();
                        if (upper == "AVG")
                        {
                            result.Append("AVERAGE");
                        }
                        else if (allowedFunctions.Contains(upper))
                        {
                            result.Append(identifier);
                        }
                        else
                        {
                            return false;
                        }

                        i = j;
                    }
                    else
                    {
                        result.Append(identifier);
                    }
                    continue;
                }

                result.Append(ch);
                i++;
            }

            translated = result.ToString();
            return true;
        }

        private static XLColor BrushToXlColor(IBrush brush)
        {
            if (brush == null)
            {
                return null;
            }

            if (brush is ISolidColorBrush solid)
            {
                Color color = solid.Color;
                return XLColor.FromArgb(color.A, color.R, color.G, color.B);
            }

            // Gradients and other complex brushes have no single colour; fall back to black.
            if (brush is ILinearGradientBrush gradient && gradient.GradientStops != null && gradient.GradientStops.Count > 0)
            {
                Color stopColor = gradient.GradientStops[0].Color;
                return XLColor.FromArgb(stopColor.A, stopColor.R, stopColor.G, stopColor.B);
            }

            return null;
        }

        private static string SafeFontFamilyName(FontFamily fontFamily)
        {
            if (fontFamily == null)
            {
                return null;
            }

            try
            {
                string name = fontFamily.Name;
                if (string.IsNullOrEmpty(name))
                {
                    return null;
                }
                return name;
            }
            catch
            {
                return null;
            }
        }

        private static XLAlignmentHorizontalValues MapHorizontal(TextAlignment alignment)
        {
            switch (alignment)
            {
                case TextAlignment.Center:
                    return XLAlignmentHorizontalValues.Center;
                case TextAlignment.Right:
                    return XLAlignmentHorizontalValues.Right;
                case TextAlignment.Justify:
                    return XLAlignmentHorizontalValues.Justify;
                default:
                    return XLAlignmentHorizontalValues.Left;
            }
        }

        private static XLAlignmentVerticalValues MapVertical(VerticalAlignment alignment)
        {
            switch (alignment)
            {
                case VerticalAlignment.Top:
                    return XLAlignmentVerticalValues.Top;
                case VerticalAlignment.Bottom:
                    return XLAlignmentVerticalValues.Bottom;
                case VerticalAlignment.Stretch:
                    return XLAlignmentVerticalValues.Justify;
                default:
                    return XLAlignmentVerticalValues.Center;
            }
        }
    }
}
