namespace Spreadalonia.Formula
{
    /// <summary>
    /// Types of tokens produced by the formula lexer.
    /// </summary>
    public enum TokenType
    {
        // Literals
        Number,
        String,
        Boolean,

        // Cell references
        CellRef,
        RangeRef,

        // Arithmetic operators
        Plus,
        Minus,
        Mul,
        Div,
        Power,

        // Comparison operators
        Less,
        Greater,
        LessEq,
        GreaterEq,
        Eq,
        NotEq,

        // String operator
        Ampersand,

        // Delimiters
        LParen,
        RParen,
        Comma,
        Colon,

        // Other
        Function,
        Percent,

        // End of input
        Eof,

        // Error
        Error
    }
}
