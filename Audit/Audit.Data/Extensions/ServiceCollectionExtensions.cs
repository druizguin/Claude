using Audit.Data.Database;
using Audit.Data.Repositories;
using Audit.Dom.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Data.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuditData(this IServiceCollection services, string connectionString)
    {
        services.AddSingleton<IAuditRepository>(_ => new AuditRepository(connectionString));
        services.AddSingleton(_ => new DatabaseInitializer(connectionString));
        return services;
    }
}
