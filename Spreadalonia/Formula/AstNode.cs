namespace Spreadalonia.Formula
{
    /// <summary>
    /// Abstract base class for all AST nodes in the formula parse tree.
    /// </summary>
    public abstract class AstNode
    {
        /// <summary>
        /// Evaluates this node and returns the result.
        /// </summary>
        public abstract object Evaluate(EvaluationContext ctx);
    }

    /// <summary>
    /// Represents a numeric literal (e.g., 42, 3.14, 1e5).
    /// </summary>
    public class NumberNode : AstNode
    {
        public double Value { get; }

        public NumberNode(double value)
        {
            Value = value;
        }

        public override object Evaluate(EvaluationContext ctx)
        {
            return Value;
        }

        public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Represents a string literal (e.g., "hello").
    /// </summary>
    public class StringNode : AstNode
    {
        public string Value { get; }

        public StringNode(string value)
        {
            Value = value;
        }

        public override object Evaluate(EvaluationContext ctx)
        {
            return Value;
        }

        public override string ToString() => "\"" + Value + "\"";
    }

    /// <summary>
    /// Represents a boolean literal (TRUE or FALSE).
    /// </summary>
    public class BooleanNode : AstNode
    {
        public bool Value { get; }

        public BooleanNode(bool value)
        {
            Value = value;
        }

        public override object Evaluate(EvaluationContext ctx)
        {
            return Value;
        }

        public override string ToString() => Value ? "TRUE" : "FALSE";
    }

    /// <summary>
    /// Represents a reference to a single cell (e.g., A1, $B$2).
    /// </summary>
    public class CellRefNode : AstNode
    {
        public int Column { get; }
        public int Row { get; }
        public bool IsAbsoluteColumn { get; }
        public bool IsAbsoluteRow { get; }
        public string OriginalText { get; }

        public CellRefNode(int column, int row, bool isAbsoluteColumn, bool isAbsoluteRow, string originalText)
        {
            Column = column;
            Row = row;
            IsAbsoluteColumn = isAbsoluteColumn;
            IsAbsoluteRow = isAbsoluteRow;
            OriginalText = originalText;
        }

        public override object Evaluate(EvaluationContext ctx)
        {
            return ctx.GetCellValue(Column, Row);
        }

        public override string ToString() => OriginalText;
    }

    /// <summary>
    /// Represents a reference to a range of cells (e.g., A1:B5).
    /// </summary>
    public class RangeRefNode : AstNode
    {
        public CellRefNode Start { get; }
        public CellRefNode End { get; }

        public RangeRefNode(CellRefNode start, CellRefNode end)
        {
            Start = start;
            End = end;
        }

        public override object Evaluate(EvaluationContext ctx)
        {
            // Returns a list of values from the range
            var values = new System.Collections.Generic.List<object>();
            for (int row = Start.Row; row <= End.Row; row++)
            {
                for (int col = Start.Column; col <= End.Column; col++)
                {
                    values.Add(ctx.GetCellValue(col, row));
                }
            }
            return values;
        }

        public override string ToString() => Start.ToString() + ":" + End.ToString();
    }

    /// <summary>
    /// Represents a binary operation (e.g., A1 + B1, 5 * 3).
    /// </summary>
    public class BinaryOpNode : AstNode
    {
        public AstNode Left { get; }
        public AstNode Right { get; }
        public TokenType Operator { get; }

        public BinaryOpNode(AstNode left, TokenType op, AstNode right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        public override object Evaluate(EvaluationContext ctx)
        {
            object leftVal = Left.Evaluate(ctx);
            object rightVal = Right.Evaluate(ctx);

            switch (Operator)
            {
                case TokenType.Plus:
                    return EvaluatePlus(leftVal, rightVal);

                case TokenType.Minus:
                    return EvaluateMinus(leftVal, rightVal);

                case TokenType.Mul:
                    return EvaluateMul(leftVal, rightVal);

                case TokenType.Div:
                    return EvaluateDiv(leftVal, rightVal);

                case TokenType.Power:
                    return System.Math.Pow(ToNumber(leftVal), ToNumber(rightVal));

                case TokenType.Ampersand:
                    return (leftVal?.ToString() ?? "") + (rightVal?.ToString() ?? "");

                case TokenType.Eq:
                    return Compare(leftVal, rightVal) == 0;

                case TokenType.NotEq:
                    return Compare(leftVal, rightVal) != 0;

                case TokenType.Less:
                    return Compare(leftVal, rightVal) < 0;

                case TokenType.LessEq:
                    return Compare(leftVal, rightVal) <= 0;

                case TokenType.Greater:
                    return Compare(leftVal, rightVal) > 0;

                case TokenType.GreaterEq:
                    return Compare(leftVal, rightVal) >= 0;

                default:
                    throw new FormulaException($"Unknown binary operator: {Operator}");
            }
        }

        private static object EvaluatePlus(object left, object right)
        {
            // String concatenation if either operand is a string
            if (left is string || right is string)
                return (left?.ToString() ?? "") + (right?.ToString() ?? "");

            return ToNumber(left) + ToNumber(right);
        }

        private static object EvaluateMinus(object left, object right)
        {
            return ToNumber(left) - ToNumber(right);
        }

        private static object EvaluateMul(object left, object right)
        {
            return ToNumber(left) * ToNumber(right);
        }

        private static object EvaluateDiv(object left, object right)
        {
            double divisor = ToNumber(right);
            if (divisor == 0.0)
                throw new FormulaException("Division by zero");
            return ToNumber(left) / divisor;
        }

        private static double ToNumber(object val)
        {
            if (val == null)
                return 0.0;
            if (val is double d)
                return d;
            if (val is int i)
                return i;
            if (val is bool b)
                return b ? 1.0 : 0.0;
            if (double.TryParse(val.ToString(),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double result))
                return result;
            throw new FormulaException($"Cannot convert '{val}' to a number");
        }

        private static int Compare(object left, object right)
        {
            if (left == null && right == null) return 0;
            if (left == null) return -1;
            if (right == null) return 1;

            // Try numeric comparison first
            if ((left is double || left is int) && (right is double || right is int))
            {
                double l = ToNumber(left);
                double r = ToNumber(right);
                return l.CompareTo(r);
            }

            // String comparison
            return string.Compare(left.ToString(), right.ToString(),
                System.StringComparison.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            string op;
            switch (Operator)
            {
                case TokenType.Plus: op = "+"; break;
                case TokenType.Minus: op = "-"; break;
                case TokenType.Mul: op = "*"; break;
                case TokenType.Div: op = "/"; break;
                case TokenType.Power: op = "^"; break;
                case TokenType.Ampersand: op = "&"; break;
                case TokenType.Eq: op = "="; break;
                case TokenType.NotEq: op = "<>"; break;
                case TokenType.Less: op = "<"; break;
                case TokenType.LessEq: op = "<="; break;
                case TokenType.Greater: op = ">"; break;
                case TokenType.GreaterEq: op = ">="; break;
                default: op = "?"; break;
            }
            return $"({Left} {op} {Right})";
        }
    }

    /// <summary>
    /// Represents a unary operation (e.g., -A1, +5).
    /// </summary>
    public class UnaryOpNode : AstNode
    {
        public AstNode Operand { get; }
        public TokenType Operator { get; }

        public UnaryOpNode(TokenType op, AstNode operand)
        {
            Operator = op;
            Operand = operand;
        }

        public override object Evaluate(EvaluationContext ctx)
        {
            object val = Operand.Evaluate(ctx);

            if (Operator == TokenType.Minus)
            {
                if (val is double d) return -d;
                if (val is int i) return (double)(-i);
                double num = ConvertToDouble(val);
                return -num;
            }

            // Unary plus is a no-op
            return val;
        }

        private static double ConvertToDouble(object val)
        {
            if (val == null) return 0.0;
            if (val is double d) return d;
            if (val is int i) return i;
            if (val is bool b) return b ? 1.0 : 0.0;
            if (double.TryParse(val.ToString(),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double result))
                return result;
            throw new FormulaException($"Cannot convert '{val}' to a number");
        }

        public override string ToString()
        {
            string op = Operator == TokenType.Minus ? "-" : "+";
            return op + Operand;
        }
    }

    /// <summary>
    /// Represents a function call (e.g., SUM(A1:A10), COUNT(1,2,3)).
    /// </summary>
    public class FunctionNode : AstNode
    {
        public string FunctionName { get; }
        public System.Collections.Generic.List<AstNode> Arguments { get; }

        public FunctionNode(string functionName, System.Collections.Generic.List<AstNode> arguments)
        {
            FunctionName = functionName;
            Arguments = arguments ?? new System.Collections.Generic.List<AstNode>();
        }

        public override object Evaluate(EvaluationContext ctx)
        {
            // Evaluate all arguments
            var argValues = new System.Collections.Generic.List<object>();
            foreach (var arg in Arguments)
            {
                object val = arg.Evaluate(ctx);

                // If the argument is a range result (list), flatten it
                if (val is System.Collections.Generic.List<object> list)
                {
                    argValues.AddRange(list);
                }
                else
                {
                    argValues.Add(val);
                }
            }

            // Call the function
            return ctx.CallFunction(FunctionName, argValues);
        }

        public override string ToString()
        {
            var args = string.Join(", ", Arguments);
            return $"{FunctionName}({args})";
        }
    }
}
