using System.Reflection;
using DataBridge.Core.Interfaces;
using DataBridge.Core.Models;

namespace DataBridge.UnitOfWork;

/// <summary>
/// Resolves cross-source navigation properties.
/// After a primary query, this class batch-loads related entities from their
/// respective connectors (different data sources) and populates navigation properties.
/// </summary>
public class CrossSourceResolver
{
    private readonly EntitySourceRegistry _registry;

    public CrossSourceResolver(EntitySourceRegistry registry)
        => _registry = registry;

    public async Task ResolveAsync<T>(IList<T> entities, CancellationToken ct = default)
        where T : class, IEntity
    {
        var relationships = _registry.GetRelationships(typeof(T));
        if (!relationships.Any()) return;

        // Each relationship is resolved in parallel
        await Task.WhenAll(relationships.Select(rel => ResolveRelationshipAsync(entities, rel, ct)));
    }

    private async Task ResolveRelationshipAsync<T>(
        IList<T> entities,
        (string NavProp, string FkProp, Type RelatedType) rel,
        CancellationToken ct) where T : class
    {
        var fkProp  = typeof(T).GetProperty(rel.FkProp,  BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        var navProp = typeof(T).GetProperty(rel.NavProp, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

        if (fkProp == null || navProp == null) return;

        // Collect distinct FK values
        var ids = entities
            .Select(e => fkProp.GetValue(e))
            .OfType<Guid>()
            .Distinct()
            .ToList();

        if (!ids.Any()) return;

        // Load from the related connector
        var relatedConnector = _registry.GetConnector(rel.RelatedType);
        if (relatedConnector == null) return;

        var related = await relatedConnector.GetByIdsAsync(ids, ct);
        var dict    = related.ToDictionary(e => e.Id);

        // Populate navigation properties
        foreach (var entity in entities)
        {
            var fkValue = fkProp.GetValue(entity);
            if (fkValue is Guid id && dict.TryGetValue(id, out var relatedEntity))
                navProp.SetValue(entity, relatedEntity);
        }
    }
}
