using DataBridge.Api.Infrastructure;
using DataBridge.Api.JsonApi;
using DataBridge.Core.Interfaces;
using DataBridge.Core.Models;
using DataBridge.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Api.Controllers;

/// <summary>Purchases — stored in a CSV file via CsvConnector. Supports read and write.</summary>
[Route("api/purchases")]
[ApiExplorerSettings(GroupName = "Purchases")]
[ProducesResponseType(typeof(JsonApiErrorResponse), StatusCodes.Status400BadRequest)]
public class PurchasesController : BaseController
{
    private readonly IUnitOfWork _uow;
    private const string Type = "purchases";

    public PurchasesController(IUnitOfWork uow) => _uow = uow;

    // ── GET (OData-style) ─────────────────────────────────────────────────────

    /// <summary>List / query purchases using OData-style parameters.</summary>
    /// <remarks>
    /// Examples:
    /// <code>
    /// GET /api/purchases?$filter=status eq 'completed'&amp;$orderby=purchaseDate desc
    /// GET /api/purchases?$filter=totalPrice gt 10.0&amp;$top=20
    /// GET /api/purchases?$filter=quantity ge 2 and status eq 'completed'
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
        var result = await _uow.QueryAsync<Purchase>(spec);
        return Ok(JsonApiDocument.FromCollection(result, Type, spec.Select?.AsReadOnly()));
    }

    // ── POST /query ───────────────────────────────────────────────────────────

    /// <summary>Advanced query with a full QuerySpec JSON body.</summary>
    [HttpPost("query")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(JsonApiCollectionResponse<Dictionary<string, object?>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Query([FromBody] QuerySpec spec)
    {
        spec.From  = Type;
        var result = await _uow.QueryAsync<Purchase>(spec);
        return Ok(JsonApiDocument.FromCollection(result, Type, spec.Select?.AsReadOnly()));
    }

    // ── GET /{id} ─────────────────────────────────────────────────────────────

    /// <summary>Get a single purchase by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JsonApiSingleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(JsonApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await _uow.GetByIdAsync<Purchase>(id);
        if (entity == null)
            return JsonApiError(404, "Not Found", $"Purchase '{id}' not found.");
        return Ok(JsonApiDocument.FromSingle(entity, Type));
    }

    // ── POST ──────────────────────────────────────────────────────────────────

    /// <summary>Register a new purchase.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(JsonApiSingleResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] JsonApiWriteRequest<PurchaseAttributes> request)
    {
        var attrs  = request.Data.Attributes;
        var entity = new Purchase
        {
            Id           = Guid.NewGuid(),
            UserId       = attrs.UserId,
            ProductId    = attrs.ProductId,
            Quantity     = attrs.Quantity,
            TotalPrice   = attrs.TotalPrice,
            PurchaseDate = attrs.PurchaseDate ?? DateTime.UtcNow,
            Status       = attrs.Status ?? "completed"
        };
        var created = await _uow.InsertAsync(entity, PersonName);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            JsonApiDocument.FromSingle(created, Type));
    }

    // ── PATCH ─────────────────────────────────────────────────────────────────

    /// <summary>Update a purchase (quantity, total price or status).</summary>
    [HttpPatch("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(JsonApiSingleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(JsonApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] JsonApiWriteRequest<PurchaseAttributes> request)
    {
        var existing = await _uow.GetByIdAsync<Purchase>(id);
        if (existing == null)
            return JsonApiError(404, "Not Found", $"Purchase '{id}' not found.");

        var attrs           = request.Data.Attributes;
        existing.Quantity   = attrs.Quantity;
        existing.TotalPrice = attrs.TotalPrice;
        existing.Status     = attrs.Status ?? existing.Status;

        var updated = await _uow.UpdateAsync(existing, PersonName);
        return Ok(JsonApiDocument.FromSingle(updated, Type));
    }

    // ── DELETE ────────────────────────────────────────────────────────────────

    /// <summary>Delete a purchase by ID.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(JsonApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _uow.DeleteAsync<Purchase>(id, PersonName);
        if (!deleted)
            return JsonApiError(404, "Not Found", $"Purchase '{id}' not found.");
        return NoContent();
    }
}

/// <summary>Purchase writable attributes.</summary>
public class PurchaseAttributes
{
    /// <summary>ID of the purchasing user.</summary>
    public Guid UserId { get; set; }
    /// <summary>ID of the purchased product.</summary>
    public Guid ProductId { get; set; }
    /// <summary>Number of units purchased.</summary>
    public int Quantity { get; set; }
    /// <summary>Total price (Quantity × unit price).</summary>
    public decimal TotalPrice { get; set; }
    /// <summary>UTC timestamp. Defaults to now.</summary>
    public DateTime? PurchaseDate { get; set; }
    /// <summary>Status: <c>completed</c> or <c>pending</c>.</summary>
    public string? Status { get; set; }
}
