using DataBridge.Api.Infrastructure;
using DataBridge.Api.JsonApi;
using DataBridge.Api.SeedData;
using DataBridge.Connectors.CSV;
using DataBridge.Connectors.Excel;
using DataBridge.Connectors.SQLite;
using DataBridge.Domain.Entities;
using Microsoft.OpenApi.Models;

// Must run before any SQLite/Dapper usage — registers TEXT↔Guid/DateTime converters
SQLiteTypeHandlers.Register();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opt =>
    {
        opt.JsonSerializerOptions.PropertyNamingPolicy         = null;
        opt.JsonSerializerOptions.DefaultIgnoreCondition       = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title   = "DataBridge API",
        Version = "v1",
        Description =
            "Multi-source Unit of Work REST API.\n\n" +
            "**Data sources**\n" +
            "| Entity   | Source |\n" +
            "|----------|--------|\n" +
            "| Product  | Excel (.xlsx) |\n" +
            "| User     | SQLite |\n" +
            "| Address  | CSV (cross-source, navigation property of User) |\n" +
            "| Purchase | CSV |\n" +
            "| Audit    | CSV |\n\n" +
            "**Two query styles**\n" +
            "- `GET /{resource}?$filter=...&$orderby=...&$top=N&$skip=N&$select=...` — OData-style\n" +
            "- `POST /{resource}/query` — full QuerySpec JSON body (complex nested filters)\n\n" +
            "**OData filter operators:** `eq ne gt ge lt le`\n" +
            "**OData functions:** `contains(f,'v')` `startswith(f,'v')` `endswith(f,'v')` `in(f,'a','b')`\n" +
            "**Logical:** `and or not`\n\n" +
            "**Audit:** every write is logged. Pass `X-User-Name` header to identify the actor.",
        Contact = new OpenApiContact { Name = "DataBridge" }
    });

    // ── Scan assemblies and register XML docs ─────────────────────────────
    SwaggerAssemblyScanner.RegisterXmlDocs(c, typeof(Program).Assembly);

    // ── Operation filters ─────────────────────────────────────────────────
    c.OperationFilter<UserNameHeaderFilter>();          // X-User-Name on all ops
    c.OperationFilter<ODataParametersDocumentationFilter>(); // OData examples on GET ops

    // ── Schema filters ────────────────────────────────────────────────────
    c.SchemaFilter<QuerySpecExampleFilter>();           // realistic QuerySpec example

    // ── Document filter — assembly cross-check & tag cleanup ─────────────
    c.DocumentFilter<AssemblyOperationsDocumentFilter>();

    // ── Type mappings (JSON:API envelope types → simple "object") ─────────
    c.MapType<JsonApiCollectionResponse<Dictionary<string, object?>>>(() =>
        new OpenApiSchema { Type = "object", Description = "JSON:API collection response" });
    c.MapType<JsonApiSingleResponse>(() =>
        new OpenApiSchema { Type = "object", Description = "JSON:API single resource response" });
    c.MapType<JsonApiErrorResponse>(() =>
        new OpenApiSchema { Type = "object", Description = "JSON:API error response" });

    // ── Tag grouping ──────────────────────────────────────────────────────
    c.TagActionsBy(api =>
        new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] ?? "api" });

    // ── X-User-Name security definition ───────────────────────────────────
    c.AddSecurityDefinition("UserName", new OpenApiSecurityScheme
    {
        Type        = SecuritySchemeType.ApiKey,
        In          = ParameterLocation.Header,
        Name        = "X-User-Name",
        Description = "Username for audit logging (not authentication)"
    });

    // Make Swagger accept application/json as the wire format
    // (the actual API uses application/vnd.api+json but Swagger UI calls with application/json)
    c.MapType<System.Text.Json.JsonElement>(() =>
        new OpenApiSchema { Type = "object" });
});

builder.Services.AddCors(opt => opt.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

builder.Services.AddDataBridge(builder.Configuration);

var app = builder.Build();

// ── Seed data ────────────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var products  = scope.ServiceProvider.GetRequiredService<ExcelConnector<Product>>();
    var users     = scope.ServiceProvider.GetRequiredService<SQLiteConnector<User>>();
    var purchases = scope.ServiceProvider.GetRequiredService<CsvConnector<Purchase>>();
    var addresses = scope.ServiceProvider.GetRequiredService<CsvConnector<Address>>();

    await DataSeeder.SeedAsync(products, users, purchases, addresses);
}

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DataBridge API v1");
    c.RoutePrefix        = "swagger";
    c.DocumentTitle      = "DataBridge API";
    c.DefaultModelsExpandDepth(2);
    c.DefaultModelExpandDepth(3);
    c.DisplayRequestDuration();
    c.EnableFilter();
    c.EnableDeepLinking();
});

app.UseCors();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

// Make Program discoverable for WebApplicationFactory in tests
public partial class Program { }
