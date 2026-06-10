using System.Reflection;

namespace DataBridge.Core.Query;

/// <summary>
/// Applies field projection to a set of entities based on a select list.
/// Supports dot-notation paths (case-insensitive), e.g. "AddressPrincipal.street".
/// Returns a dictionary of field-name → value per entity.
/// </summary>
public static class ProjectionEngine
{
    public static Dictionary<string, object?> Project<T>(T entity, IReadOnlyList<string>? select)
        where T : class
    {
        if (select == null || select.Count == 0)
            return GetAllProperties(entity);

        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in select)
        {
            var parts = path.Split('.');

            if (parts.Length == 1)
            {
                var prop = GetProperty(entity!.GetType(), parts[0]);
                if (prop != null)
                    result[ToCamelCase(parts[0])] = prop.GetValue(entity);
                continue;
            }

            // Nested path — traverse the object graph
            object? current = entity;
            for (int i = 0; i < parts.Length - 1 && current != null; i++)
            {
                var prop = GetProperty(current.GetType(), parts[i]);
                current = prop?.GetValue(current);
            }

            if (current != null)
            {
                var leaf = GetProperty(current.GetType(), parts[^1]);
                if (leaf != null)
                    result[ToCamelCase(path)] = leaf.GetValue(current);
            }
        }

        return result;
    }

    private static Dictionary<string, object?> GetAllProperties<T>(T entity) where T : class
    {
        return typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(
                p => ToCamelCase(p.Name),
                p => p.GetValue(entity),
                StringComparer.OrdinalIgnoreCase);
    }

    private static PropertyInfo? GetProperty(Type type, string name)
        => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static string ToCamelCase(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToLowerInvariant(s[0]) + s[1..];
    }
}
