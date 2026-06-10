using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataBridge.Core.Models;

public class QuerySpec
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("select")]
    public List<string>? Select { get; set; }

    [JsonPropertyName("filter")]
    public JsonElement? Filter { get; set; }

    [JsonPropertyName("orderby")]
    public List<OrderBySpec>? OrderBy { get; set; }

    [JsonPropertyName("page")]
    public PageSpec? Page { get; set; }
}

public class OrderBySpec
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = "asc";

    [JsonIgnore]
    public bool IsDescending => Direction.Equals("desc", StringComparison.OrdinalIgnoreCase);
}

public class PageSpec
{
    /// <summary>Start record index (skip N records)</summary>
    [JsonPropertyName("from")]
    public int From { get; set; } = 0;

    /// <summary>Number of records to return (page size)</summary>
    [JsonPropertyName("offset")]
    public int Offset { get; set; } = 20;
}
