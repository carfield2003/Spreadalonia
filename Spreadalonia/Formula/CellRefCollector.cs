using System;
using System.Collections.Generic;

namespace Spreadalonia.Formula
{
    /// <summary>
    /// Walks an AST to collect all cell references.
    /// Used for building the dependency graph.
    /// </summary>
    public static class CellRefCollector
    {
        /// <summary>
        /// Collects all cell references from an AST node.
        /// </summary>
        public static HashSet<(int, int)> Collect(AstNode node)
        {
            var refs = new HashSet<(int, int)>();
            CollectRecursive(node, refs);
            return refs;
        }

        private static void CollectRecursive(AstNode node, HashSet<(int, int)> refs)
        {
            if (node == null) return;

            if (node is CellRefNode cellRef)
            {
                refs.Add((cellRef.Column, cellRef.Row));
            }
            else if (node is RangeRefNode rangeRef)
            {
                for (int row = rangeRef.Start.Row; row <= rangeRef.End.Row; row++)
                {
                    for (int col = rangeRef.Start.Column; col <= rangeRef.End.Column; col++)
                    {
                        refs.Add((col, row));
                    }
                }
            }
            else if (node is BinaryOpNode binOp)
            {
                CollectRecursive(binOp.Left, refs);
                CollectRecursive(binOp.Right, refs);
            }
            else if (node is UnaryOpNode unaryOp)
            {
                CollectRecursive(unaryOp.Operand, refs);
            }
            else if (node is FunctionNode funcNode)
            {
                foreach (var arg in funcNode.Arguments)
                {
                    CollectRecursive(arg, refs);
                }
            }
        }
    }
}
