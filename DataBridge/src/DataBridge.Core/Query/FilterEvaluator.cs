using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;

namespace DataBridge.Core.Query;

/// <summary>
/// Parses the JSON filter spec from QuerySpec.Filter and builds LINQ predicates.
/// Supports: eq, neq, gt, gte, lt, lte, like, contains, startsWith, endsWith, in
/// Logical: and, or, not — all nestable and negatable.
/// Field paths are case-insensitive and support dot-notation (e.g. "AddressPrincipal.street").
/// </summary>
public static class FilterEvaluator
{
    public static IEnumerable<T> Apply<T>(IEnumerable<T> source, JsonElement? filter) where T : class
    {
        if (!filter.HasValue || filter.Value.ValueKind != JsonValueKind.Object)
            return source;

        var param = Expression.Parameter(typeof(T), "x");
        var body = ParseFilter(param, filter.Value);
        if (body == null) return source;

        var lambda = Expression.Lambda<Func<T, bool>>(body, param);
        return source.Where(lambda.Compile());
    }

    public static Expression<Func<T, bool>> BuildPredicate<T>(JsonElement filter) where T : class
    {
        var param = Expression.Parameter(typeof(T), "x");
        var body = ParseFilter(param, filter) ?? Expression.Constant(true);
        return Expression.Lambda<Func<T, bool>>(body, param);
    }

    private static Expression? ParseFilter(ParameterExpression param, JsonElement filter)
    {
        if (filter.ValueKind != JsonValueKind.Object) return null;

        var conditions = new List<Expression>();

        foreach (var prop in filter.EnumerateObject())
        {
            Expression? cond = prop.Name.ToLowerInvariant() switch
            {
                "and" => ParseLogical(param, prop.Value, isAnd: true),
                "or"  => ParseLogical(param, prop.Value, isAnd: false),
                "not" => ParseNot(param, prop.Value),
                _     => ParseFieldCondition(param, prop.Name, prop.Value)
            };
            if (cond != null) conditions.Add(cond);
        }

        return conditions.Count switch
        {
            0 => null,
            1 => conditions[0],
            _ => conditions.Aggregate(Expression.AndAlso)
        };
    }

    private static Expression? ParseLogical(ParameterExpression param, JsonElement array, bool isAnd)
    {
        if (array.ValueKind != JsonValueKind.Array) return null;

        var parts = array.EnumerateArray()
            .Select(el => ParseFilter(param, el))
            .OfType<Expression>()
            .ToList();

        if (parts.Count == 0) return null;
        return isAnd
            ? parts.Aggregate(Expression.AndAlso)
            : parts.Aggregate(Expression.OrElse);
    }

    private static Expression? ParseNot(ParameterExpression param, JsonElement filter)
    {
        var inner = ParseFilter(param, filter);
        return inner != null ? Expression.Not(inner) : null;
    }

    private static Expression? ParseFieldCondition(ParameterExpression param, string fieldPath, JsonElement value)
    {
        Expression accessor;
        try { accessor = BuildPropertyAccessor(param, fieldPath); }
        catch { return null; }

        if (value.ValueKind == JsonValueKind.Object)
        {
            var ops = new List<Expression>();
            foreach (var op in value.EnumerateObject())
            {
                var opExpr = BuildOperatorExpression(accessor, op.Name, op.Value);
                if (opExpr != null) ops.Add(opExpr);
            }
            return ops.Count switch
            {
                0 => null,
                1 => ops[0],
                _ => ops.Aggregate(Expression.AndAlso)
            };
        }

        return BuildImplicitEquality(accessor, value);
    }

    private static Expression BuildPropertyAccessor(Expression expr, string path)
    {
        foreach (var segment in path.Split('.'))
        {
            var prop = expr.Type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.Name.Equals(segment, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Property '{segment}' not found on {expr.Type.Name}");
            expr = Expression.Property(expr, prop);
        }
        return expr;
    }

    private static Expression? BuildOperatorExpression(Expression accessor, string op, JsonElement value)
    {
        return op.ToLowerInvariant() switch
        {
            "eq"          => BuildEquality(accessor, value, negate: false),
            "neq"         => BuildEquality(accessor, value, negate: true),
            "gt"          => BuildComparison(accessor, value, ExpressionType.GreaterThan),
            "gte"         => BuildComparison(accessor, value, ExpressionType.GreaterThanOrEqual),
            "lt"          => BuildComparison(accessor, value, ExpressionType.LessThan),
            "lte"         => BuildComparison(accessor, value, ExpressionType.LessThanOrEqual),
            "like"        => BuildLike(accessor, value),
            "contains"    => BuildStringMethod(accessor, "Contains", value),
            "startswith"  => BuildStringMethod(accessor, "StartsWith", value),
            "endswith"    => BuildStringMethod(accessor, "EndsWith", value),
            "in"          => BuildIn(accessor, value),
            _             => null
        };
    }

    private static Expression BuildImplicitEquality(Expression accessor, JsonElement value)
        => BuildEquality(accessor, value, negate: false);

    private static Expression BuildEquality(Expression accessor, JsonElement value, bool negate)
    {
        var constant = CoerceConstant(accessor.Type, value);
        Expression eq = Expression.Equal(ConvertAccessor(accessor, constant.Type), constant);
        return negate ? Expression.Not(eq) : eq;
    }

    private static Expression BuildComparison(Expression accessor, JsonElement value, ExpressionType type)
    {
        var constant = CoerceConstant(accessor.Type, value);
        var left = ConvertAccessor(accessor, constant.Type);
        return Expression.MakeBinary(type, left, constant);
    }

    private static Expression BuildLike(Expression accessor, JsonElement value)
    {
        // "like": "%foo%" → Contains, "%foo" → EndsWith, "foo%" → StartsWith, "foo" → Equals
        var pattern = value.GetString() ?? string.Empty;
        var str = Expression.Convert(accessor, typeof(string));

        bool startsWild = pattern.StartsWith('%');
        bool endsWild   = pattern.EndsWith('%');
        var core        = pattern.Trim('%');
        var coreConst   = Expression.Constant(core, typeof(string));

        if (startsWild && endsWild)
            return Expression.Call(str, typeof(string).GetMethod("Contains", new[] { typeof(string) })!, coreConst);
        if (startsWild)
            return Expression.Call(str, typeof(string).GetMethod("EndsWith", new[] { typeof(string) })!, coreConst);
        if (endsWild)
            return Expression.Call(str, typeof(string).GetMethod("StartsWith", new[] { typeof(string) })!, coreConst);

        return Expression.Equal(str, coreConst);
    }

    private static Expression BuildStringMethod(Expression accessor, string method, JsonElement value)
    {
        var str = Expression.Convert(accessor, typeof(string));
        var arg = Expression.Constant(value.GetString() ?? string.Empty, typeof(string));
        var mi  = typeof(string).GetMethod(method, new[] { typeof(string) })!;
        return Expression.Call(str, mi, arg);
    }

    private static Expression BuildIn(Expression accessor, JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array) return Expression.Constant(false);

        var parts = value.EnumerateArray()
            .Select(el => BuildEquality(accessor, el, negate: false))
            .ToList();

        return parts.Count == 0
            ? Expression.Constant(false)
            : parts.Aggregate(Expression.OrElse);
    }

    private static ConstantExpression CoerceConstant(Type targetType, JsonElement value)
    {
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying == typeof(string))
            return Expression.Constant(value.GetString(), typeof(string));
        if (underlying == typeof(int))
            return Expression.Constant(value.GetInt32());
        if (underlying == typeof(long))
            return Expression.Constant(value.GetInt64());
        if (underlying == typeof(double))
            return Expression.Constant(value.GetDouble());
        if (underlying == typeof(decimal))
            return Expression.Constant(value.GetDecimal());
        if (underlying == typeof(bool))
            return Expression.Constant(value.GetBoolean());
        if (underlying == typeof(Guid))
            return Expression.Constant(Guid.Parse(value.GetString()!));
        if (underlying == typeof(DateTime))
            return Expression.Constant(DateTime.Parse(value.GetString()!));
        if (underlying == typeof(DateTimeOffset))
            return Expression.Constant(DateTimeOffset.Parse(value.GetString()!));

        // Fallback: try string comparison
        return Expression.Constant(value.ToString(), typeof(string));
    }

    private static Expression ConvertAccessor(Expression accessor, Type targetType)
    {
        if (accessor.Type == targetType) return accessor;
        var underlying = Nullable.GetUnderlyingType(accessor.Type) ?? accessor.Type;
        return underlying == targetType
            ? Expression.Convert(accessor, targetType)
            : Expression.Convert(accessor, targetType);
    }
}
