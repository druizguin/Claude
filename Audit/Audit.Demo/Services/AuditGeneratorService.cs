using Audit.Dom.Entities;
using Audit.Dom.Enums;
using Audit.Dom.Interfaces;
using Audit.Entities.Dom;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Audit.Demo.Services;

public class AuditGeneratorService : BackgroundService
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditGeneratorService> _logger;

    private static readonly string[] UserIds = ["user-001", "user-002", "user-003", "admin"];
    private static readonly AuditAction[] Actions = [AuditAction.Create, AuditAction.Update, AuditAction.Delete, AuditAction.Read];

    private static readonly Random Rnd = new();

    public AuditGeneratorService(IAuditService auditService, ILogger<AuditGeneratorService> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AuditGeneratorService iniciado. Generando auditorías cada 5 segundos...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var entry = Rnd.Next(2) == 0 ? GeneratePersonaAudit() : GenerateProductoAudit();
                var id = await _auditService.CreateAuditAsync(entry, stoppingToken);
                _logger.LogInformation(
                    "[{Timestamp}] Auditoría creada: {EntityName} - {Action} - ID: {Id}",
                    entry.Timestamp, entry.EntityName, entry.Action, id);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar auditoría");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("AuditGeneratorService detenido.");
    }

    private static AuditEntry GeneratePersonaAudit()
    {
        var entityId = Guid.NewGuid();
        var action = Actions[Rnd.Next(Actions.Length)];
        var userId = UserIds[Rnd.Next(UserIds.Length)];

        var oldPersona = new Persona
        {
            Id = entityId,
            Nombre = PickRandom(["Juan", "María", "Carlos", "Ana"]),
            Apellido = PickRandom(["García", "López", "Martínez"]),
            Edad = Rnd.Next(18, 65),
            Email = $"user{Rnd.Next(1000)}@example.com",
            Telefono = $"+34 6{Rnd.Next(10000000, 99999999)}",
            Direccion = new Direccion
            {
                Calle = PickRandom(["Calle Mayor", "Avenida España", "Calle Sol"]),
                Numero = Rnd.Next(1, 200).ToString(),
                Ciudad = PickRandom(["Madrid", "Barcelona", "Valencia"]),
                CodigoPostal = $"{Rnd.Next(10000, 52999)}",
                Pais = "España"
            }
        };

        var newPersona = new Persona
        {
            Id = entityId,
            Nombre = oldPersona.Nombre,
            Apellido = oldPersona.Apellido,
            Edad = oldPersona.Edad + Rnd.Next(-5, 5),
            Email = $"updated{Rnd.Next(1000)}@example.com",
            Telefono = $"+34 6{Rnd.Next(10000000, 99999999)}",
            Direccion = new Direccion
            {
                Calle = PickRandom(["Gran Vía", "Paseo Colón", "Rambla"]),
                Numero = Rnd.Next(1, 300).ToString(),
                Ciudad = PickRandom(["Sevilla", "Bilbao", "Zaragoza"]),
                CodigoPostal = $"{Rnd.Next(10000, 52999)}",
                Pais = "España"
            }
        };

        return BuildEntry("Persona", entityId, userId, action,
            BuildPropertyChanges(oldPersona, newPersona, action));
    }

    private static AuditEntry GenerateProductoAudit()
    {
        var entityId = Guid.NewGuid();
        var action = Actions[Rnd.Next(Actions.Length)];
        var userId = UserIds[Rnd.Next(UserIds.Length)];

        var oldProducto = new Producto
        {
            Id = entityId,
            Nombre = PickRandom(["Laptop", "Mouse", "Teclado", "Monitor", "Auriculares"]),
            Descripcion = "Descripción original del producto",
            Precio = Math.Round((decimal)(Rnd.NextDouble() * 1000 + 10), 2),
            Stock = Rnd.Next(0, 200),
            Categoria = PickRandom(["Electrónica", "Periféricos", "Accesorios"]),
            Activo = true
        };

        var newProducto = new Producto
        {
            Id = entityId,
            Nombre = oldProducto.Nombre,
            Descripcion = "Descripción actualizada del producto",
            Precio = Math.Round(oldProducto.Precio * (decimal)(0.8 + Rnd.NextDouble() * 0.4), 2),
            Stock = oldProducto.Stock + Rnd.Next(-20, 50),
            Categoria = oldProducto.Categoria,
            Activo = Rnd.Next(5) != 0
        };

        return BuildEntry("Producto", entityId, userId, action,
            BuildPropertyChanges(oldProducto, newProducto, action));
    }

    private static AuditEntry BuildEntry(string entityName, Guid entityId, string userId,
        AuditAction action, List<AuditDetail> details) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EntityId = entityId,
            EntityName = entityName,
            Action = action,
            Timestamp = DateTime.UtcNow,
            Details = details
        };

    private static List<AuditDetail> BuildPropertyChanges<T>(T oldObj, T newObj, AuditAction action)
    {
        if (action == AuditAction.Read) return [];

        var details = new List<AuditDetail>();
        var props = typeof(T).GetProperties();

        foreach (var prop in props)
        {
            var oldVal = prop.GetValue(oldObj);
            var newVal = prop.GetValue(newObj);

            string Serialize(object? v) => v is null ? "" :
                (v.GetType().IsValueType || v is string)
                    ? v.ToString()!
                    : JsonSerializer.Serialize(v, new JsonSerializerOptions { WriteIndented = false });

            var oldStr = Serialize(oldVal);
            var newStr = Serialize(newVal);

            if (action == AuditAction.Create || action == AuditAction.Delete || oldStr != newStr)
            {
                details.Add(new AuditDetail
                {
                    Id = Guid.NewGuid(),
                    PropertyName = prop.Name,
                    OldValue = action == AuditAction.Create ? null : oldStr,
                    NewValue = action == AuditAction.Delete ? null : newStr
                });
            }
        }

        return details;
    }

    private static T PickRandom<T>(T[] arr) => arr[Rnd.Next(arr.Length)];
}
