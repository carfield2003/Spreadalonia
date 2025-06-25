using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Spreadalonia.Formula
{
    /// <summary>
    /// Tokenizes a formula string into a sequence of tokens.
    /// </summary>
    public class Lexer
    {
        private readonly string _input;
        private int _pos;
        private readonly int _length;

        public Lexer(string input)
        {
            _input = input ?? string.Empty;
            _length = _input.Length;
            _pos = 0;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();
            Token token;

            while ((token = NextToken()).Type != TokenType.Eof)
            {
                tokens.Add(token);
            }

            return tokens;
        }

        private Token NextToken()
        {
            SkipWhitespace();

            if (_pos >= _length)
                return new Token(TokenType.Eof, string.Empty, _pos);

            char ch = _input[_pos];

            // Numbers
            if (char.IsDigit(ch) || (ch == '.' && _pos + 1 < _length && char.IsDigit(_input[_pos + 1])))
            {
                return ReadNumber();
            }

            // Strings
            if (ch == '"' || ch == '\'')
            {
                return ReadString(ch);
            }

            // Operators and delimiters
            switch (ch)
            {
                case '+': return new Token(TokenType.Plus, "+", _pos++);
                case '-': return new Token(TokenType.Minus, "-", _pos++);
                case '*': return new Token(TokenType.Mul, "*", _pos++);
                case '/': return new Token(TokenType.Div, "/", _pos++);
                case '^': return new Token(TokenType.Power, "^", _pos++);
                case '%': return new Token(TokenType.Percent, "%", _pos++);
                case '(': return new Token(TokenType.LParen, "(", _pos++);
                case ')': return new Token(TokenType.RParen, ")", _pos++);
                case ',': return new Token(TokenType.Comma, ",", _pos++);
                case ':': return new Token(TokenType.Colon, ":", _pos++);
                case '&': return new Token(TokenType.Ampersand, "&", _pos++);
                case '=': return new Token(TokenType.Eq, "=", _pos++);
            }

            // Two-character operators
            if (ch == '<')
            {
                if (_pos + 1 < _length && _input[_pos + 1] == '=')
                {
                    _pos += 2;
                    return new Token(TokenType.LessEq, "<=", _pos - 2);
                }
                if (_pos + 1 < _length && _input[_pos + 1] == '>')
                {
                    _pos += 2;
                    return new Token(TokenType.NotEq, "<>", _pos - 2);
                }
                return new Token(TokenType.Less, "<", _pos++);
            }

            if (ch == '>')
            {
                if (_pos + 1 < _length && _input[_pos + 1] == '=')
                {
                    _pos += 2;
                    return new Token(TokenType.GreaterEq, ">=", _pos - 2);
                }
                return new Token(TokenType.Greater, ">", _pos++);
            }

            // Cell references (e.g., A1, $A$1, AB123)
            if (char.IsLetter(ch))
            {
                return ReadIdentifierOrCellRef();
            }

            // Unknown character
            return new Token(TokenType.Error, ch.ToString(), _pos++);
        }

        private void SkipWhitespace()
        {
            while (_pos < _length && char.IsWhiteSpace(_input[_pos]))
                _pos++;
        }

        private Token ReadNumber()
        {
            int start = _pos;
            bool hasDecimal = false;

            while (_pos < _length)
            {
                char ch = _input[_pos];
                if (char.IsDigit(ch))
                {
                    _pos++;
                }
                else if (ch == '.' && !hasDecimal)
                {
                    hasDecimal = true;
                    _pos++;
                }
                else
                {
                    break;
                }
            }

            // Scientific notation
            if (_pos < _length && (_input[_pos] == 'e' || _input[_pos] == 'E'))
            {
                _pos++;
                if (_pos < _length && (_input[_pos] == '+' || _input[_pos] == '-'))
                    _pos++;

                int expStart = _pos;
                while (_pos < _length && char.IsDigit(_input[_pos]))
                    _pos++;

                if (_pos == expStart)
                {
                    // No digits after 'e', rollback
                    _pos -= 2;
                }
            }

            string value = _input.Substring(start, _pos - start);
            return new Token(TokenType.Number, value, start);
        }

        private Token ReadString(char quote)
        {
            int start = _pos;
            _pos++; // skip opening quote

            var sb = new StringBuilder();

            while (_pos < _length)
            {
                char ch = _input[_pos++];

                if (ch == '\\' && _pos < _length)
                {
                    // Simple escape
                    sb.Append(_input[_pos++]);
                }
                else if (ch == quote)
                {
                    // Check for double-quote escaping
                    if (_pos < _length && _input[_pos] == quote)
                    {
                        sb.Append(quote);
                        _pos++;
                    }
                    else
                    {
                        return new Token(TokenType.String, sb.ToString(), start);
                    }
                }
                else
                {
                    sb.Append(ch);
                }
            }

            // Unterminated string
            return new Token(TokenType.Error, "Unterminated string", start);
        }

        private Token ReadIdentifierOrCellRef()
        {
            int start = _pos;
            bool hasDollarCol = false;
            bool hasDollarRow = false;

            // Read column letters (with optional $)
            if (_input[_pos] == '$')
            {
                hasDollarCol = true;
                _pos++;
            }

            int colStart = _pos;
            while (_pos < _length && char.IsLetter(_input[_pos]))
                _pos++;

            string columnPart = _input.Substring(colStart, _pos - colStart);

            // Check if followed by row number
            if (_pos < _length)
            {
                if (_input[_pos] == '$')
                {
                    hasDollarRow = true;
                    _pos++;
                }

                if (_pos < _length && char.IsDigit(_input[_pos]))
                {
                    int rowStart = _pos;
                    while (_pos < _length && char.IsDigit(_input[_pos]))
                        _pos++;

                    // Note: If this cell ref is followed by ':' (range ref like A1:B2),
                    // we still produce a CellRef token. The next call to NextToken will
                    // produce the Colon token, and the Parser assembles RangeRef from
                    // CellRef + Colon + CellRef.

                    string rowPart = _input.Substring(rowStart, _pos - rowStart);

                    string fullRef = (hasDollarCol ? "$" : "") + columnPart + (hasDollarRow ? "$" : "") + rowPart;

                    if (int.TryParse(rowPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int row))
                    {
                        return new Token(TokenType.CellRef, fullRef, start);
                    }
                }
            }

            // Not a cell reference - could be a function name or boolean
            return ReadFunctionOrBool(start, columnPart);
        }

        private Token ReadFunctionOrBool(int start, string columnPart)
        {
            // Read remaining identifier characters
            var sb = new StringBuilder(columnPart);
            while (_pos < _length && (char.IsLetterOrDigit(_input[_pos]) || _input[_pos] == '_'))
            {
                sb.Append(_input[_pos]);
                _pos++;
            }

            string identifier = sb.ToString();
            string upper = identifier.ToUpperInvariant();

            // Check for booleans
            if (upper == "TRUE")
                return new Token(TokenType.Boolean, "TRUE", start);
            if (upper == "FALSE")
                return new Token(TokenType.Boolean, "FALSE", start);

            // Otherwise it's a function name (or could be a named range in future)
            return new Token(TokenType.Function, identifier, start);
        }
    }
}
