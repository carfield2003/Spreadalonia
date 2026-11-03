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
    /// The kind of number format applied to a <see cref="NumericCell"/>.
    /// </summary>
    public enum NumericFormatKind
    {
        /// <summary>A plain number (the default, and the behaviour before this enum existed).</summary>
        Number = 0,

        /// <summary>A currency amount: the value is prefixed with <see cref="NumericCell.CurrencySymbol"/>.</summary>
        Currency = 1,

        /// <summary>
        /// A percentage. The stored value is the ratio (0.1234), the display is multiplied by 100 and
        /// carries a percent sign, and the exported workbook gets the <c>0.00%</c> format.
        /// </summary>
        Percent = 2
    }

    /// <summary>
    /// Shared numeric formatting / parsing helpers. Both <see cref="NumericCell"/> and
    /// <see cref="NumericLinkCell"/> route through this class so that the two implementations
    /// can never drift apart.
    /// </summary>
    internal static class NumberFormatter
    {
        /// <summary>
        /// Rounds <paramref name="value"/> to <paramref name="digits"/> decimal digits using the
        /// same "round half away from zero for positives" convention as the WPF reference
        /// implementation (<c>Math.Floor(d * 10^n + 0.5) / 10^n</c>).
        /// </summary>
        /// <param name="value">The value to round.</param>
        /// <param name="digits">The number of decimal digits.</param>
        /// <returns>The rounded value.</returns>
        internal static decimal Round(decimal value, int digits)
        {
            if (digits < 0)
            {
                digits = 0;
            }

            if (digits > 28)
            {
                digits = 28;
            }

            decimal factor = 1m;
            for (int i = 0; i < digits; i++)
            {
                factor *= 10m;
            }

            decimal scaled = value * factor;

            // Math.Floor(x + 0.5) matches the WPF behaviour for positive values; negative values
            // are mirrored so that -1.005 with 2 digits becomes -1.01 rather than -1.00.
            decimal rounded = scaled >= 0
                ? Math.Floor(scaled + 0.5m)
                : -Math.Floor(-scaled + 0.5m);

            return rounded / factor;
        }

        /// <summary>
        /// Formats a raw cell value for display: thousand separators, a fixed number of decimal
        /// digits, and an empty string for zero / non-numeric values.
        /// </summary>
        /// <param name="rawData">The raw string stored in the cell.</param>
        /// <param name="digits">The number of decimal digits.</param>
        /// <param name="zeroAsEmpty">Whether a zero value is displayed as an empty string.</param>
        /// <returns>The formatted display text.</returns>
        internal static string Format(string rawData, int digits, bool zeroAsEmpty)
        {
            return Format(rawData, digits, zeroAsEmpty, true, NumericFormatKind.Number, NumericCell.DefaultCurrencySymbol);
        }

        /// <summary>
        /// Formats a raw cell value for display, honouring the thousand separator switch and the
        /// number format kind (plain number, currency or percentage), and returns the parsed value
        /// together with the display text.
        /// </summary>
        /// <param name="rawData">The raw string stored in the cell.</param>
        /// <param name="digits">The number of decimal digits.</param>
        /// <param name="zeroAsEmpty">Whether a zero value is displayed as an empty string.</param>
        /// <param name="thousandsSeparator">Whether thousand separators are drawn.</param>
        /// <param name="kind">The kind of number format.</param>
        /// <param name="currencySymbol">The currency symbol used by <see cref="NumericFormatKind.Currency"/>.</param>
        /// <returns>The formatted display text.</returns>
        internal static string Format(string rawData, int digits, bool zeroAsEmpty, bool thousandsSeparator, NumericFormatKind kind, string currencySymbol)
        {
            if (!TryFormat(rawData, digits, zeroAsEmpty, thousandsSeparator, kind, currencySymbol, out string displayText, out _))
            {
                return string.Empty;
            }

            return displayText;
        }

        /// <summary>
        /// The single formatting entry point shared by the renderer (regular and merged cells) and
        /// by the xlsx exporter, so that a cell can never look different on screen than in the
        /// exported workbook.
        /// </summary>
        /// <param name="rawData">The raw string stored in the cell.</param>
        /// <param name="digits">The number of decimal digits.</param>
        /// <param name="zeroAsEmpty">Whether a zero value is displayed as an empty string.</param>
        /// <param name="thousandsSeparator">Whether thousand separators are drawn.</param>
        /// <param name="kind">The kind of number format.</param>
        /// <param name="currencySymbol">The currency symbol used by <see cref="NumericFormatKind.Currency"/>.</param>
        /// <param name="displayText">The formatted display text (empty when the value cannot be parsed).</param>
        /// <param name="value">The parsed raw value (zero when the value cannot be parsed).</param>
        /// <returns><see langword="true"/> when the raw value could be parsed as a number.</returns>
        internal static bool TryFormat(string rawData, int digits, bool zeroAsEmpty, bool thousandsSeparator, NumericFormatKind kind, string currencySymbol, out string displayText, out decimal value)
        {
            displayText = string.Empty;
            value = 0m;

            if (!TryParse(rawData, out value))
            {
                return false;
            }

            if (zeroAsEmpty && value == 0m)
            {
                return true;
            }

            displayText = FormatValue(value, digits, thousandsSeparator, kind, currencySymbol);
            return true;
        }

        /// <summary>
        /// Formats an already parsed value for display.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="digits">The number of decimal digits.</param>
        /// <returns>The formatted display text.</returns>
        internal static string FormatValue(decimal value, int digits)
        {
            return FormatValue(value, digits, true, NumericFormatKind.Number, NumericCell.DefaultCurrencySymbol);
        }

        /// <summary>
        /// Formats an already parsed value for display, honouring the thousand separator switch and
        /// the number format kind.
        /// <para>
        /// The value itself is never modified: a percentage is stored as its raw ratio (0.1234) and
        /// only multiplied by 100 for display, so that the cell keeps a calculable value and the
        /// exported workbook (which applies the <c>0.00%</c> format on its own) agrees with the
        /// screen.
        /// </para>
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="digits">The number of decimal digits.</param>
        /// <param name="thousandsSeparator">Whether thousand separators are drawn.</param>
        /// <param name="kind">The kind of number format.</param>
        /// <param name="currencySymbol">The currency symbol used by <see cref="NumericFormatKind.Currency"/>.</param>
        /// <returns>The formatted display text.</returns>
        internal static string FormatValue(decimal value, int digits, bool thousandsSeparator, NumericFormatKind kind, string currencySymbol)
        {
            if (digits < 0)
            {
                digits = 0;
            }

            string decimals = digits > 0 ? "." + new string('0', digits) : string.Empty;
            string group = thousandsSeparator ? "#,#0" : "0";

            switch (kind)
            {
                case NumericFormatKind.Percent:
                    // The stored value is the ratio; the screen shows it as a percentage. The percent
                    // sign is appended by hand: a '%' inside a .NET numeric format string would make
                    // ToString multiply the value by 100 a second time.
                    // Scale first, then round: rounding the ratio to two digits would turn 0.1234
                    // into 0.12 (i.e. 12.00%) instead of 12.34%.
                    return Round(value * 100m, digits).ToString(group + decimals, CultureInfo.CurrentCulture) + "%";

                case NumericFormatKind.Currency:
                    string text = Round(value, digits).ToString(group + decimals, CultureInfo.CurrentCulture);
                    return (currencySymbol ?? string.Empty) + text;

                default:
                    return Round(value, digits).ToString(group + decimals, CultureInfo.CurrentCulture);
            }
        }

        /// <summary>
        /// Builds the Excel number format string that corresponds to the given settings. It is used
        /// by the xlsx exporter, so that the exported cell carries the same format as the screen.
        /// </summary>
        /// <param name="digits">The number of decimal digits.</param>
        /// <param name="thousandsSeparator">Whether thousand separators are used.</param>
        /// <param name="kind">The kind of number format.</param>
        /// <param name="currencySymbol">The currency symbol used by <see cref="NumericFormatKind.Currency"/>.</param>
        /// <returns>The Excel number format string.</returns>
        internal static string GetNumberFormat(int digits, bool thousandsSeparator, NumericFormatKind kind, string currencySymbol)
        {
            if (digits < 0)
            {
                digits = 0;
            }

            string decimals = digits > 0 ? "." + new string('0', digits) : string.Empty;
            string group = thousandsSeparator ? "#,##0" : "0";

            switch (kind)
            {
                case NumericFormatKind.Percent:
                    return group + decimals + "%";

                case NumericFormatKind.Currency:
                    // Excel treats the quoted symbol as a literal, next to the number.
                    return "\"" + (currencySymbol ?? string.Empty) + "\"" + group + decimals;

                default:
                    return group + decimals;
            }
        }

        /// <summary>
        /// Attempts to parse a raw cell value that may contain thousand separators.
        /// </summary>
        /// <param name="rawData">The raw string stored in the cell.</param>
        /// <param name="value">The parsed value.</param>
        /// <returns><see langword="true"/> if the value could be parsed.</returns>
        internal static bool TryParse(string rawData, out decimal value)
        {
            value = 0m;

            if (string.IsNullOrWhiteSpace(rawData))
            {
                return false;
            }

            string text = rawData.Trim();

            return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value) ||
                   decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value) ||
                   decimal.TryParse(StripGroupSeparators(text), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out value) ||
                   decimal.TryParse(StripGroupSeparators(text), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>
        /// Removes thousand separators and spaces from a numeric string.
        /// </summary>
        /// <param name="text">The text to strip.</param>
        /// <returns>The stripped text.</returns>
        internal static string StripGroupSeparators(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            char[] buffer = text.ToCharArray();
            int len = 0;

            for (int i = 0; i < buffer.Length; i++)
            {
                char c = buffer[i];

                if (c == ',' || c == ' ' || c == '　')
                {
                    continue;
                }

                buffer[len++] = c;
            }

            return new string(buffer, 0, len);
        }

        /// <summary>
        /// Validates and normalises text typed by the user.
        /// </summary>
        /// <param name="input">The text typed by the user.</param>
        /// <param name="digits">The maximum number of decimal digits.</param>
        /// <param name="allowNegative">Whether negative values are accepted.</param>
        /// <param name="normalized">
        /// The normalised value (round-half-away-from-zero to <paramref name="digits"/> digits,
        /// invariant culture, no thousand separators), or <see langword="null"/> when the input
        /// was empty (meaning "clear the cell").
        /// </param>
        /// <param name="error">A human readable reason when the input is rejected.</param>
        /// <returns><see langword="true"/> if the input is acceptable.</returns>
        internal static bool TryValidate(string input, int digits, bool allowNegative, out string normalized, out string error)
        {
            normalized = null;
            error = null;

            if (string.IsNullOrWhiteSpace(input))
            {
                // An empty value means "clear the cell" (equivalent to zero) and is always accepted.
                return true;
            }

            string text = input.Trim();

            if (!IsSyntacticallyValid(text, digits, allowNegative, out error))
            {
                return false;
            }

            if (!decimal.TryParse(StripGroupSeparators(text), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal value))
            {
                error = NumericCell.ReasonNotANumber;
                return false;
            }

            if (!allowNegative && value < 0)
            {
                error = NumericCell.ReasonNegativeNotAllowed;
                return false;
            }

            value = Round(value, digits);
            normalized = value.ToString("0.############################", CultureInfo.InvariantCulture);
            return true;
        }

        /// <summary>
        /// Checks whether the text may appear in the editing box while the user is still typing.
        /// This is a looser check than <see cref="IsSyntacticallyValid"/>: partial values such as
        /// "-" or "1." are accepted because they are legitimate intermediate states.
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <param name="digits">The maximum number of decimal digits.</param>
        /// <param name="allowNegative">Whether a leading minus sign is accepted.</param>
        /// <returns><see langword="true"/> if the character may be kept.</returns>
        internal static bool IsTypingValid(string text, int digits, bool allowNegative)
        {
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }

            bool seenDot = false;
            int decimals = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c >= '0' && c <= '9')
                {
                    if (seenDot)
                    {
                        decimals++;

                        if (decimals > digits)
                        {
                            return false;
                        }
                    }

                    continue;
                }

                if (c == '.' || c == ',')
                {
                    if (seenDot)
                    {
                        return false;
                    }

                    seenDot = true;
                    continue;
                }

                if (c == '-')
                {
                    if (i != 0 || !allowNegative)
                    {
                        return false;
                    }

                    continue;
                }

                if (c == ' ')
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks that the text only contains characters that may appear in a number, and that the
        /// number of decimal digits does not exceed <paramref name="digits"/>.
        /// </summary>
        /// <param name="text">The text to check.</param>
        /// <param name="digits">The maximum number of decimal digits.</param>
        /// <param name="allowNegative">Whether a leading minus sign is accepted.</param>
        /// <param name="error">A human readable reason when the text is rejected.</param>
        /// <returns><see langword="true"/> if the text is syntactically valid.</returns>
        internal static bool IsSyntacticallyValid(string text, int digits, bool allowNegative, out string error)
        {
            error = null;

            bool seenDot = false;
            int decimals = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];

                if (c >= '0' && c <= '9')
                {
                    if (seenDot)
                    {
                        decimals++;

                        if (decimals > digits)
                        {
                            error = NumericCell.ReasonTooManyDecimals;
                            return false;
                        }
                    }

                    continue;
                }

                if (c == '.' || c == ',')
                {
                    if (seenDot)
                    {
                        error = NumericCell.ReasonNotANumber;
                        return false;
                    }

                    seenDot = true;
                    continue;
                }

                if (c == '-')
                {
                    if (i != 0 || !allowNegative)
                    {
                        error = NumericCell.ReasonNotANumber;
                        return false;
                    }

                    continue;
                }

                if (c == ' ')
                {
                    continue;
                }

                error = NumericCell.ReasonNotANumber;
                return false;
            }

            if (text.Length == 1 && (text[0] == '.' || text[0] == '-'))
            {
                error = NumericCell.ReasonNotANumber;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// A cell that displays its raw content as a formatted number (thousand separators and a fixed
    /// number of decimal digits) and only accepts numeric input when edited.
    /// </summary>
    public class NumericCell
    {
        /// <summary>Reason code used when the typed text cannot be parsed as a number.</summary>
        internal const string ReasonNotANumber = "NotANumber";

        /// <summary>Reason code used when the typed value is negative but negatives are not allowed.</summary>
        internal const string ReasonNegativeNotAllowed = "NegativeNotAllowed";

        /// <summary>Reason code used when the typed value has more decimal digits than allowed.</summary>
        internal const string ReasonTooManyDecimals = "TooManyDecimals";

        /// <summary>The currency symbol used when <see cref="CurrencySymbol"/> is not set.</summary>
        internal const string DefaultCurrencySymbol = "¥";

        /// <summary>
        /// The number of decimal digits displayed (and enforced when editing).
        /// </summary>
        public int Digits { get; set; } = 2;

        /// <summary>
        /// Whether thousand separators are drawn. The default (<see langword="true"/>) is the
        /// behaviour before this property existed, so an existing configuration keeps its format.
        /// </summary>
        public bool ThousandsSeparator { get; set; } = true;

        /// <summary>
        /// The kind of number format applied to the value: a plain number, a currency amount or a
        /// percentage. For a percentage the stored value is the ratio (0.1234) and only the display
        /// is multiplied by 100, so that the cell keeps a calculable value.
        /// </summary>
        public NumericFormatKind FormatKind { get; set; } = NumericFormatKind.Number;

        /// <summary>
        /// The symbol drawn in front of a <see cref="NumericFormatKind.Currency"/> value.
        /// </summary>
        public string CurrencySymbol { get; set; } = DefaultCurrencySymbol;

        /// <summary>
        /// Whether a zero (or empty) value is displayed as an empty string.
        /// </summary>
        public bool ZeroAsEmpty { get; set; } = true;

        /// <summary>
        /// Whether a red strikethrough line is drawn through the text (used for posted / reversed data).
        /// </summary>
        public bool Strikethrough { get; set; }

        /// <summary>
        /// Whether negative values are accepted.
        /// </summary>
        public bool AllowNegative { get; set; } = true;

        /// <summary>
        /// Whether negative values are drawn in red.
        /// </summary>
        public bool NegativeRed { get; set; }

        /// <summary>
        /// Whether the editing box rejects characters that cannot be part of a number while the
        /// user is typing. When this is <see langword="false"/> the commit-time validation is the
        /// only guard.
        /// </summary>
        public bool FilterWhileTyping { get; set; } = true;

        /// <summary>
        /// The brush used to draw negative values when <see cref="NegativeRed"/> is set.
        /// </summary>
        public IBrush NegativeBrush { get; set; }

        /// <summary>
        /// The brush used to draw the <see cref="Strikethrough"/> line.
        /// </summary>
        public IBrush StrikethroughBrush { get; set; }

        /// <summary>
        /// Creates a new <see cref="NumericCell"/> with two decimal digits.
        /// </summary>
        public NumericCell()
        {
        }

        /// <summary>
        /// Creates a new <see cref="NumericCell"/> with the specified number of decimal digits.
        /// </summary>
        /// <param name="digits">The number of decimal digits.</param>
        public NumericCell(int digits)
        {
            Digits = digits;
        }

        /// <summary>
        /// Computes the text to display for the specified raw cell value.
        /// </summary>
        /// <param name="rawData">The raw string stored in the cell.</param>
        /// <returns>The formatted display text.</returns>
        public string GetDisplayText(string rawData)
        {
            return NumberFormatter.Format(rawData, Digits, ZeroAsEmpty, ThousandsSeparator, FormatKind, CurrencySymbol);
        }

        /// <summary>
        /// The Excel number format that corresponds to <see cref="Digits"/> (thousands separators and a
        /// fixed number of decimals, e.g. <c>#,##0.00</c>). It is used both by the renderer and by the
        /// xlsx exporter, so that a cell looks the same on screen and in the exported workbook.
        /// <para>
        /// With the default settings (thousand separators on, <see cref="NumericFormatKind.Number"/>)
        /// the result is exactly what the previous version produced, so an existing workbook keeps its
        /// format.
        /// </para>
        /// </summary>
        public string GetNumberFormat()
        {
            return NumberFormatter.GetNumberFormat(Digits, ThousandsSeparator, FormatKind, CurrencySymbol);
        }

        /// <summary>
        /// Validates text typed by the user and normalises it to the value that should be stored
        /// in the cell data.
        /// </summary>
        /// <param name="input">The text typed by the user.</param>
        /// <param name="normalized">
        /// The normalised value, or <see langword="null"/> when the input was empty (meaning that
        /// the cell should be cleared).
        /// </param>
        /// <param name="error">A reason code when the input is rejected.</param>
        /// <returns><see langword="true"/> if the input is acceptable.</returns>
        public bool TryValidate(string input, out string normalized, out string error)
        {
            return NumberFormatter.TryValidate(input, Digits, AllowNegative, out normalized, out error);
        }

        /// <summary>
        /// Returns <see langword="true"/> when the raw cell value is negative and should be drawn
        /// with <see cref="NegativeBrush"/>.
        /// </summary>
        /// <param name="rawData">The raw string stored in the cell.</param>
        internal bool IsNegative(string rawData)
        {
            return NumberFormatter.TryParse(rawData, out decimal value) && value < 0m;
        }

        /// <summary>
        /// Renders the numeric cell into the specified drawing context.
        /// </summary>
        /// <param name="context">The drawing context.</param>
        /// <param name="bounds">The bounds available to the cell.</param>
        /// <param name="rawData">The raw string stored in the cell.</param>
        /// <param name="typeface">The typeface used to draw the text.</param>
        /// <param name="fontSize">The font size used to draw the text.</param>
        /// <param name="margin">The cell margin.</param>
        /// <param name="defaultBrush">The brush used when the value is neither negative nor struck through.</param>
        internal void Render(DrawingContext context, Rect bounds, string rawData, Typeface typeface, double fontSize, Thickness margin, IBrush defaultBrush)
        {
            string text = GetDisplayText(rawData);

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            IBrush brush = defaultBrush;

            if (NegativeRed && IsNegative(rawData))
            {
                brush = NegativeBrush ?? new SolidColorBrush(Colors.Red);
            }

            FormattedText fmtText;

            try
            {
                fmtText = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, fontSize, brush);
            }
            catch
            {
                return;
            }

            // Numeric values are always right aligned and vertically centred, like the WPF reference.
            double x = bounds.X + bounds.Width - margin.Right - fmtText.Width;
            double y = bounds.Y + (bounds.Height - fmtText.Height) * 0.5;

            using (context.PushClip(new Rect(
                bounds.X + margin.Left,
                bounds.Y + margin.Top,
                Math.Max(0, bounds.Width - margin.Left - margin.Right),
                Math.Max(0, bounds.Height - margin.Top - margin.Bottom))))
            {
                context.DrawText(fmtText, new Point(x, y));

                if (Strikethrough)
                {
                    IBrush strikeBrush = StrikethroughBrush ?? new SolidColorBrush(Colors.Red);
                    double strikeY = y + fmtText.Height * 0.5;
                    context.DrawLine(new Pen(strikeBrush, 2), new Point(x, strikeY), new Point(x + fmtText.Width, strikeY));
                }
            }
        }
    }

    /// <summary>
    /// Carries the details of an input that was rejected by a <see cref="NumericCell"/>.
    /// </summary>
    public class InvalidInputEventArgs : EventArgs
    {
        /// <summary>
        /// The column of the cell whose input was rejected.
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// The row of the cell whose input was rejected.
        /// </summary>
        public int Row { get; }

        /// <summary>
        /// The text that was typed by the user.
        /// </summary>
        public string InputText { get; }

        /// <summary>
        /// The reason why the input was rejected
        /// (<see cref="NumericCell"/> reason codes: <c>NotANumber</c>, <c>NegativeNotAllowed</c>,
        /// <c>TooManyDecimals</c>).
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Creates a new <see cref="InvalidInputEventArgs"/>.
        /// </summary>
        /// <param name="column">The column of the cell.</param>
        /// <param name="row">The row of the cell.</param>
        /// <param name="inputText">The text typed by the user.</param>
        /// <param name="reason">The reason why the input was rejected.</param>
        public InvalidInputEventArgs(int column, int row, string inputText, string reason)
        {
            Column = column;
            Row = row;
            InputText = inputText;
            Reason = reason;
        }
    }
}
