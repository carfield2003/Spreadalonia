using System;
using System.Collections.Generic;
using System.Linq;

namespace Spreadalonia.Formula
{
    /// <summary>
    /// Registry for built-in and custom formula functions.
    /// Extensible via the Register method for user-defined business functions.
    /// </summary>
    public class FunctionRegistry
    {
        private readonly Dictionary<string, Func<List<object>, object>> _functions
            = new Dictionary<string, Func<List<object>, object>>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, Func<CustomFunctionContext, object>> _contextFunctions
            = new Dictionary<string, Func<CustomFunctionContext, object>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Creates a new FunctionRegistry with standard built-in functions.
        /// </summary>
        public FunctionRegistry()
        {
            RegisterBuiltInFunctions();
        }

        /// <summary>
        /// Registers a simple custom function. Name is case-insensitive.
        /// </summary>
        /// <param name="name">Function name (case-insensitive).</param>
        /// <param name="func">The function implementation. Receives a list of evaluated arguments.</param>
        public void Register(string name, Func<List<object>, object> func)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            string key = name.ToUpperInvariant();
            _functions[key] = func;
            _contextFunctions.Remove(key); // remove context-aware version if exists
        }

        /// <summary>
        /// Registers a context-aware custom function. Name is case-insensitive.
        /// The function receives a <see cref="CustomFunctionContext"/> with access to
        /// arguments, the calling cell position, and the ability to read other cells.
        /// </summary>
        /// <param name="name">Function name (case-insensitive).</param>
        /// <param name="func">The function implementation receiving full context.</param>
        public void Register(string name, Func<CustomFunctionContext, object> func)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            string key = name.ToUpperInvariant();
            _contextFunctions[key] = func;
            _functions.Remove(key); // remove simple version if exists
        }

        /// <summary>
        /// Calls a registered function by name.
        /// </summary>
        public object Call(string name, List<object> args, CustomFunctionContext context = null)
        {
            string key = name.ToUpperInvariant();

            // Prefer context-aware version when context is available
            if (context != null && _contextFunctions.TryGetValue(key, out var ctxFunc))
            {
                try
                {
                    return ctxFunc(context);
                }
                catch (FormulaException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new FormulaException($"Error in function '{name}': {ex.Message}");
                }
            }

            if (_functions.TryGetValue(key, out var func))
            {
                try
                {
                    return func(args);
                }
                catch (FormulaException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    throw new FormulaException($"Error in function '{name}': {ex.Message}");
                }
            }

            throw new FormulaException($"Unknown function: '{name}'");
        }

        /// <summary>
        /// Checks if a function with the given name is registered.
        /// </summary>
        public bool IsRegistered(string name)
        {
            return _functions.ContainsKey(name.ToUpperInvariant());
        }

        #region Built-in Functions

        private void RegisterBuiltInFunctions()
        {
            // Mathematical
            Register("SUM", Sum);
            Register("AVERAGE", Average);
            Register("AVG", Average);
            Register("COUNT", Count);
            Register("COUNTA", CountA);
            Register("MAX", Max);
            Register("MIN", Min);
            Register("ABS", Abs);
            Register("ROUND", Round);
            Register("SQRT", Sqrt);
            Register("POWER", PowerFunc);

            // Rounding and integer arithmetic (used by the accounting report formulas)
            Register("FLOOR", Floor);
            Register("CEILING", Ceiling);
            Register("ROUNDUP", RoundUp);
            Register("ROUNDDOWN", RoundDown);
            Register("MOD", Mod);
            Register("INT", Int);

            // Logical
            Register("IF", If);
            Register("AND", And);
            Register("OR", Or);
            Register("NOT", Not);
            Register("IFERROR", IfError);

            // Text
            Register("CONCAT", Concat);
            Register("CONCATENATE", Concat);
            Register("UPPER", Upper);
            Register("LOWER", Lower);
            Register("TRIM", Trim);
            Register("LEN", Len);
            Register("LEFT", Left);
            Register("RIGHT", Right);
            Register("MID", Mid);

            // Conditional aggregation
            Register("SUMIF", SumIf);
            Register("COUNTIF", CountIf);

            // Lookup
            Register("VLOOKUP", VLookup);
            Register("HLOOKUP", HLookup);

            // Information
            Register("ISBLANK", IsBlank);
            Register("ISNUMBER", IsNumber);
            Register("ISTEXT", IsText);
        }

        // --- Math Functions ---

        private static object Sum(List<object> args)
        {
            double sum = 0;
            foreach (var arg in args)
            {
                if (arg != null && IsNumericValue(arg))
                    sum += Convert.ToDouble(arg, System.Globalization.CultureInfo.InvariantCulture);
            }
            return sum;
        }

        private static object Average(List<object> args)
        {
            double sum = 0;
            int count = 0;
            foreach (var arg in args)
            {
                if (arg != null && IsNumericValue(arg))
                {
                    sum += Convert.ToDouble(arg, System.Globalization.CultureInfo.InvariantCulture);
                    count++;
                }
            }
            if (count == 0)
                throw new FormulaException("AVERAGE requires at least one numeric value");
            return sum / count;
        }

        private static object Count(List<object> args)
        {
            int count = 0;
            foreach (var arg in args)
            {
                if (arg != null && IsNumericValue(arg))
                    count++;
            }
            return (double)count;
        }

        private static object CountA(List<object> args)
        {
            int count = 0;
            foreach (var arg in args)
            {
                if (arg != null && !string.IsNullOrEmpty(arg.ToString()))
                    count++;
            }
            return (double)count;
        }

        private static object Max(List<object> args)
        {
            double max = double.MinValue;
            bool found = false;
            foreach (var arg in args)
            {
                if (arg != null && IsNumericValue(arg))
                {
                    double val = Convert.ToDouble(arg, System.Globalization.CultureInfo.InvariantCulture);
                    if (!found || val > max)
                    {
                        max = val;
                        found = true;
                    }
                }
            }
            if (!found)
                throw new FormulaException("MAX requires at least one numeric value");
            return max;
        }

        private static object Min(List<object> args)
        {
            double min = double.MaxValue;
            bool found = false;
            foreach (var arg in args)
            {
                if (arg != null && IsNumericValue(arg))
                {
                    double val = Convert.ToDouble(arg, System.Globalization.CultureInfo.InvariantCulture);
                    if (!found || val < min)
                    {
                        min = val;
                        found = true;
                    }
                }
            }
            if (!found)
                throw new FormulaException("MIN requires at least one numeric value");
            return min;
        }

        private static object Abs(List<object> args)
        {
            RequireArgCount("ABS", args, 1);
            return Math.Abs(ToDouble(args[0]));
        }

        private static object Round(List<object> args)
        {
            RequireArgCount("ROUND", args, 1, 2);
            double value = ToDouble(args[0]);
            int digits = args.Count > 1 ? (int)ToDouble(args[1]) : 0;
            return Math.Round(value, digits);
        }

        private static object Sqrt(List<object> args)
        {
            RequireArgCount("SQRT", args, 1);
            double val = ToDouble(args[0]);
            if (val < 0)
                throw new FormulaException("SQRT requires a non-negative number");
            return Math.Sqrt(val);
        }

        private static object PowerFunc(List<object> args)
        {
            RequireArgCount("POWER", args, 2);
            return Math.Pow(ToDouble(args[0]), ToDouble(args[1]));
        }

        private static object Floor(List<object> args)
        {
            RequireArgCount("FLOOR", args, 1, 2);
            double value = ToDouble(args[0]);
            double significance = args.Count > 1 ? ToDouble(args[1]) : 1;

            if (significance == 0)
            {
                return 0.0;
            }

            return Math.Floor(value / significance) * significance;
        }

        private static object Ceiling(List<object> args)
        {
            RequireArgCount("CEILING", args, 1, 2);
            double value = ToDouble(args[0]);
            double significance = args.Count > 1 ? ToDouble(args[1]) : 1;

            if (significance == 0)
            {
                return 0.0;
            }

            return Math.Ceiling(value / significance) * significance;
        }

        private static object RoundUp(List<object> args)
        {
            RequireArgCount("ROUNDUP", args, 1, 2);
            double value = ToDouble(args[0]);
            int digits = args.Count > 1 ? (int)ToDouble(args[1]) : 0;
            double factor = Math.Pow(10, digits);
            double scaled = value * factor;

            // Away from zero, like Excel does (Math.Round would go to the nearest even value).
            return (scaled >= 0 ? Math.Ceiling(scaled) : Math.Floor(scaled)) / factor;
        }

        private static object RoundDown(List<object> args)
        {
            RequireArgCount("ROUNDDOWN", args, 1, 2);
            double value = ToDouble(args[0]);
            int digits = args.Count > 1 ? (int)ToDouble(args[1]) : 0;
            double factor = Math.Pow(10, digits);
            double scaled = value * factor;

            // Towards zero, like Excel does.
            return (scaled >= 0 ? Math.Floor(scaled) : Math.Ceiling(scaled)) / factor;
        }

        private static object Mod(List<object> args)
        {
            RequireArgCount("MOD", args, 2);
            double divisor = ToDouble(args[1]);

            if (divisor == 0)
            {
                throw new FormulaException("MOD requires a non-zero divisor");
            }

            double value = ToDouble(args[0]);
            double result = value % divisor;

            // Excel returns a result that has the same sign as the divisor.
            if (result != 0 && (result < 0) != (divisor < 0))
            {
                result += divisor;
            }

            return result;
        }

        private static object Int(List<object> args)
        {
            RequireArgCount("INT", args, 1);
            return Math.Floor(ToDouble(args[0]));
        }

        // --- Logical Functions ---

        private static object If(List<object> args)
        {
            RequireArgCount("IF", args, 2, 3);
            bool condition = ToBool(args[0]);
            return condition ? (args[1] ?? 0.0) : (args.Count > 2 ? (args[2] ?? 0.0) : 0.0);
        }

        /// <summary>
        /// IFERROR(value, fallback). The error itself is caught while the arguments are being
        /// evaluated (see <see cref="FunctionNode.Evaluate"/>), so by the time this is called the value
        /// is known to be valid and is simply returned.
        /// </summary>
        private static object IfError(List<object> args)
        {
            RequireArgCount("IFERROR", args, 2);
            return args[0] ?? 0.0;
        }

        private static object And(List<object> args)
        {
            foreach (var arg in args)
            {
                if (!ToBool(arg))
                    return false;
            }
            return true;
        }

        private static object Or(List<object> args)
        {
            foreach (var arg in args)
            {
                if (ToBool(arg))
                    return true;
            }
            return false;
        }

        private static object Not(List<object> args)
        {
            RequireArgCount("NOT", args, 1);
            return !ToBool(args[0]);
        }

        // --- Text Functions ---

        private static object Concat(List<object> args)
        {
            var parts = new List<string>();
            foreach (var arg in args)
            {
                parts.Add(arg?.ToString() ?? "");
            }
            return string.Concat(parts);
        }

        private static object Upper(List<object> args)
        {
            RequireArgCount("UPPER", args, 1);
            return (args[0]?.ToString() ?? "").ToUpperInvariant();
        }

        private static object Lower(List<object> args)
        {
            RequireArgCount("LOWER", args, 1);
            return (args[0]?.ToString() ?? "").ToLowerInvariant();
        }

        private static object Trim(List<object> args)
        {
            RequireArgCount("TRIM", args, 1);
            return (args[0]?.ToString() ?? "").Trim();
        }

        private static object Len(List<object> args)
        {
            RequireArgCount("LEN", args, 1);
            return (double)(args[0]?.ToString() ?? "").Length;
        }

        private static object Left(List<object> args)
        {
            RequireArgCount("LEFT", args, 1, 2);
            string text = args[0]?.ToString() ?? "";
            int count = args.Count > 1 ? (int)ToDouble(args[1]) : 1;
            if (count >= text.Length) return text;
            return text.Substring(0, count);
        }

        private static object Right(List<object> args)
        {
            RequireArgCount("RIGHT", args, 1, 2);
            string text = args[0]?.ToString() ?? "";
            int count = args.Count > 1 ? (int)ToDouble(args[1]) : 1;
            if (count >= text.Length) return text;
            return text.Substring(text.Length - count);
        }

        private static object Mid(List<object> args)
        {
            RequireArgCount("MID", args, 3);
            string text = args[0]?.ToString() ?? "";
            int start = (int)ToDouble(args[1]) - 1; // 1-based
            int count = (int)ToDouble(args[2]);
            if (start < 0) start = 0;
            if (start >= text.Length) return "";
            if (start + count > text.Length) count = text.Length - start;
            return text.Substring(start, count);
        }

        // --- Lookup Functions ---

        // --- Conditional aggregation ---

        /// <summary>
        /// SUMIF(range, criteria, [sum_range]): adds the values of the sum range (or of the range
        /// itself when no sum range is given) whose counterpart in the criteria range matches.
        /// </summary>
        private static object SumIf(List<object> args)
        {
            RequireArgCount("SUMIF", args, 2, 3);

            // SUMIF(range, criteria, [sum_range]): the criteria come second, the optional sum range
            // third (when it is omitted the range itself is summed).
            List<object> criteriaRange = AsList(args[0]);
            object criteria = args[1];
            List<object> sumRange = args.Count > 2 ? AsList(args[2]) : criteriaRange;

            double sum = 0;

            for (int i = 0; i < criteriaRange.Count && i < sumRange.Count; i++)
            {
                if (MatchesCriteria(criteriaRange[i], criteria) && IsNumericValue(sumRange[i]))
                {
                    sum += ToDouble(sumRange[i]);
                }
            }

            return sum;
        }

        /// <summary>
        /// COUNTIF(range, criteria): counts the values of the range that match the criteria.
        /// </summary>
        private static object CountIf(List<object> args)
        {
            RequireArgCount("COUNTIF", args, 2);

            List<object> range = AsList(args[0]);
            object criteria = args[1];

            int count = 0;

            foreach (object value in range)
            {
                if (MatchesCriteria(value, criteria))
                {
                    count++;
                }
            }

            return (double)count;
        }

        /// <summary>
        /// Returns the argument as a list: a range arrives as a flat list, a single value as a list of
        /// one element.
        /// </summary>
        private static List<object> AsList(object value)
        {
            if (value is List<object> list)
            {
                return list;
            }

            return new List<object> { value };
        }

        /// <summary>
        /// Checks a value against an Excel criteria, which is either a plain value (compared for
        /// equality) or a comparison introduced by one of the operators =, &lt;&gt;, &lt;, &gt;, &lt;=, &gt;=.
        /// Text comparisons are case-insensitive.
        /// </summary>
        private static bool MatchesCriteria(object value, object criteria)
        {
            string text = value == null ? "" : Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "";
            string pattern = criteria == null ? "" : Convert.ToString(criteria, System.Globalization.CultureInfo.InvariantCulture) ?? "";

            string op = "=";

            if (pattern.StartsWith(">=", StringComparison.Ordinal) || pattern.StartsWith("<=", StringComparison.Ordinal) ||
                pattern.StartsWith("<>", StringComparison.Ordinal))
            {
                op = pattern.Substring(0, 2);
                pattern = pattern.Substring(2);
            }
            else if (pattern.StartsWith(">", StringComparison.Ordinal) || pattern.StartsWith("<", StringComparison.Ordinal) ||
                     pattern.StartsWith("=", StringComparison.Ordinal))
            {
                op = pattern.Substring(0, 1);
                pattern = pattern.Substring(1);
            }

            // A numeric comparison is done as numbers, anything else as text.
            if (IsNumericValue(value) &&
                double.TryParse(pattern, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double expected))
            {
                double actual = ToDouble(value);

                switch (op)
                {
                    case ">": return actual > expected;
                    case "<": return actual < expected;
                    case ">=": return actual >= expected;
                    case "<=": return actual <= expected;
                    case "<>": return actual != expected;
                    default: return actual == expected;
                }
            }

            int comparison = string.Compare(text, pattern, StringComparison.OrdinalIgnoreCase);

            switch (op)
            {
                case ">": return comparison > 0;
                case "<": return comparison < 0;
                case ">=": return comparison >= 0;
                case "<=": return comparison <= 0;
                case "<>": return comparison != 0;
                default: return comparison == 0;
            }
        }

        // --- Lookup Functions ---

        private static object VLookup(List<object> args)
        {
            // Simplified VLOOKUP - only supports 2D data passed as range
            RequireArgCount("VLOOKUP", args, 2, 3);
            object lookupValue = args[0];
            object range = args[1];
            int colIndex = args.Count > 2 ? (int)ToDouble(args[2]) : 1;

            // For now, VLOOKUP only works when range is a list of lists (from a range reference)
            throw new FormulaException("VLOOKUP with direct cell ranges is not yet fully supported. Use it with explicit values.");
        }

        private static object HLookup(List<object> args)
        {
            RequireArgCount("HLOOKUP", args, 2, 3);
            throw new FormulaException("HLOOKUP with direct cell ranges is not yet fully supported.");
        }

        // --- Information Functions ---

        private static object IsBlank(List<object> args)
        {
            RequireArgCount("ISBLANK", args, 1);
            return args[0] == null || string.IsNullOrEmpty(args[0].ToString());
        }

        private static object IsNumber(List<object> args)
        {
            RequireArgCount("ISNUMBER", args, 1);
            return args[0] is double || args[0] is int || args[0] is float ||
                   (args[0] is string s && double.TryParse(s,
                       System.Globalization.NumberStyles.Float,
                       System.Globalization.CultureInfo.InvariantCulture,
                       out _));
        }

        private static object IsText(List<object> args)
        {
            RequireArgCount("ISTEXT", args, 1);
            return args[0] is string;
        }

        #endregion

        #region Helpers

        private static void RequireArgCount(string funcName, List<object> args, int min, int? max = null)
        {
            if (args.Count < min)
                throw new FormulaException(
                    $"{funcName} requires at least {min} argument(s), got {args.Count}");

            if (max.HasValue && args.Count > max.Value)
                throw new FormulaException(
                    $"{funcName} accepts at most {max.Value} argument(s), got {args.Count}");
        }

        private static double ToDouble(object val)
        {
            if (val == null) return 0.0;
            if (val is double d) return d;
            if (val is int i) return i;
            if (val is bool b) return b ? 1.0 : 0.0;
            return Convert.ToDouble(val, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static bool ToBool(object val)
        {
            if (val == null) return false;
            if (val is bool b) return b;
            if (val is double d) return d != 0.0;
            if (val is int i) return i != 0;
            string s = val.ToString();
            return !string.IsNullOrEmpty(s) &&
                   !string.Equals(s, "0", StringComparison.Ordinal) &&
                   !string.Equals(s, "FALSE", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNumericValue(object val)
        {
            if (val is double || val is int || val is float || val is long)
                return true;
            if (val is string s)
                return double.TryParse(s,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out _);
            return false;
        }

        #endregion
    }
}
