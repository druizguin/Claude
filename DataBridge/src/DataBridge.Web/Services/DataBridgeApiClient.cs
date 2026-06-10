using System.Net.Http.Json;
using System.Text.Json;

namespace DataBridge.Web.Services;

/// <summary>
/// Typed HTTP client that wraps DataBridge.Api.
/// Translates JSON:API envelope responses into flat DTOs with Id populated.
/// </summary>
public class DataBridgeApiClient
{
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public DataBridgeApiClient(HttpClient http) => _http = http;

    // ── Generic helpers ───────────────────────────────────────────────────────

    public async Task<(List<T> Items, int Total)> QueryAsync<T>(string resource, QuerySpec spec)
        where T : class
    {
        spec.From = resource;
        var resp = await _http.PostAsJsonAsync($"api/{resource}/query", spec, JsonOpts);
        resp.EnsureSuccessStatusCode();
        var doc = await resp.Content.ReadFromJsonAsync<JsonApiCollection<T>>(JsonOpts)
                  ?? new JsonApiCollection<T>();

        var items = doc.Data.Select(r =>
        {
            SetId(r.Attributes, r.Id);
            return r.Attributes;
        }).ToList();

        return (items, doc.Meta?.Total ?? items.Count);
    }

    public async Task<T?> GetByIdAsync<T>(string resource, string id) where T : class
    {
        var resp = await _http.GetAsync($"api/{resource}/{id}");
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();
        var doc = await resp.Content.ReadFromJsonAsync<JsonApiSingle<T>>(JsonOpts);
        if (doc == null) return null;
        SetId(doc.Data.Attributes, doc.Data.Id);
        return doc.Data.Attributes;
    }

    public async Task<T> CreateAsync<T>(string resource, object attributes) where T : class
    {
        var body = new { data = new { type = resource, attributes } };
        var resp = await _http.PostAsJsonAsync($"api/{resource}", body, JsonOpts);
        resp.EnsureSuccessStatusCode();
        var doc = await resp.Content.ReadFromJsonAsync<JsonApiSingle<T>>(JsonOpts)!;
        SetId(doc!.Data.Attributes, doc.Data.Id);
        return doc.Data.Attributes;
    }

    public async Task<T> UpdateAsync<T>(string resource, string id, object attributes) where T : class
    {
        var body = new { data = new { type = resource, id, attributes } };
        var resp = await _http.PatchAsJsonAsync($"api/{resource}/{id}", body, JsonOpts);
        resp.EnsureSuccessStatusCode();
        var doc = await resp.Content.ReadFromJsonAsync<JsonApiSingle<T>>(JsonOpts)!;
        SetId(doc!.Data.Attributes, doc.Data.Id);
        return doc.Data.Attributes;
    }

    public async Task DeleteAsync(string resource, string id)
    {
        var resp = await _http.DeleteAsync($"api/{resource}/{id}");
        resp.EnsureSuccessStatusCode();
    }

    public async Task<List<AuditRecordDto>> GetAuditByEntityAsync(string entityId)
    {
        var resp = await _http.GetAsync($"api/audit/entity/{entityId}");
        resp.EnsureSuccessStatusCode();
        var doc = await resp.Content.ReadFromJsonAsync<JsonApiCollection<AuditRecordDto>>(JsonOpts)
                  ?? new JsonApiCollection<AuditRecordDto>();
        return doc.Data.Select(r => { r.Attributes.Id = r.Id; return r.Attributes; }).ToList();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SetId<T>(T obj, string id)
    {
        var prop = typeof(T).GetProperty("Id");
        prop?.SetValue(obj, id);
    }
}
