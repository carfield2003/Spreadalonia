using System;

namespace Spreadalonia.Formula
{
    /// <summary>
    /// Represents an error that occurs during formula parsing or evaluation.
    /// </summary>
    public class FormulaException : Exception
    {
        /// <summary>
        /// The position in the formula string where the error occurred (0-based),
        /// or -1 if the position is unknown.
        /// </summary>
        public int Position { get; }

        public FormulaException(string message) : base(message)
        {
            Position = -1;
        }

        public FormulaException(string message, int position) : base(message)
        {
            Position = position;
        }

        public FormulaException(string message, int position, Exception inner)
            : base(message, inner)
        {
            Position = position;
        }
    }
}
