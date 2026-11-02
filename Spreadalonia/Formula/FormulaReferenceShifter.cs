using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Spreadalonia.Formula
{
    /// <summary>
    /// Rewrites the cell and range references of a formula after rows or columns have been
    /// inserted into or removed from a worksheet.
    /// <para>
    /// The rewrite is carried out on the tokens of the formula (through <see cref="Lexer"/>), not
    /// on its syntax tree: every token that is not a reference is copied verbatim, so the spacing,
    /// the operator precedence and the spelling of the function names are preserved and the
    /// rewritten formula stays byte for byte identical to the original one apart from the
    /// references (<c>SUM(A41:A43)</c> becomes <c>SUM(A42:A44)</c> and not
    /// <c>SUM((A42:A44))</c>, which is what rebuilding the text from the AST would produce).
    /// </para>
    /// <para>
    /// References are relative by default, like in Excel: a reference moves along with the cells
    /// that are inserted above / before it, and disappears (as an <c>#REF!</c> error) when the
    /// cells it points at are removed. The <c>$</c> flags are honoured: <c>$A$1</c> is never moved,
    /// <c>$A1</c> only moves with the rows, <c>A$1</c> only with the columns. The references of a
    /// formula are shifted on behalf of the cell that holds it, so that a formula that has been
    /// moved along with its data keeps pointing at the same cells.
    /// </para>
    /// </summary>
    public static class FormulaReferenceShifter
    {
        /// <summary>
        /// The text written in place of a reference whose cells have been removed.
        /// </summary>
        public const string ReferenceError = "#REF!";

        /// <summary>
        /// Shifts the references of a formula after <paramref name="count"/> rows have been
        /// inserted (<paramref name="delete"/> is <see langword="false"/>) or removed
        /// (<paramref name="delete"/> is <see langword="true"/>) at row <paramref name="index"/>.
        /// </summary>
        /// <param name="formula">The formula, with or without its leading equals sign.</param>
        /// <param name="index">The position the first inserted / removed row has.</param>
        /// <param name="count">The number of rows inserted or removed.</param>
        /// <param name="delete">Whether the rows are removed instead of inserted.</param>
        /// <returns>The rewritten formula (keeping the leading equals sign of the input), or <see langword="null"/> when no reference had to change.</returns>
        public static string ShiftRows(string formula, int index, int count, bool delete)
        {
            return Shift(formula, null, new IndexShifter(index, count, delete));
        }

        /// <summary>
        /// Shifts the references of a formula after <paramref name="count"/> columns have been
        /// inserted (<paramref name="delete"/> is <see langword="false"/>) or removed
        /// (<paramref name="delete"/> is <see langword="true"/>) at column <paramref name="index"/>.
        /// </summary>
        /// <param name="formula">The formula, with or without its leading equals sign.</param>
        /// <param name="index">The position the first inserted / removed column has.</param>
        /// <param name="count">The number of columns inserted or removed.</param>
        /// <param name="delete">Whether the columns are removed instead of inserted.</param>
        /// <returns>The rewritten formula (keeping the leading equals sign of the input), or <see langword="null"/> when no reference had to change.</returns>
        public static string ShiftColumns(string formula, int index, int count, bool delete)
        {
            return Shift(formula, new IndexShifter(index, count, delete), null);
        }

        /// <summary>
        /// The shift applied to one index: an index before the changed block stays where it is, an
        /// index at or after it moves by the number of inserted / removed rows or columns (or
        /// comes back by that amount when the block is removed). <see cref="int.MinValue"/> marks
        /// an index that no longer exists.
        /// </summary>
        private sealed class IndexShifter
        {
            private readonly int index;
            private readonly int count;
            private readonly bool delete;

            public IndexShifter(int index, int count, bool delete)
            {
                this.index = index;
                this.count = count;
                this.delete = delete;
            }

            public int Shift(int value)
            {
                if (!this.delete)
                {
                    return value >= this.index ? value + this.count : value;
                }

                if (value < this.index)
                {
                    return value;
                }

                return value >= this.index + this.count ? value - this.count : int.MinValue;
            }
        }

        /// <summary>
        /// Rewrites the references of a formula.
        /// </summary>
        /// <param name="formula">The formula, with or without its leading equals sign.</param>
        /// <param name="columnShifter">The shift applied to the column of every reference, or <see langword="null"/> to leave the columns alone.</param>
        /// <param name="rowShifter">The shift applied to the row of every reference, or <see langword="null"/> to leave the rows alone.</param>
        /// <returns>The rewritten formula, or <see langword="null"/> when nothing had to change.</returns>
        private static string Shift(string formula, IndexShifter columnShifter, IndexShifter rowShifter)
        {
            if (string.IsNullOrEmpty(formula) || (columnShifter == null && rowShifter == null))
            {
                return null;
            }

            bool hasEquals = formula[0] == '=';
            string body = hasEquals ? formula.Substring(1) : formula;

            List<Token> tokens;

            try
            {
                tokens = new Lexer(body).Tokenize();
            }
            catch
            {
                // A formula that cannot be tokenized is left alone rather than corrupted.
                return null;
            }

            StringBuilder builder = new StringBuilder(body);
            bool changed = false;

            // The tokens are rewritten from the last one to the first one, so that the positions
            // recorded by the lexer (which refer to the original text) stay valid while the
            // length of the text changes.
            for (int i = tokens.Count - 1; i >= 0; i--)
            {
                Token token = tokens[i];

                if (token.Type != TokenType.CellRef)
                {
                    continue;
                }

                if (!TryParseReference(token.Value, out int column, out int row, out string dollarColumn, out string dollarRow))
                {
                    continue;
                }

                int newColumn = columnShifter == null || dollarColumn.Length > 0 ? column : columnShifter.Shift(column);
                int newRow = rowShifter == null || dollarRow.Length > 0 ? row : rowShifter.Shift(row);

                if (newColumn == int.MinValue || newRow == int.MinValue)
                {
                    builder.Remove(token.Position, token.Value.Length);
                    builder.Insert(token.Position, ReferenceError);
                    changed = true;
                    continue;
                }

                if (newColumn == column && newRow == row)
                {
                    continue;
                }

                string newText = dollarColumn + Parser.ColumnIndexToName(newColumn) + dollarRow + (newRow + 1).ToString(CultureInfo.InvariantCulture);

                builder.Remove(token.Position, token.Value.Length);
                builder.Insert(token.Position, newText);
                changed = true;
            }

            if (!changed)
            {
                return null;
            }

            return hasEquals ? "=" + builder.ToString() : builder.ToString();
        }

        /// <summary>
        /// Splits a reference such as <c>$AB$12</c> into its column, its row (both 0 based) and the
        /// <c>$</c> flags.
        /// </summary>
        private static bool TryParseReference(string text, out int column, out int row, out string dollarColumn, out string dollarRow)
        {
            column = 0;
            row = 0;
            dollarColumn = string.Empty;
            dollarRow = string.Empty;

            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            int pos = 0;

            if (text[pos] == '$')
            {
                dollarColumn = "$";
                pos++;
            }

            int columnStart = pos;

            while (pos < text.Length && char.IsLetter(text[pos]))
            {
                pos++;
            }

            if (pos == columnStart)
            {
                return false;
            }

            string columnText = text.Substring(columnStart, pos - columnStart);

            if (pos < text.Length && text[pos] == '$')
            {
                dollarRow = "$";
                pos++;
            }

            if (pos >= text.Length)
            {
                return false;
            }

            string rowText = text.Substring(pos);

            if (!int.TryParse(rowText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int oneBasedRow) || oneBasedRow < 1)
            {
                return false;
            }

            column = Parser.ColumnNameToIndex(columnText);
            row = oneBasedRow - 1;

            return column >= 0;
        }
    }
}
