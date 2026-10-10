using System.Globalization;
using System.Text.RegularExpressions;

namespace HARFileInvestigator
{
    internal sealed class HarQueryExpression
    {
        private readonly Func<HarTraceEntry, bool> _predicate;

        internal HarQueryExpression(Func<HarTraceEntry, bool> predicate)
        {
            _predicate = predicate;
        }

        internal bool Matches(HarTraceEntry entry) => _predicate(entry);
    }

    internal static class HarQueryParser
    {
        private const int OrBindingPower = 10;
        private const int AndBindingPower = 20;
        private const int NotBindingPower = 30;

        internal static bool TryParse(string query, out HarQueryExpression? expression, out string? error)
        {
            try
            {
                var parser = new Parser(query);
                expression = new HarQueryExpression(parser.Parse());
                error = null;
                return true;
            }
            catch (FormatException exception)
            {
                expression = null;
                error = exception.Message;
                return false;
            }
        }

        private static bool EvaluateTerm(HarTraceEntry entry, string token)
        {
            var match = Regex.Match(token, "^(?<field>[a-zA-Z_][a-zA-Z0-9_]*)\\w*(?<op><=|>=|!=|=|:|<|>)\\w*(?<value>.+)$");
            if (!match.Success)
            {
                return entry.SearchText.Contains(Unquote(token), StringComparison.OrdinalIgnoreCase);
            }

            var field = match.Groups["field"].Value.ToLowerInvariant();
            var op = match.Groups["op"].Value;
            var value = Unquote(match.Groups["value"].Value);

            return field switch
            {
                "method" => CompareString(entry.Method, op, value),
                "host" => CompareString(entry.Host, op, value),
                "url" => CompareString(entry.Url, op, value),
                "mime" or "mimetype" => CompareString(entry.MimeType, op, value),
                "ip" => CompareString(entry.IpAddress, op, value),
                "tag" or "tags" => CompareString(entry.Tags, op, value),
                "status" => CompareInt(entry.Status, op, value),
                "time" or "duration" => CompareDouble(entry.DurationMs, op, value),
                "request" => CompareString(entry.RequestText, op, value),
                "response" => CompareString(entry.ResponseText, op, value),
                "text" => CompareString(entry.SearchText, op, value),
                _ => entry.AdditionalFields.TryGetValue(field, out var additionalValue)
                    ? CompareString(additionalValue, op, value)
                    : entry.SearchText.Contains(token, StringComparison.OrdinalIgnoreCase)
            };
        }

        private static string Unquote(string value)
        {
            return value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"')
                ? value[1..^1]
                : value;
        }

        private static bool CompareString(string source, string op, string value)
        {
            return op switch
            {
                "=" => string.Equals(source, value, StringComparison.OrdinalIgnoreCase),
                "!=" => !string.Equals(source, value, StringComparison.OrdinalIgnoreCase),
                ":" => source.Contains(value, StringComparison.OrdinalIgnoreCase),
                _ => source.Contains(value, StringComparison.OrdinalIgnoreCase)
            };
        }

        private static bool CompareInt(int source, string op, string value)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return false;
            }

            return op switch
            {
                "=" => source == parsed,
                "!=" => source != parsed,
                ">" => source > parsed,
                ">=" => source >= parsed,
                "<" => source < parsed,
                "<=" => source <= parsed,
                _ => false
            };
        }

        private static bool CompareDouble(double source, string op, string value)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return false;
            }

            return op switch
            {
                "=" => Math.Abs(source - parsed) < 0.0001,
                "!=" => Math.Abs(source - parsed) >= 0.0001,
                ">" => source > parsed,
                ">=" => source >= parsed,
                "<" => source < parsed,
                "<=" => source <= parsed,
                _ => false
            };
        }

        private enum TokenKind
        {
            Term,
            Not,
            And,
            Or,
            LeftParenthesis,
            RightParenthesis,
            End
        }

        private readonly record struct Token(TokenKind Kind, string Text, int Position);

        private sealed class Parser
        {
            private readonly List<Token> _tokens;
            private int _position;

            internal Parser(string query)
            {
                _tokens = Tokenize(query);
            }

            internal Func<HarTraceEntry, bool> Parse()
            {
                if (Current.Kind == TokenKind.End)
                {
                    return _ => true;
                }

                var expression = ParseExpression(0);
                if (Current.Kind != TokenKind.End)
                {
                    throw Error($"Unexpected '{Current.Text}'.");
                }

                return expression;
            }

            private Func<HarTraceEntry, bool> ParseExpression(int minimumBindingPower)
            {
                var left = ParsePrefix();

                while (true)
                {
                    var implicitAnd = false;
                    int bindingPower;
                    switch (Current.Kind)
                    {
                        case TokenKind.Or:
                            bindingPower = OrBindingPower;
                            break;
                        case TokenKind.And:
                            bindingPower = AndBindingPower;
                            break;
                        case TokenKind.Term:
                        case TokenKind.Not:
                        case TokenKind.LeftParenthesis:
                            bindingPower = AndBindingPower;
                            implicitAnd = true;
                            break;
                        default:
                            return left;
                    }

                    if (bindingPower <= minimumBindingPower)
                    {
                        return left;
                    }

                    if (!implicitAnd)
                    {
                        Advance();
                    }

                    var right = ParseExpression(bindingPower);
                    var previous = left;
                    left = bindingPower == OrBindingPower
                        ? entry => previous(entry) || right(entry)
                        : entry => previous(entry) && right(entry);
                }
            }

            private Func<HarTraceEntry, bool> ParsePrefix()
            {
                switch (Current.Kind)
                {
                    case TokenKind.Term:
                        var term = Current.Text;
                        Advance();
                        return entry => EvaluateTerm(entry, term);
                    case TokenKind.Not:
                        Advance();
                        var negated = ParseExpression(NotBindingPower);
                        return entry => !negated(entry);
                    case TokenKind.LeftParenthesis:
                        var opening = Current;
                        Advance();
                        var grouped = ParseExpression(0);
                        if (Current.Kind != TokenKind.RightParenthesis)
                        {
                            throw new FormatException($"Missing ')' for '(' at position {opening.Position + 1}.");
                        }

                        Advance();
                        return grouped;
                    default:
                        throw Error("Expected a search term or '('.");
                }
            }

            private Token Current => _tokens[_position];

            private void Advance()
            {
                if (Current.Kind != TokenKind.End)
                {
                    _position++;
                }
            }

            private FormatException Error(string message)
            {
                return new FormatException($"{message} (position {Current.Position + 1})");
            }

            private static List<Token> Tokenize(string query)
            {
                var tokens = new List<Token>();
                var index = 0;
                while (index < query.Length)
                {
                    if (char.IsWhiteSpace(query[index]))
                    {
                        index++;
                        continue;
                    }

                    var start = index;
                    switch (query[index])
                    {
                        case '(':
                            tokens.Add(new Token(TokenKind.LeftParenthesis, "(", index++));
                            continue;
                        case ')':
                            tokens.Add(new Token(TokenKind.RightParenthesis, ")", index++));
                            continue;
                        case '!' when index + 1 >= query.Length || query[index + 1] != '=':
                            tokens.Add(new Token(TokenKind.Not, "!", index++));
                            continue;
                    }

                    var inQuotes = false;
                    while (index < query.Length)
                    {
                        var character = query[index];
                        if (character == '"')
                        {
                            inQuotes = !inQuotes;
                            index++;
                            continue;
                        }

                        if (!inQuotes && (char.IsWhiteSpace(character) || character is '(' or ')'))
                        {
                            break;
                        }

                        if (inQuotes && character == '\\' && index + 1 < query.Length)
                        {
                            index += 2;
                            continue;
                        }

                        index++;
                    }

                    if (inQuotes)
                    {
                        throw new FormatException($"Unclosed quote at position {start + 1}.");
                    }

                    var text = query[start..index];
                    var kind = string.Equals(text, "and", StringComparison.OrdinalIgnoreCase)
                        ? TokenKind.And
                        : string.Equals(text, "or", StringComparison.OrdinalIgnoreCase)
                            ? TokenKind.Or
                            : TokenKind.Term;
                    tokens.Add(new Token(kind, text, start));
                }

                tokens.Add(new Token(TokenKind.End, string.Empty, query.Length));
                return tokens;
            }
        }
    }
}
