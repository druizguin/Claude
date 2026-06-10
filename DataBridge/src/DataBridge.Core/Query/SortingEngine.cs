using System.Reflection;
using DataBridge.Core.Models;

namespace DataBridge.Core.Query;

public static class SortingEngine
{
    public static IEnumerable<T> Apply<T>(IEnumerable<T> source, IReadOnlyList<OrderBySpec>? orderBy)
        where T : class
    {
        if (orderBy == null || orderBy.Count == 0) return source;

        IOrderedEnumerable<T>? ordered = null;

        foreach (var spec in orderBy)
        {
            var prop = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.Name.Equals(spec.Field, StringComparison.OrdinalIgnoreCase));

            if (prop == null) continue;

            Func<T, object?> keySelector = x => prop.GetValue(x);

            if (ordered == null)
            {
                ordered = spec.IsDescending
                    ? source.OrderByDescending(keySelector)
                    : source.OrderBy(keySelector);
            }
            else
            {
                ordered = spec.IsDescending
                    ? ordered.ThenByDescending(keySelector)
                    : ordered.ThenBy(keySelector);
            }
        }

        return ordered ?? source;
    }
}
