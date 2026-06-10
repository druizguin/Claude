using Audit.Api.Models;
using Audit.Dom.Entities;
using Audit.Dom.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Audit.Api.Controllers;

/// <summary>
/// Endpoints con formato JSON:API (https://jsonapi.org/).
/// Content-Type: application/vnd.api+json
/// </summary>
[ApiController]
[Route("jsonapi/audits")]
[Produces("application/vnd.api+json")]
public class AuditJsonApiController : ControllerBase
{
    private readonly IAuditService _service;

    public AuditJsonApiController(IAuditService service)
    {
        _service = service;
    }

    /// <summary>Lista auditorías en formato JSON:API.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery(Name = "page[number]")] int pageNumber = 1,
        [FromQuery(Name = "page[size]")] int pageSize = 20,
        [FromQuery(Name = "filter[userId]")] string? userId = null,
        CancellationToken ct = default)
    {
        var skip = (pageNumber - 1) * pageSize;
        IEnumerable<AuditEntry> items;
        int total;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            items = await _service.GetAuditsByUserIdAsync(userId, skip, pageSize, ct);
            total = await _service.GetCountByUserIdAsync(userId, ct);
        }
        else
        {
            items = await _service.GetAllAuditsAsync(skip, pageSize, ct);
            total = await _service.GetTotalCountAsync(ct);
        }

        var data = items.Select(ToResource).ToList();

        var doc = new JsonApiDocument<List<JsonApiResource>>
        {
            Data = data,
            Meta = new JsonApiMeta { Total = total, Page = pageNumber, PageSize = pageSize },
            Links = new JsonApiLinks
            {
                Self = $"/jsonapi/audits?page[number]={pageNumber}&page[size]={pageSize}",
                Next = pageNumber * pageSize < total
                    ? $"/jsonapi/audits?page[number]={pageNumber + 1}&page[size]={pageSize}"
                    : null,
                Prev = pageNumber > 1
                    ? $"/jsonapi/audits?page[number]={pageNumber - 1}&page[size]={pageSize}"
                    : null
            }
        };

        return Ok(doc);
    }

    /// <summary>Obtiene una auditoría por Id en formato JSON:API.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var entry = await _service.GetAuditByIdAsync(id, ct);
        if (entry is null) return NotFound();

        return Ok(new JsonApiDocument<JsonApiResource> { Data = ToResource(entry) });
    }

    /// <summary>Crea una auditoría recibiendo formato JSON:API.</summary>
    [HttpPost]
    [Consumes("application/vnd.api+json")]
    public async Task<IActionResult> Create(
        [FromBody] JsonApiDocument<JsonApiResource> document,
        CancellationToken ct)
    {
        var attrs = document.Data.Attributes;

        var entry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            UserId = attrs["userId"]?.ToString() ?? string.Empty,
            EntityId = Guid.TryParse(attrs["entityId"]?.ToString(), out var eid) ? eid : Guid.NewGuid(),
            EntityName = attrs["entityName"]?.ToString() ?? string.Empty,
            Action = Enum.TryParse<Dom.Enums.AuditAction>(attrs["action"]?.ToString(), out var action)
                ? action : Dom.Enums.AuditAction.Create,
            Timestamp = DateTime.UtcNow
        };

        var id = await _service.CreateAuditAsync(entry, ct);
        var created = await _service.GetAuditByIdAsync(id, ct);

        return Created($"/jsonapi/audits/{id}",
            new JsonApiDocument<JsonApiResource> { Data = ToResource(created!) });
    }

    private static JsonApiResource ToResource(AuditEntry entry) => new()
    {
        Type = "audits",
        Id = entry.Id.ToString(),
        Attributes = new Dictionary<string, object?>
        {
            ["userId"] = entry.UserId,
            ["entityId"] = entry.EntityId,
            ["entityName"] = entry.EntityName,
            ["action"] = entry.Action.ToString(),
            ["timestamp"] = entry.Timestamp
        },
        Relationships = entry.Details.Count > 0
            ? new Dictionary<string, JsonApiRelationship>
            {
                ["details"] = new JsonApiRelationship
                {
                    Data = entry.Details.Select(d => new
                    {
                        type = "auditDetails",
                        id = d.Id.ToString(),
                        attributes = new
                        {
                            d.PropertyName,
                            d.OldValue,
                            d.NewValue
                        }
                    }).ToList()
                }
            }
            : null
    };
}
