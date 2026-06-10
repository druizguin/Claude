using System.Text.Json.Serialization;
using DataBridge.Core.Models;
using DataBridge.Core.Query;

namespace DataBridge.Api.JsonApi;

/// <summary>
/// Lightweight JSON:API response builder.
/// Spec: https://jsonapi.org/format/
/// </summary>
public static class JsonApiDocument
{
    public static JsonApiCollectionResponse<Dictionary<string, object?>> FromCollection<T>(
        QueryResult<T> result,
        string type,
        IReadOnlyList<string>? select = null)
        where T : class, IEntity
    {
        var data = result.Items.Select(item => new JsonApiResource
        {
            Type       = type,
            Id         = item.Id.ToString(),
            Attributes = ProjectionEngine.Project(item, select)
        }).ToList();

        return new JsonApiCollectionResponse<Dictionary<string, object?>>
        {
            Data = data,
            Meta = new JsonApiMeta
            {
                Total  = result.TotalCount,
                From   = result.From,
                Offset = result.Offset
            }
        };
    }

    public static JsonApiSingleResponse FromSingle<T>(T item, string type, IReadOnlyList<string>? select = null)
        where T : class, IEntity
        => new()
        {
            Data = new JsonApiResource
            {
                Type       = type,
                Id         = item.Id.ToString(),
                Attributes = ProjectionEngine.Project(item, select)
            }
        };

    public static JsonApiErrorResponse Error(int status, string title, string detail)
        => new()
        {
            Errors = new[]
            {
                new JsonApiError { Status = status.ToString(), Title = title, Detail = detail }
            }
        };
}

// ── Response models ──────────────────────────────────────────────────────────

public class JsonApiCollectionResponse<T>
{
    [JsonPropertyName("data")]
    public List<JsonApiResource> Data { get; init; } = new();

    [JsonPropertyName("meta")]
    public JsonApiMeta? Meta { get; init; }
}

public class JsonApiSingleResponse
{
    [JsonPropertyName("data")]
    public JsonApiResource Data { get; init; } = new();
}

public class JsonApiResource
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("attributes")]
    public Dictionary<string, object?> Attributes { get; init; } = new();
}

public class JsonApiMeta
{
    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("from")]
    public int From { get; init; }

    [JsonPropertyName("offset")]
    public int Offset { get; init; }
}

public class JsonApiErrorResponse
{
    [JsonPropertyName("errors")]
    public IEnumerable<JsonApiError> Errors { get; init; } = Array.Empty<JsonApiError>();
}

public class JsonApiError
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("detail")]
    public string Detail { get; init; } = string.Empty;
}
