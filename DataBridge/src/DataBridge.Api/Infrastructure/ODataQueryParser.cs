using System.Text;
using System.Text.Json;
using DataBridge.Core.Models;

namespace DataBridge.Api.Infrastructure;

/// <summary>
/// Parses OData-style query parameters and converts them to QuerySpec.
/// Supported syntax:
///   $filter  : price gt 2.0 and category eq 'Fruits'
///              contains(name,'apple') or startswith(barcode,'000')
///              not (price lt 1.0)
///              price ge 1 and price le 5 or category eq 'Dairy'
///   $orderby : price asc,name desc
///   $select  : name,price,category  (or dot-notation: AddressPrincipal.street)
///   $top     : 20
///   $skip    : 0
/// </summary>
public static class ODataQueryParser
{
    // ── Public entry points ──────────────────────────────────────────────────

    public static QuerySpec Build(
        string? resource,
        string? filter,
        string? orderby,
        string? select,
        int?    top,
        int?    skip)
    {
        return new QuerySpec
        {
            From    = resource ?? string.Empty,
            Select  = ParseSelect(select),
            Filter  = ToJsonElement(ParseFilter(filter)),
            OrderBy = ParseOrderBy(orderby),
            Page    = new PageSpec
            {
                From   = skip ?? 0,
                Offset = top  ?? 20
            }
        };
    }

    // ── Filter ────────────────────────────────────────────────────────────────

    public static object? ParseFilter(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter)) return null;
        return new FilterParser(filter.Trim()).ParseExpression();
    }

    private static JsonElement? ToJsonElement(object? obj)
    {
        if (obj == null) return null;
        var json = JsonSerializer.Serialize(obj);
        return JsonDocument.Parse(json).RootElement;
    }

    // ── OrderBy ───────────────────────────────────────────────────────────────

    public static List<OrderBySpec>? ParseOrderBy(string? orderby)
    {
        if (string.IsNullOrWhiteSpace(orderby)) return null;

        return orderby.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(part =>
            {
                var tokens    = part.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var field     = tokens[0];
                var direction = tokens.Length > 1 ? tokens[1].ToLowerInvariant() : "asc";
                if (direction is not ("asc" or "desc")) direction = "asc";
                return new OrderBySpec { Field = field, Direction = direction };
            })
            .ToList();
    }

    // ── Select ────────────────────────────────────────────────────────────────

    public static List<string>? ParseSelect(string? select)
        => string.IsNullOrWhiteSpace(select)
            ? null
            : select.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToList();

    // ── Filter parser (recursive descent) ────────────────────────────────────

    private sealed class FilterParser
    {
        private readonly string _input;
        private int _pos;

        public FilterParser(string input) => _input = input;

        // expression := and_expr
        public Dictionary<string, object> ParseExpression() => ParseAnd();

        // and_expr := or_expr ('and' or_expr)*
        private Dictionary<string, object> ParseAnd()
        {
            var parts = new List<Dictionary<string, object>> { ParseOr() };

            while (PeekKeyword("and"))
            {
                SkipKeyword("and");
                parts.Add(ParseOr());
            }

            return parts.Count == 1
                ? parts[0]
                : new Dictionary<string, object> { ["and"] = parts.ToArray() };
        }

        // or_expr := unary_expr ('or' unary_expr)*
        private Dictionary<string, object> ParseOr()
        {
            var parts = new List<Dictionary<string, object>> { ParseUnary() };

            while (PeekKeyword("or"))
            {
                SkipKeyword("or");
                parts.Add(ParseUnary());
            }

            return parts.Count == 1
                ? parts[0]
                : new Dictionary<string, object> { ["or"] = parts.ToArray() };
        }

        // unary_expr := 'not' primary | primary
        private Dictionary<string, object> ParseUnary()
        {
            if (PeekKeyword("not"))
            {
                SkipKeyword("not");
                return new Dictionary<string, object> { ["not"] = ParsePrimary() };
            }
            return ParsePrimary();
        }

        // primary := '(' expression ')' | function_call | comparison
        private Dictionary<string, object> ParsePrimary()
        {
            SkipWs();
            if (_pos < _input.Length && _input[_pos] == '(')
            {
                _pos++;
                var inner = ParseExpression();
                SkipWs();
                if (_pos < _input.Length && _input[_pos] == ')') _pos++;
                return inner;
            }

            var func = TryParseFunction();
            if (func != null) return func;

            return ParseComparison();
        }

        // function_call := 'contains' | 'startswith' | 'endswith' | 'in'  '(' field, value ')'
        private Dictionary<string, object>? TryParseFunction()
        {
            SkipWs();
            foreach (var fn in new[] { "contains", "startswith", "endswith" })
            {
                if (!PeekFunction(fn)) continue;

                _pos += fn.Length + 1; // name + '('
                var field = ParseIdentifier();
                SkipWs(); Expect(',');
                SkipWs();
                var value = ParseValue();
                SkipWs(); Expect(')');

                return new Dictionary<string, object>
                {
                    [field] = new Dictionary<string, object> { [fn] = value }
                };
            }

            // in(field, 'a','b','c')
            if (PeekFunction("in"))
            {
                _pos += 3; // "in("
                var field  = ParseIdentifier();
                var values = new List<object>();
                while (_pos < _input.Length && _input[_pos] != ')')
                {
                    SkipWs(); Expect(','); SkipWs();
                    values.Add(ParseValue());
                }
                Expect(')');
                return new Dictionary<string, object>
                {
                    [field] = new Dictionary<string, object> { ["in"] = values.ToArray() }
                };
            }

            return null;
        }

        // comparison := field op value
        private Dictionary<string, object> ParseComparison()
        {
            var field = ParseIdentifier();
            SkipWs();
            var op    = ParseOperator();
            SkipWs();
            var value = ParseValue();

            // Map OData operator → QuerySpec operator
            var qsOp = op switch
            {
                "eq" => null,   // implicit equality
                "ne" => "neq",
                "gt" => "gt",
                "ge" => "gte",
                "lt" => "lt",
                "le" => "lte",
                _    => op
            };

            return qsOp == null
                ? new Dictionary<string, object> { [field] = value }
                : new Dictionary<string, object>
                  { [field] = new Dictionary<string, object> { [qsOp] = value } };
        }

        // identifier := (letter|digit|'_'|'/')+ — '/' converted to '.' for nested paths
        private string ParseIdentifier()
        {
            SkipWs();
            var sb = new StringBuilder();
            while (_pos < _input.Length &&
                   (char.IsLetterOrDigit(_input[_pos]) || _input[_pos] is '_' or '/' or '.'))
                sb.Append(_input[_pos++]);

            if (sb.Length == 0)
                throw new FormatException($"Expected identifier at pos {_pos} in: {_input}");

            return sb.ToString().Replace('/', '.');
        }

        // op := 'eq'|'ne'|'gt'|'ge'|'lt'|'le'
        private string ParseOperator()
        {
            foreach (var op in new[] { "eq", "ne", "gt", "ge", "lt", "le" })
            {
                if (_pos + op.Length > _input.Length) continue;
                if (!_input.AsSpan(_pos, op.Length).Equals(op, StringComparison.OrdinalIgnoreCase)) continue;

                // Must be followed by whitespace or end
                var after = _pos + op.Length;
                if (after < _input.Length && !char.IsWhiteSpace(_input[after])) continue;

                _pos += op.Length;
                return op;
            }
            throw new FormatException(
                $"Expected comparison operator at pos {_pos}: …{_input[Math.Max(0, _pos - 5)..Math.Min(_input.Length, _pos + 15)]}…");
        }

        // value := 'string' | number | true | false | null
        private object ParseValue()
        {
            SkipWs();
            if (_pos >= _input.Length) throw new FormatException("Unexpected end of filter.");

            var ch = _input[_pos];

            if (ch is '\'' or '"')
            {
                var quote = ch;
                _pos++;
                var sb = new StringBuilder();
                while (_pos < _input.Length && _input[_pos] != quote)
                {
                    if (_input[_pos] == '\\') _pos++;
                    sb.Append(_input[_pos++]);
                }
                if (_pos < _input.Length) _pos++;
                return sb.ToString();
            }

            if (PeekKeyword("true"))  { SkipKeyword("true");  return true; }
            if (PeekKeyword("false")) { SkipKeyword("false"); return false; }
            if (PeekKeyword("null"))  { SkipKeyword("null");  return (object)null!; }

            // number (int or decimal)
            var numSb = new StringBuilder();
            if (_pos < _input.Length && _input[_pos] == '-') numSb.Append(_input[_pos++]);
            while (_pos < _input.Length && (char.IsDigit(_input[_pos]) || _input[_pos] == '.'))
                numSb.Append(_input[_pos++]);

            var ns = numSb.ToString();
            if (ns.Length == 0) throw new FormatException($"Cannot parse value at pos {_pos}.");
            if (ns.Contains('.') && decimal.TryParse(ns, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var d)) return d;
            if (long.TryParse(ns, out var l)) return l;

            throw new FormatException($"Cannot convert '{ns}' to a number.");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private bool PeekKeyword(string kw)
        {
            SkipWs();
            if (_pos + kw.Length > _input.Length) return false;
            if (!_input.AsSpan(_pos, kw.Length).Equals(kw, StringComparison.OrdinalIgnoreCase)) return false;
            var after = _pos + kw.Length;
            return after >= _input.Length || !char.IsLetterOrDigit(_input[after]);
        }

        private bool PeekFunction(string name)
        {
            SkipWs();
            var full = name + "(";
            return _pos + full.Length <= _input.Length &&
                   _input.AsSpan(_pos, full.Length).Equals(full, StringComparison.OrdinalIgnoreCase);
        }

        private void SkipKeyword(string kw)
        {
            SkipWs();
            _pos += kw.Length;
        }

        private void Expect(char ch)
        {
            if (_pos < _input.Length && _input[_pos] == ch) { _pos++; return; }
            throw new FormatException($"Expected '{ch}' at pos {_pos} in: {_input}");
        }

        private void SkipWs()
        {
            while (_pos < _input.Length && char.IsWhiteSpace(_input[_pos])) _pos++;
        }
    }
}
