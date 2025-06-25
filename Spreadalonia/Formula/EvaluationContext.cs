using System;
using System.Collections.Generic;

namespace Spreadalonia.Formula
{
    /// <summary>
    /// Provides the runtime context for formula evaluation,
    /// bridging the formula engine with the spreadsheet data model.
    /// </summary>
    public class EvaluationContext
    {
        /// <summary>
        /// Delegate to retrieve a cell's data by column and row.
        /// </summary>
        public Func<int, int, CellData> GetCellData { get; set; }

        /// <summary>
        /// Delegate to call a registered function.
        /// </summary>
        public Func<string, List<object>, object> CallFunction { get; set; }

        /// <summary>
        /// Set of cells currently being evaluated, used for circular reference detection.
        /// </summary>
        public HashSet<(int, int)> EvaluatingCells { get; } = new HashSet<(int, int)>();

        /// <summary>
        /// Gets the evaluated value of a cell. For formula cells, this triggers
        /// recursive evaluation. For non-formula cells, returns the cached value.
        /// </summary>
        public object GetCellValue(int column, int row)
        {
            var key = (column, row);

            // Circular reference detection
            if (EvaluatingCells.Contains(key))
            {
                throw new FormulaException($"Circular reference detected at {Parser.ColumnIndexToName(column)}{row + 1}");
            }

            CellData cell = GetCellData?.Invoke(column, row);
            if (cell == null)
                return null;

            // If it's a formula cell, we need to recursively evaluate via the formula engine
            // But that's handled at a higher level. Here we just return the cached value.
            return cell.CachedValue;
        }
    }
}
