using Audit.Data.Extensions;
using Audit.Demo.Services;
using Audit.Svc.Extensions;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("AuditDb")
    ?? "Data Source=audit-demo.db";

builder.Services.AddAuditData(connectionString);
builder.Services.AddAuditService();
builder.Services.AddHostedService<AuditGeneratorService>();

var host = builder.Build();

// Inicializar la base de datos antes de arrancar
using (var scope = host.Services.CreateScope())
{
    var dbInit = scope.ServiceProvider.GetRequiredService<Audit.Data.Database.DatabaseInitializer>();
    await dbInit.InitializeAsync();
}

await host.RunAsync();
