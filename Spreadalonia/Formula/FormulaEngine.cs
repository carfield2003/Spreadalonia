using System;
using System.Collections.Generic;

namespace Spreadalonia.Formula
{
    /// <summary>
    /// Central engine for formula parsing, evaluation, and dependency management.
    /// Integrates lexer, parser, evaluator, function registry, and dependency graph.
    /// </summary>
    public class FormulaEngine
    {
        /// <summary>
        /// The function registry containing built-in and custom functions.
        /// </summary>
        public FunctionRegistry Functions { get; } = new FunctionRegistry();

        /// <summary>
        /// The dependency graph tracking cell relationships.
        /// </summary>
        public DependencyGraph Dependencies { get; } = new DependencyGraph();

        // Cached ASTs for formula cells to avoid re-parsing
        private readonly Dictionary<(int, int), AstNode> _cachedAsts
            = new Dictionary<(int, int), AstNode>();

        // Delegate to get cell data from the spreadsheet
        private Func<int, int, CellData> _getCellData;

        /// <summary>
        /// Sets the data accessor for cell value retrieval.
        /// </summary>
        public void SetDataAccessor(Func<int, int, CellData> getCellData)
        {
            _getCellData = getCellData;
        }

        /// <summary>
        /// Parses a formula string and returns the AST.
        /// </summary>
        public AstNode Parse(string formula)
        {
            if (string.IsNullOrEmpty(formula))
                return new StringNode(string.Empty);

            var lexer = new Lexer(formula);
            var tokens = lexer.Tokenize();
            var parser = new Parser(tokens);
            return parser.Parse();
        }

        /// <summary>
        /// Evaluates a formula for a specific cell, storing the result and updating dependencies.
        /// </summary>
        /// <param name="cell">The cell being evaluated.</param>
        /// <param name="formula">The formula text (without leading =).</param>
        /// <returns>The CellData with the formula and computed value.</returns>
        public CellData Evaluate((int, int) cell, string formula)
        {
            try
            {
                // Parse the formula
                var ast = Parse(formula);

                // Cache the AST
                _cachedAsts[cell] = ast;

                // Update dependency graph
                Dependencies.UpdateDependencies(cell, ast);

                // Evaluate
                var ctx = CreateContext();
                object result = EvaluateWithCircularCheck(cell, ast, ctx);

                return new CellData
                {
                    RawText = "=" + formula,
                    Formula = formula,
                    CachedValue = result,
                    ValueType = CellData.InferType(result)
                };
            }
            catch (FormulaException ex)
            {
                // Clean up dependencies on error
                Dependencies.RemoveDependencies(cell);
                _cachedAsts.Remove(cell);

                return CellData.FromError(formula, ex.Message);
            }
            catch (Exception ex)
            {
                Dependencies.RemoveDependencies(cell);
                _cachedAsts.Remove(cell);

                return CellData.FromError(formula, ex.Message);
            }
        }

        /// <summary>
        /// Re-evaluates a formula cell using its cached AST.
        /// </summary>
        public CellData ReEvaluate((int, int) cell)
        {
            if (!_cachedAsts.TryGetValue(cell, out var ast))
                return null;

            try
            {
                var ctx = CreateContext();
                object result = EvaluateWithCircularCheck(cell, ast, ctx);

                return new CellData
                {
                    Formula = GetFormulaFromCell(cell),
                    RawText = "=" + (GetFormulaFromCell(cell) ?? ""),
                    CachedValue = result,
                    ValueType = CellData.InferType(result)
                };
            }
            catch (FormulaException ex)
            {
                return CellData.FromError(GetFormulaFromCell(cell) ?? "", ex.Message);
            }
            catch (Exception ex)
            {
                return CellData.FromError(GetFormulaFromCell(cell) ?? "", ex.Message);
            }
        }

        /// <summary>
        /// Recalculates all affected cells when a cell value changes.
        /// </summary>
        /// <param name="changedCell">The cell that changed.</param>
        /// <returns>Dictionary of cells that were recalculated with their new CellData.</returns>
        public Dictionary<(int, int), CellData> CascadeRecalculate((int, int) changedCell)
        {
            var recalcOrder = Dependencies.GetRecalculationOrder(changedCell);
            var results = new Dictionary<(int, int), CellData>();

            foreach (var cell in recalcOrder)
            {
                if (_cachedAsts.ContainsKey(cell))
                {
                    var newData = ReEvaluate(cell);
                    if (newData != null)
                    {
                        results[cell] = newData;
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Removes a cell from the engine (formula and dependencies).
        /// </summary>
        public void RemoveCell((int, int) cell)
        {
            _cachedAsts.Remove(cell);
            Dependencies.RemoveDependencies(cell);
        }

        /// <summary>
        /// Clears all cached data.
        /// </summary>
        public void Clear()
        {
            _cachedAsts.Clear();
            Dependencies.Clear();
        }

        #region Private Helpers

        private EvaluationContext CreateContext()
        {
            var currentCell = _currentEvaluatingCell;
            return new EvaluationContext
            {
                GetCellData = (col, row) =>
                {
                    return _getCellData?.Invoke(col, row);
                },
                CallFunction = (name, args) =>
                {
                    var context = new CustomFunctionContext(
                        args.AsReadOnly(),
                        currentCell.Item1,
                        currentCell.Item2,
                        (c, r) => _getCellData?.Invoke(c, r)?.CachedValue
                    );
                    return Functions.Call(name, args, context);
                }
            };
        }

        private string GetFormulaFromCell((int, int) cell)
        {
            var cellData = _getCellData?.Invoke(cell.Item1, cell.Item2);
            return cellData?.Formula;
        }

        // Tracks the cell currently being evaluated, so custom functions
        // can receive cell context via CustomFunctionContext.
        private (int, int) _currentEvaluatingCell;

        private object EvaluateWithCircularCheck((int, int) cell, AstNode ast, EvaluationContext ctx)
        {
            // Check for circular reference
            if (ctx.EvaluatingCells.Contains(cell))
            {
                throw new FormulaException(
                    $"Circular reference detected at {Parser.ColumnIndexToName(cell.Item1)}{cell.Item2 + 1}");
            }

            var previousCell = _currentEvaluatingCell;
            _currentEvaluatingCell = cell;

            ctx.EvaluatingCells.Add(cell);
            try
            {
                return ast.Evaluate(ctx);
            }
            finally
            {
                ctx.EvaluatingCells.Remove(cell);
                _currentEvaluatingCell = previousCell;
            }
        }

        #endregion
    }
}
