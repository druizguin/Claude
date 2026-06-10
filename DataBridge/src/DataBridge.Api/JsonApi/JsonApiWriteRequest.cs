using System.Text.Json.Serialization;

namespace DataBridge.Api.JsonApi;

public class JsonApiWriteRequest<TAttributes>
{
    [JsonPropertyName("data")]
    public JsonApiWriteData<TAttributes> Data { get; set; } = new();
}

public class JsonApiWriteData<TAttributes>
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("attributes")]
    public TAttributes Attributes { get; set; } = default!;
}
