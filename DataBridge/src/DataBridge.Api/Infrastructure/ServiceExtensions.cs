using DataBridge.Audit;
using DataBridge.Connectors.CSV;
using DataBridge.Connectors.Excel;
using DataBridge.Connectors.SQLite;
using DataBridge.Core.Interfaces;
using DataBridge.Domain.Entities;
using DataBridge.UnitOfWork;

namespace DataBridge.Api.Infrastructure;

public static class ServiceExtensions
{
    public static IServiceCollection AddDataBridge(this IServiceCollection services, IConfiguration config)
    {
        var dataDir = config["DataBridge:DataDirectory"]
            ?? Path.Combine(AppContext.BaseDirectory, "data");

        Directory.CreateDirectory(dataDir);

        // ── Connectors ───────────────────────────────────────────────────────
        var productConnector = new ExcelConnector<Product>(
            Path.Combine(dataDir, "products.xlsx"));

        var sqliteCs = $"Data Source={Path.Combine(dataDir, "databridge.db")}";
        var userConnector = new SQLiteConnector<User>(sqliteCs);

        var purchaseConnector = new CsvConnector<Purchase>(
            Path.Combine(dataDir, "purchases.csv"));

        var addressConnector = new CsvConnector<Address>(
            Path.Combine(dataDir, "addresses.csv"));

        // ── Registry ─────────────────────────────────────────────────────────
        var registry = new EntitySourceRegistry()
            .Register(productConnector)
            .Register(userConnector)
            .Register(purchaseConnector)
            .Register(addressConnector);

        // User.AddressPrincipal is resolved from the Address CSV connector
        registry.AddRelationship<User, Address>("AddressPrincipal", "AddressPrincipalId");

        // ── Audit ────────────────────────────────────────────────────────────
        var auditService = new CsvAuditService(Path.Combine(dataDir, "audit.csv"));

        services.AddSingleton(registry);
        services.AddSingleton<IAuditService>(auditService);
        services.AddScoped<IUnitOfWork>(sp =>
            new DataBridgeUnitOfWork(
                sp.GetRequiredService<EntitySourceRegistry>(),
                sp.GetRequiredService<IAuditService>()));

        // Expose typed connectors for seed access
        services.AddSingleton(productConnector);
        services.AddSingleton(userConnector);
        services.AddSingleton(purchaseConnector);
        services.AddSingleton(addressConnector);

        return services;
    }
}
