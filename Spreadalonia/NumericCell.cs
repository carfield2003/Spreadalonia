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
            if (!TryParse(rawData, out decimal value))
            {
                return string.Empty;
            }

            if (zeroAsEmpty && value == 0m)
            {
                return string.Empty;
            }

            return FormatValue(value, digits);
        }

        /// <summary>
        /// Formats an already parsed value for display.
        /// </summary>
        /// <param name="value">The value to format.</param>
        /// <param name="digits">The number of decimal digits.</param>
        /// <returns>The formatted display text.</returns>
        internal static string FormatValue(decimal value, int digits)
        {
            if (digits < 0)
            {
                digits = 0;
            }

            string format = digits > 0 ? "#,#0." + new string('0', digits) : "#,#0";
            return Round(value, digits).ToString(format, CultureInfo.CurrentCulture);
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

        /// <summary>
        /// The number of decimal digits displayed (and enforced when editing).
        /// </summary>
        public int Digits { get; set; } = 2;

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
            return NumberFormatter.Format(rawData, Digits, ZeroAsEmpty);
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
