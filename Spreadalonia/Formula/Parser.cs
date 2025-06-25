using System;
using System.Collections.Generic;

namespace Spreadalonia.Formula
{
    /// <summary>
    /// Recursive descent parser that converts a token stream into an AST.
    ///
    /// Grammar (in order of precedence, lowest to highest):
    ///   Expression  -> Comparison
    ///   Comparison  -> Concat (('='|'&lt;&gt;'|'&lt;'|'&lt;='|'&gt;'|'&gt;=') Concat)?
    ///   Concat      -> AddSub ('&amp;' AddSub)*
    ///   AddSub      -> MulDiv (('+'|'-') MulDiv)*
    ///   MulDiv      -> Percent (('*'|'/') Percent)*
    ///   Percent     -> Unary ('%')?
    ///   Unary       -> ('+'|'-') Unary | Power
    ///   Power       -> Primary ('^' Unary)?
    ///   Primary     -> Number | String | Boolean | CellRef | '(' Expression ')' | Function
    ///   Range       -> CellRef ':' CellRef
    /// </summary>
    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _pos;

        public Parser(List<Token> tokens)
        {
            _tokens = tokens ?? new List<Token>();
            _pos = 0;
        }

        public AstNode Parse()
        {
            if (_tokens.Count == 0)
                return new StringNode(string.Empty);

            AstNode node = ParseExpression();

            // Check for unexpected trailing tokens
            if (_pos < _tokens.Count)
            {
                var unexpected = _tokens[_pos];
                if (unexpected.Type != TokenType.Eof)
                    throw new FormulaException(
                        $"Unexpected token: '{unexpected.Value}'", unexpected.Position);
            }

            return node;
        }

        private Token Peek()
        {
            if (_pos < _tokens.Count)
                return _tokens[_pos];
            return new Token(TokenType.Eof, string.Empty, -1);
        }

        private Token Advance()
        {
            if (_pos < _tokens.Count)
                return _tokens[_pos++];
            return new Token(TokenType.Eof, string.Empty, -1);
        }

        private Token Expect(TokenType type)
        {
            Token token = Advance();
            if (token.Type != type)
                throw new FormulaException(
                    $"Expected {type} but got {token.Type} ('{token.Value}')", token.Position);
            return token;
        }

        // Expression -> Comparison
        private AstNode ParseExpression()
        {
            return ParseComparison();
        }

        // Comparison -> Concat (('='|'<>'|'<'|'<='|'>'|'>=') Concat)?
        private AstNode ParseComparison()
        {
            AstNode left = ParseConcat();

            TokenType op = Peek().Type;
            if (op == TokenType.Eq || op == TokenType.NotEq ||
                op == TokenType.Less || op == TokenType.LessEq ||
                op == TokenType.Greater || op == TokenType.GreaterEq)
            {
                Advance();
                AstNode right = ParseConcat();
                return new BinaryOpNode(left, op, right);
            }

            return left;
        }

        // Concat -> AddSub ('&' AddSub)*
        private AstNode ParseConcat()
        {
            AstNode left = ParseAddSub();

            while (Peek().Type == TokenType.Ampersand)
            {
                Advance();
                AstNode right = ParseAddSub();
                left = new BinaryOpNode(left, TokenType.Ampersand, right);
            }

            return left;
        }

        // AddSub -> MulDiv (('+'|'-') MulDiv)*
        private AstNode ParseAddSub()
        {
            AstNode left = ParseMulDiv();

            while (true)
            {
                TokenType op = Peek().Type;
                if (op != TokenType.Plus && op != TokenType.Minus)
                    break;

                Advance();
                AstNode right = ParseMulDiv();
                left = new BinaryOpNode(left, op, right);
            }

            return left;
        }

        // MulDiv -> Percent (('*'|'/') Percent)*
        private AstNode ParseMulDiv()
        {
            AstNode left = ParsePercent();

            while (true)
            {
                TokenType op = Peek().Type;
                if (op != TokenType.Mul && op != TokenType.Div)
                    break;

                Advance();
                AstNode right = ParsePercent();
                left = new BinaryOpNode(left, op, right);
            }

            return left;
        }

        // Percent -> Unary ('%')?
        private AstNode ParsePercent()
        {
            AstNode node = ParseUnary();

            if (Peek().Type == TokenType.Percent)
            {
                Advance();
                // A% is equivalent to A/100
                node = new BinaryOpNode(node, TokenType.Div, new NumberNode(100.0));
            }

            return node;
        }

        // Unary -> ('+'|'-') Unary | Power
        private AstNode ParseUnary()
        {
            TokenType op = Peek().Type;
            if (op == TokenType.Plus || op == TokenType.Minus)
            {
                Advance();
                AstNode operand = ParseUnary();
                return new UnaryOpNode(op, operand);
            }

            return ParsePower();
        }

        // Power -> Primary ('^' Unary)?
        private AstNode ParsePower()
        {
            AstNode left = ParsePrimary();

            if (Peek().Type == TokenType.Power)
            {
                Advance();
                // Right-associative
                AstNode right = ParseUnary();
                return new BinaryOpNode(left, TokenType.Power, right);
            }

            return left;
        }

        // Primary -> Number | String | Boolean | CellRef [':' CellRef] | '(' Expression ')' | Function
        private AstNode ParsePrimary()
        {
            Token token = Peek();

            switch (token.Type)
            {
                case TokenType.Number:
                    Advance();
                    // Try parsing the value
                    if (double.TryParse(token.Value,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double num))
                    {
                        return new NumberNode(num);
                    }
                    throw new FormulaException($"Invalid number: {token.Value}", token.Position);

                case TokenType.String:
                    Advance();
                    return new StringNode(token.Value);

                case TokenType.Boolean:
                    Advance();
                    return new BooleanNode(
                        string.Equals(token.Value, "TRUE", StringComparison.OrdinalIgnoreCase));

                case TokenType.CellRef:
                    Advance();
                    AstNode cellRef = ParseCellRef(token);

                    // Check for range reference: CellRef : CellRef
                    if (Peek().Type == TokenType.Colon)
                    {
                        Advance(); // consume ':'
                        Token endToken = Peek();
                        if (endToken.Type == TokenType.CellRef)
                        {
                            Advance();
                            AstNode endRef = ParseCellRef(endToken);
                            return new RangeRefNode(
                                (CellRefNode)cellRef, (CellRefNode)endRef);
                        }
                        throw new FormulaException(
                            "Expected cell reference after ':'", endToken.Position);
                    }

                    return cellRef;

                case TokenType.Function:
                    Advance();
                    return ParseFunctionCall(token);

                case TokenType.LParen:
                    Advance(); // consume '('
                    AstNode expr = ParseExpression();
                    Expect(TokenType.RParen);
                    return expr;

                default:
                    throw new FormulaException(
                        $"Unexpected token: '{token.Value}' ({token.Type})", token.Position);
            }
        }

        private static CellRefNode ParseCellRef(Token token)
        {
            string text = token.Value;
            bool isAbsoluteCol = false;
            bool isAbsoluteRow = false;
            int idx = 0;

            if (text.Length > 0 && text[idx] == '$')
            {
                isAbsoluteCol = true;
                idx++;
            }

            // Read column letters
            int colStart = idx;
            while (idx < text.Length && char.IsLetter(text[idx]))
                idx++;
            string colStr = text.Substring(colStart, idx - colStart);

            if (text.Length > idx && text[idx] == '$')
            {
                isAbsoluteRow = true;
                idx++;
            }

            string rowStr = text.Substring(idx);

            int col = ColumnNameToIndex(colStr);
            int row = int.Parse(rowStr, System.Globalization.CultureInfo.InvariantCulture) - 1; // 0-based

            return new CellRefNode(col, row, isAbsoluteCol, isAbsoluteRow, text);
        }

        private AstNode ParseFunctionCall(Token funcToken)
        {
            string funcName = funcToken.Value;

            // Check for opening parenthesis
            if (Peek().Type != TokenType.LParen)
            {
                // It's an identifier without parens, treat as error for now
                // Future: could be a named range
                throw new FormulaException(
                    $"Expected '(' after function name '{funcName}'", Peek().Position);
            }

            Advance(); // consume '('

            var args = new List<AstNode>();

            // Parse arguments
            if (Peek().Type != TokenType.RParen)
            {
                args.Add(ParseExpression());

                while (Peek().Type == TokenType.Comma)
                {
                    Advance(); // consume ','
                    args.Add(ParseExpression());
                }
            }

            Expect(TokenType.RParen);

            return new FunctionNode(funcName, args);
        }

        /// <summary>
        /// Converts a column letter string to a 0-based index (A=0, B=1, ..., Z=25, AA=26, ...).
        /// </summary>
        public static int ColumnNameToIndex(string columnName)
        {
            int result = 0;
            foreach (char c in columnName.ToUpperInvariant())
            {
                result = result * 26 + (c - 'A' + 1);
            }
            return result - 1; // 0-based
        }

        /// <summary>
        /// Converts a 0-based column index to a letter string.
        /// </summary>
        public static string ColumnIndexToName(int index)
        {
            string result = "";
            int n = index + 1;
            while (n > 0)
            {
                n--;
                result = (char)('A' + n % 26) + result;
                n /= 26;
            }
            return result;
        }
    }
}
