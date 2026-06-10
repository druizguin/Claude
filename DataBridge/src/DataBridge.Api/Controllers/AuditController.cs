using DataBridge.Api.Infrastructure;
using DataBridge.Api.JsonApi;
using DataBridge.Core.Interfaces;
using DataBridge.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Api.Controllers;

/// <summary>
/// Audit log — append-only CSV that records every Create/Read/Update/Delete operation
/// performed through the Unit of Work.
/// </summary>
[Route("api/audit")]
[ApiExplorerSettings(GroupName = "Audit")]
public class AuditController : BaseController
{
    private readonly IAuditService _audit;
    private const string Type = "audit-records";

    public AuditController(IAuditService audit) => _audit = audit;

    // ── GET (OData-style) ─────────────────────────────────────────────────────

    /// <summary>List / query audit records using OData-style parameters.</summary>
    /// <remarks>
    /// Examples:
    /// <code>
    /// GET /api/audit?$filter=operationType eq 'Create'&amp;$orderby=timestamp desc
    /// GET /api/audit?$filter=entityType eq 'Product' and operationType ne 'Read'
    /// GET /api/audit?$filter=contains(personName,'alice')&amp;$top=50
    /// </code>
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(JsonApiCollectionResponse<Dictionary<string, object?>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery(Name = "$filter")]  string? filter,
        [FromQuery(Name = "$orderby")] string? orderby,
        [FromQuery(Name = "$select")]  string? select,
        [FromQuery(Name = "$top")]     int?    top,
        [FromQuery(Name = "$skip")]    int?    skip)
    {
        var spec   = ODataQueryParser.Build(Type, filter, orderby, select, top, skip);
        var result = await _audit.QueryAsync(spec);
        return Ok(JsonApiDocument.FromCollection(result, Type, spec.Select?.AsReadOnly()));
    }

    // ── POST /query ───────────────────────────────────────────────────────────

    /// <summary>
    /// Advanced audit query. Useful for filtering by <c>OperationType</c>,
    /// <c>EntityType</c>, <c>PersonName</c> or date ranges.
    /// </summary>
    /// <remarks>
    /// Example — all deletes by a specific person:
    /// <code>
    /// {
    ///   "from": "audit-records",
    ///   "filter": { "operationType": "Delete", "personName": { "like": "%alice%" } },
    ///   "orderby": [{"field":"timestamp","direction":"desc"}]
    /// }
    /// </code>
    /// </remarks>
    [HttpPost("query")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(JsonApiCollectionResponse<Dictionary<string, object?>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Query([FromBody] QuerySpec spec)
    {
        spec.From  = Type;
        var result = await _audit.QueryAsync(spec);
        return Ok(JsonApiDocument.FromCollection(result, Type, spec.Select?.AsReadOnly()));
    }

    // ── GET /entity/{entityId} ────────────────────────────────────────────────

    /// <summary>
    /// Returns all audit records for a specific entity ID.
    /// Useful for viewing the full history of a product, user or purchase.
    /// </summary>
    [HttpGet("entity/{entityId:guid}")]
    [ProducesResponseType(typeof(JsonApiCollectionResponse<Dictionary<string, object?>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEntity(Guid entityId)
    {
        var records = await _audit.GetByEntityIdAsync(entityId);
        var result  = new QueryResult<AuditRecord>
        {
            Items      = records.ToList(),
            TotalCount = records.Count,
            From       = 0,
            Offset     = records.Count
        };
        return Ok(JsonApiDocument.FromCollection(result, Type));
    }
}
