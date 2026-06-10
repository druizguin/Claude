using DataBridge.Api.Infrastructure;
using DataBridge.Api.JsonApi;
using DataBridge.Core.Interfaces;
using DataBridge.Core.Models;
using DataBridge.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Api.Controllers;

/// <summary>
/// Users — stored in SQLite. The navigation property <c>AddressPrincipal</c> is resolved
/// cross-source from the Address CSV connector by the Unit of Work.
/// </summary>
[Route("api/users")]
[ApiExplorerSettings(GroupName = "Users")]
[ProducesResponseType(typeof(JsonApiErrorResponse), StatusCodes.Status400BadRequest)]
public class UsersController : BaseController
{
    private readonly IUnitOfWork _uow;
    private const string Type = "users";

    public UsersController(IUnitOfWork uow) => _uow = uow;

    // ── GET (OData-style) ─────────────────────────────────────────────────────

    /// <summary>
    /// List / query users using OData-style parameters.
    /// Use dot-notation in <c>$select</c> to include cross-source navigation properties
    /// (e.g. <c>AddressPrincipal.street</c> resolved from a separate CSV data source).
    /// </summary>
    /// <remarks>
    /// Examples:
    /// <code>
    /// GET /api/users?$filter=country eq 'USA' and age ge 18
    /// GET /api/users?$filter=status eq 'active'&amp;$orderby=name asc&amp;$top=10
    /// GET /api/users?$filter=not (status eq 'pending')&amp;$select=name,email,AddressPrincipal.street
    /// GET /api/users?$filter=contains(email,'@example.com')
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
        var result = await _uow.QueryAsync<User>(spec);
        return Ok(JsonApiDocument.FromCollection(result, Type, spec.Select?.AsReadOnly()));
    }

    // ── POST /query ───────────────────────────────────────────────────────────

    /// <summary>
    /// Advanced query with a full QuerySpec JSON body.
    /// Supports cross-source field selection such as <c>"AddressPrincipal.street"</c>.
    /// </summary>
    [HttpPost("query")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(JsonApiCollectionResponse<Dictionary<string, object?>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Query([FromBody] QuerySpec spec)
    {
        spec.From  = Type;
        var result = await _uow.QueryAsync<User>(spec);
        return Ok(JsonApiDocument.FromCollection(result, Type, spec.Select?.AsReadOnly()));
    }

    // ── GET /{id} ─────────────────────────────────────────────────────────────

    /// <summary>Get a single user by ID. Address navigation property is resolved automatically.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JsonApiSingleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(JsonApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await _uow.GetByIdAsync<User>(id);
        if (entity == null)
            return JsonApiError(404, "Not Found", $"User '{id}' not found.");
        return Ok(JsonApiDocument.FromSingle(entity, Type));
    }

    // ── POST ──────────────────────────────────────────────────────────────────

    /// <summary>Create a new user.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(JsonApiSingleResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] JsonApiWriteRequest<UserAttributes> request)
    {
        var attrs  = request.Data.Attributes;
        var entity = new User
        {
            Id                 = Guid.NewGuid(),
            Name               = attrs.Name,
            Email              = attrs.Email,
            Age                = attrs.Age,
            Country            = attrs.Country,
            Status             = attrs.Status ?? "active",
            SignupDate         = attrs.SignupDate ?? DateTime.UtcNow,
            AddressPrincipalId = attrs.AddressPrincipalId
        };
        var created = await _uow.InsertAsync(entity, PersonName);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            JsonApiDocument.FromSingle(created, Type));
    }

    // ── PATCH ─────────────────────────────────────────────────────────────────

    /// <summary>Update an existing user.</summary>
    [HttpPatch("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(JsonApiSingleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(JsonApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] JsonApiWriteRequest<UserAttributes> request)
    {
        var existing = await _uow.GetByIdAsync<User>(id);
        if (existing == null)
            return JsonApiError(404, "Not Found", $"User '{id}' not found.");

        var attrs = request.Data.Attributes;
        existing.Name               = attrs.Name;
        existing.Email              = attrs.Email;
        existing.Age                = attrs.Age;
        existing.Country            = attrs.Country;
        existing.Status             = attrs.Status ?? existing.Status;
        existing.AddressPrincipalId = attrs.AddressPrincipalId ?? existing.AddressPrincipalId;

        var updated = await _uow.UpdateAsync(existing, PersonName);
        return Ok(JsonApiDocument.FromSingle(updated, Type));
    }

    // ── DELETE ────────────────────────────────────────────────────────────────

    /// <summary>Delete a user by ID.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(JsonApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _uow.DeleteAsync<User>(id, PersonName);
        if (!deleted)
            return JsonApiError(404, "Not Found", $"User '{id}' not found.");
        return NoContent();
    }
}

/// <summary>User writable attributes.</summary>
public class UserAttributes
{
    /// <summary>Full name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Email address.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>Age in years.</summary>
    public int Age { get; set; }
    /// <summary>Country of residence (e.g. "USA").</summary>
    public string Country { get; set; } = string.Empty;
    /// <summary>Account status: <c>active</c> or <c>pending</c>.</summary>
    public string? Status { get; set; }
    /// <summary>Registration date (UTC). Defaults to now.</summary>
    public DateTime? SignupDate { get; set; }
    /// <summary>FK to an Address stored in the CSV data source.</summary>
    public Guid? AddressPrincipalId { get; set; }
}
