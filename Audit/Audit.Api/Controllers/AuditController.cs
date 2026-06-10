using Audit.Api.Models;
using Audit.Dom.Entities;
using Audit.Dom.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace Audit.Api.Controllers;

/// <summary>
/// Endpoints REST y OData para auditorías.
/// OData: GET /odata/Audits  (soporta $filter, $orderby, $top, $skip, $select, $count)
/// REST:  GET /api/audits, POST /api/audits
/// </summary>
[ApiController]
public class AuditController : ODataController
{
    private readonly IAuditService _service;

    public AuditController(IAuditService service)
    {
        _service = service;
    }

    // ── OData ──────────────────────────────────────────────────────────────

    [EnableQuery(PageSize = 50, MaxTop = 200)]
    [HttpGet("odata/Audits")]
    public async Task<IActionResult> GetOData(CancellationToken ct)
    {
        var items = await _service.GetAllAuditsAsync(0, 200, ct);
        return Ok(items.AsQueryable());
    }

    [EnableQuery]
    [HttpGet("odata/Audits({id})")]
    public async Task<IActionResult> GetODataById([FromRoute] Guid id, CancellationToken ct)
    {
        var entry = await _service.GetAuditByIdAsync(id, ct);
        return entry is null ? NotFound() : Ok(entry);
    }

    // ── REST ───────────────────────────────────────────────────────────────

    /// <summary>Obtiene todas las auditorías con paginación.</summary>
    [HttpGet("api/audits")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var items = await _service.GetAllAuditsAsync(skip, pageSize, ct);
        var total = await _service.GetTotalCountAsync(ct);
        return Ok(new { data = items, total, page, pageSize });
    }

    /// <summary>Obtiene auditorías por UserId.</summary>
    [HttpGet("api/audits/user/{userId}")]
    public async Task<IActionResult> GetByUser(
        string userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var items = await _service.GetAuditsByUserIdAsync(userId, skip, pageSize, ct);
        var total = await _service.GetCountByUserIdAsync(userId, ct);
        return Ok(new { data = items, total, page, pageSize });
    }

    /// <summary>Obtiene una auditoría por Id.</summary>
    [HttpGet("api/audits/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var entry = await _service.GetAuditByIdAsync(id, ct);
        return entry is null ? NotFound() : Ok(entry);
    }

    /// <summary>Crea una nueva auditoría.</summary>
    [HttpPost("api/audits")]
    public async Task<IActionResult> Create([FromBody] CreateAuditRequest request, CancellationToken ct)
    {
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            EntityId = request.EntityId,
            EntityName = request.EntityName,
            Action = request.Action,
            Timestamp = DateTime.UtcNow,
            Details = request.Details.Select(d => new AuditDetail
            {
                Id = Guid.NewGuid(),
                PropertyName = d.PropertyName,
                OldValue = d.OldValue,
                NewValue = d.NewValue
            }).ToList()
        };

        var id = await _service.CreateAuditAsync(entry, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }
}
