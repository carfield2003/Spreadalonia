using System;
using System.Collections.Generic;

namespace Spreadalonia.Formula
{
    /// <summary>
    /// Provides rich context to custom formula functions during evaluation.
    /// Gives access to arguments, the calling cell position, and the ability
    /// to read values from other cells in the spreadsheet.
    /// </summary>
    public class CustomFunctionContext
    {
        /// <summary>
        /// The evaluated argument values passed to the function.
        /// Range references (e.g. A1:B3) are already flattened into this list.
        /// </summary>
        public IReadOnlyList<object> Args { get; }

        /// <summary>
        /// The column index (0-based) of the cell that contains this formula.
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// The row index (0-based) of the cell that contains this formula.
        /// </summary>
        public int Row { get; }

        /// <summary>
        /// The (col, row) of the cell that contains this formula.
        /// </summary>
        public (int Col, int Row) Cell => (Column, Row);

        /// <summary>
        /// Reads the evaluated value of any cell in the spreadsheet.
        /// Returns null if the cell is empty.
        /// </summary>
        public Func<int, int, object> GetCellValue { get; }

        /// <summary>
        /// Creates a new context for custom function evaluation.
        /// </summary>
        public CustomFunctionContext(
            IReadOnlyList<object> args,
            int column,
            int row,
            Func<int, int, object> getCellValue)
        {
            Args = args ?? new List<object>().AsReadOnly();
            Column = column;
            Row = row;
            GetCellValue = getCellValue;
        }

        /// <summary>
        /// Reads the evaluated value of a named cell (e.g. "A1", "B5").
        /// Returns null if the cell is empty or the reference is invalid.
        /// </summary>
        public object GetValue(string cellRef)
        {
            try
            {
                // Parse cell reference like "A1" or "AA123"
                int col = -1;
                int row = -1;
                int i = 0;

                // Parse column letters
                while (i < cellRef.Length && char.IsLetter(cellRef[i]))
                {
                    col = (col + 1) * 26 + (char.ToUpperInvariant(cellRef[i]) - 'A');
                    i++;
                }

                // Parse row digits
                if (i < cellRef.Length && char.IsDigit(cellRef[i]))
                {
                    int rowStart = i;
                    while (i < cellRef.Length && char.IsDigit(cellRef[i]))
                        i++;
                    if (int.TryParse(cellRef.Substring(rowStart, i - rowStart), out int parsedRow))
                        row = parsedRow - 1; // 1-based to 0-based
                }

                if (col >= 0 && row >= 0 && i == cellRef.Length)
                    return GetCellValue?.Invoke(col, row);
            }
            catch
            {
                // Invalid cell reference
            }

            return null;
        }
    }
}
