using Audit.Api.GraphQL;
using Audit.Data.Database;
using Audit.Data.Extensions;
using Audit.Dom.Entities;
using Audit.Svc.Extensions;
using Microsoft.AspNetCore.OData;
using Microsoft.OData.ModelBuilder;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AuditDb")
    ?? "Data Source=audit.db";

// ── Infraestructura ────────────────────────────────────────────────────────
builder.Services.AddAuditData(connectionString);
builder.Services.AddAuditService();

// ── OData ──────────────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddOData(opt =>
    {
        var modelBuilder = new ODataConventionModelBuilder();
        modelBuilder.EntitySet<AuditEntry>("Audits");

        opt.AddRouteComponents("odata", modelBuilder.GetEdmModel())
           .Filter()
           .Select()
           .Expand()
           .OrderBy()
           .Count()
           .SetMaxTop(200);
    });

// ── GraphQL (HotChocolate) ─────────────────────────────────────────────────
builder.Services
    .AddGraphQLServer()
    .AddQueryType<AuditQuery>()
    .AddMutationType<AuditMutation>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

// ── Swagger ────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Audit API",
        Version = "v1",
        Description = "API de auditoría con soporte REST, OData, JSON:API y GraphQL."
    });
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// ── CORS ───────────────────────────────────────────────────────────────────
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// ── Inicializar base de datos ──────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var dbInit = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await dbInit.InitializeAsync();
}

// ── Middleware ─────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Audit API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors();
app.UseRouting();
app.MapControllers();
app.MapGraphQL();

app.Run();

// Permite que los tests de integración puedan referenciar el Program
public partial class Program { }
