/*
    Spreadalonia - A spreadsheet control for Avalonia
    Formula engine extensions.
*/

using System;

namespace Spreadalonia.Formula
{
    /// <summary>
    /// Represents the type of value stored in a cell.
    /// </summary>
    public enum CellType
    {
        /// <summary>Empty or unrecognized value.</summary>
        Empty,
        /// <summary>A text string.</summary>
        String,
        /// <summary>A numeric value (integer or floating-point).</summary>
        Number,
        /// <summary>A boolean value.</summary>
        Boolean,
    }

    /// <summary>
    /// Represents the data stored in a single spreadsheet cell,
    /// supporting both plain text and formula-based values.
    /// </summary>
    public class CellData
    {
        /// <summary>
        /// The raw text entered by the user (including leading "=" for formulas).
        /// </summary>
        public string RawText { get; set; }

        /// <summary>
        /// If the cell contains a formula, this is the formula expression
        /// without the leading "=". Otherwise, null.
        /// </summary>
        public string Formula { get; set; }

        /// <summary>
        /// The cached evaluated value of the cell. For non-formula cells,
        /// this typically equals RawText (or a parsed numeric value).
        /// </summary>
        public object CachedValue { get; set; }

        /// <summary>
        /// The type of the cached value.
        /// </summary>
        public CellType ValueType { get; set; }

        /// <summary>
        /// Whether this cell contains a formula.
        /// </summary>
        public bool IsFormula => !string.IsNullOrEmpty(Formula);

        /// <summary>
        /// The display text to show in the cell.
        /// For non-formula cells, returns RawText. For formula cells,
        /// returns the string representation of CachedValue.
        /// </summary>
        public string DisplayText
        {
            get
            {
                if (CachedValue == null)
                    return RawText ?? string.Empty;
                return CachedValue.ToString();
            }
        }

        /// <summary>
        /// Creates a new CellData with string content.
        /// </summary>
        public static CellData FromText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            var cell = new CellData { RawText = text };

            // Try parsing as number
            if (double.TryParse(text,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double numVal))
            {
                cell.CachedValue = numVal;
                cell.ValueType = CellType.Number;
            }
            // Try parsing as boolean
            else if (string.Equals(text, "TRUE", StringComparison.OrdinalIgnoreCase))
            {
                cell.CachedValue = true;
                cell.ValueType = CellType.Boolean;
            }
            else if (string.Equals(text, "FALSE", StringComparison.OrdinalIgnoreCase))
            {
                cell.CachedValue = false;
                cell.ValueType = CellType.Boolean;
            }
            else
            {
                cell.CachedValue = text;
                cell.ValueType = CellType.String;
            }

            return cell;
        }

        /// <summary>
        /// Creates a CellData that represents an error state.
        /// </summary>
        public static CellData FromError(string formula, string errorMessage)
        {
            return new CellData
            {
                RawText = "=" + formula,
                Formula = formula,
                CachedValue = "#ERROR: " + errorMessage,
                ValueType = CellType.String
            };
        }

        /// <summary>
        /// Converts the cell data back to a serializable string.
        /// For formula cells, returns "=" + Formula. Otherwise, returns RawText.
        /// </summary>
        public string ToSerializedString()
        {
            if (IsFormula)
                return "=" + Formula;
            return RawText ?? string.Empty;
        }

        /// <summary>
        /// Infers the cell type from an evaluated value.
        /// </summary>
        public static CellType InferType(object value)
        {
            if (value == null) return CellType.Empty;
            if (value is double || value is int || value is float || value is long || value is decimal)
                return CellType.Number;
            if (value is bool)
                return CellType.Boolean;
            return CellType.String;
        }
    }
}
