using System.Text.Json.Serialization;

namespace Audit.Api.Models;

public class JsonApiDocument<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; } = default!;

    [JsonPropertyName("meta")]
    public JsonApiMeta? Meta { get; set; }

    [JsonPropertyName("links")]
    public JsonApiLinks? Links { get; set; }
}

public class JsonApiMeta
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }
}

public class JsonApiLinks
{
    [JsonPropertyName("self")]
    public string? Self { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("prev")]
    public string? Prev { get; set; }
}

public class JsonApiResource
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("attributes")]
    public Dictionary<string, object?> Attributes { get; set; } = [];

    [JsonPropertyName("relationships")]
    public Dictionary<string, JsonApiRelationship>? Relationships { get; set; }
}

public class JsonApiRelationship
{
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}
