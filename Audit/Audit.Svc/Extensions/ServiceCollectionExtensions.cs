using Audit.Dom.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Audit.Svc.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuditService(this IServiceCollection services)
    {
        services.AddScoped<IAuditService, AuditService>();
        return services;
    }
}
