using DataBridge.Core.Interfaces;
using DataBridge.Core.Models;

namespace DataBridge.UnitOfWork;

/// <summary>
/// Maps entity types to their data connectors and tracks navigation-property
/// cross-source relationships (e.g. User.AddressPrincipal → Address connector).
/// </summary>
public class EntitySourceRegistry
{
    private readonly Dictionary<Type, IDataConnector> _connectors = new();

    // entityType → (navigationPropertyName, relatedEntityType)
    private readonly Dictionary<Type, List<(string NavProp, string FkProp, Type RelatedType)>> _relationships = new();

    public EntitySourceRegistry Register<T>(IDataConnector<T> connector) where T : class, IEntity
    {
        _connectors[typeof(T)] = (IDataConnector)connector;
        return this;
    }

    public void AddRelationship<TOwner, TRelated>(string navigationProperty, string foreignKeyProperty)
    {
        if (!_relationships.TryGetValue(typeof(TOwner), out var list))
        {
            list = new List<(string, string, Type)>();
            _relationships[typeof(TOwner)] = list;
        }
        list.Add((navigationProperty, foreignKeyProperty, typeof(TRelated)));
    }

    public IDataConnector<T>? GetConnector<T>() where T : class, IEntity
        => _connectors.TryGetValue(typeof(T), out var c) ? (IDataConnector<T>)c : null;

    public IDataConnector? GetConnector(Type entityType)
        => _connectors.TryGetValue(entityType, out var c) ? c : null;

    public IReadOnlyList<(string NavProp, string FkProp, Type RelatedType)> GetRelationships(Type ownerType)
        => _relationships.TryGetValue(ownerType, out var list) ? list : Array.Empty<(string, string, Type)>();

    public IEnumerable<IDataConnector> AllConnectors() => _connectors.Values;
}
