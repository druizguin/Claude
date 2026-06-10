using System.Text.Json.Serialization;

namespace DataBridge.Web.Services;

// ── JSON:API envelope ─────────────────────────────────────────────────────────

public class JsonApiCollection<T>
{
    public List<JsonApiResource<T>> Data { get; set; } = new();
    public JsonApiMeta? Meta { get; set; }
}

public class JsonApiSingle<T>
{
    public JsonApiResource<T> Data { get; set; } = new();
}

public class JsonApiResource<T>
{
    public string Type { get; set; } = "";
    public string Id   { get; set; } = "";
    public T Attributes { get; set; } = default!;
}

public class JsonApiMeta
{
    public int Total  { get; set; }
    public int From   { get; set; }
    public int Offset { get; set; }
}

// ── Query spec ────────────────────────────────────────────────────────────────

public class QuerySpec
{
    [JsonPropertyName("from")]    public string From { get; set; } = "";
    [JsonPropertyName("select")]  public List<string>? Select { get; set; }
    [JsonPropertyName("filter")]  public object? Filter { get; set; }
    [JsonPropertyName("orderby")] public List<OrderBySpec>? OrderBy { get; set; }
    [JsonPropertyName("page")]    public PageSpec? Page { get; set; }
}

public class OrderBySpec
{
    [JsonPropertyName("field")]     public string Field     { get; set; } = "";
    [JsonPropertyName("direction")] public string Direction { get; set; } = "asc";
}

public class PageSpec
{
    [JsonPropertyName("from")]   public int From   { get; set; }
    [JsonPropertyName("offset")] public int Offset { get; set; }
}

// ── Domain DTOs (property names match API camelCase output) ───────────────────

public class ProductDto
{
    public string  Id            { get; set; } = "";
    public string  Name          { get; set; } = "";
    public string  Category      { get; set; } = "";
    public decimal Price         { get; set; }
    public int     StockQuantity { get; set; }
    public string  Barcode       { get; set; } = "";
    public string? Description   { get; set; }
}

public class UserDto
{
    public string   Id                 { get; set; } = "";
    public string   Name               { get; set; } = "";
    public string   Email              { get; set; } = "";
    public int      Age                { get; set; }
    public string   Country            { get; set; } = "";
    public string   Status             { get; set; } = "active";
    public DateTime SignupDate         { get; set; }
    public string?  AddressPrincipalId { get; set; }
    public AddressDto? AddressPrincipal { get; set; }
}

public class AddressDto
{
    public string Id      { get; set; } = "";
    public string Street  { get; set; } = "";
    public string City    { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string Country { get; set; } = "";
}

public class PurchaseDto
{
    public string   Id           { get; set; } = "";
    public string   UserId       { get; set; } = "";
    public string   ProductId    { get; set; } = "";
    public int      Quantity     { get; set; }
    public decimal  TotalPrice   { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string   Status       { get; set; } = "completed";
}

public class AuditRecordDto
{
    public string   Id            { get; set; } = "";
    public DateTime Timestamp     { get; set; }
    public string   OperationType { get; set; } = "";
    public string   EntityId      { get; set; } = "";
    public string   EntityType    { get; set; } = "";
    public string   PersonName    { get; set; } = "";
}
