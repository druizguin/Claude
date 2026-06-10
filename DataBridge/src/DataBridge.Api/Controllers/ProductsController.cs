using DataBridge.Api.Infrastructure;
using DataBridge.Api.JsonApi;
using DataBridge.Core.Interfaces;
using DataBridge.Core.Models;
using DataBridge.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Api.Controllers;

/// <summary>Supermarket products — stored in an Excel (.xlsx) file via ExcelConnector.</summary>
[Route("api/products")]
[ApiExplorerSettings(GroupName = "Products")]
[ProducesResponseType(typeof(JsonApiErrorResponse), StatusCodes.Status400BadRequest)]
public class ProductsController : BaseController
{
    private readonly IUnitOfWork _uow;
    private const string Type = "products";

    public ProductsController(IUnitOfWork uow) => _uow = uow;

    // ── GET (OData-style) ─────────────────────────────────────────────────────

    /// <summary>
    /// List / query products using OData-style query parameters.
    /// Supports filtering, sorting, field projection and pagination without a request body.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// <code>
    /// GET /api/products
    /// GET /api/products?$filter=price gt 2 and category eq 'Fruits'
    /// GET /api/products?$filter=contains(name,'apple')&amp;$orderby=price asc&amp;$top=5
    /// GET /api/products?$filter=price ge 1 and price le 5&amp;$select=name,price,category
    /// GET /api/products?$filter=not (category eq 'Beverages')&amp;$orderby=name asc
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
        var result = await _uow.QueryAsync<Product>(spec);
        return Ok(JsonApiDocument.FromCollection(result, Type, spec.Select?.AsReadOnly()));
    }

    // ── POST /query (JSON body) ───────────────────────────────────────────────

    /// <summary>
    /// Advanced query with a full QuerySpec JSON body.
    /// Supports the same filtering, sorting and projection as the GET endpoint,
    /// plus complex nested filters and arbitrary JSON expressions.
    /// </summary>
    /// <remarks>
    /// Example body:
    /// <code>
    /// {
    ///   "from": "products",
    ///   "select": ["name", "price"],
    ///   "filter": {
    ///     "price": { "gt": 2.0 },
    ///     "or": [ {"category":"Fruits"}, {"category":"Dairy"} ]
    ///   },
    ///   "orderby": [{"field":"price","direction":"asc"}],
    ///   "page": {"from":0,"offset":10}
    /// }
    /// </code>
    /// Supported filter operators: <c>eq, neq, gt, gte, lt, lte, like, contains, startsWith, endsWith, in</c>.
    /// Logical: <c>and, or, not</c> (nestable to any depth).
    /// </remarks>
    [HttpPost("query")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(JsonApiCollectionResponse<Dictionary<string, object?>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Query([FromBody] QuerySpec spec)
    {
        spec.From  = Type;
        var result = await _uow.QueryAsync<Product>(spec);
        return Ok(JsonApiDocument.FromCollection(result, Type, spec.Select?.AsReadOnly()));
    }

    // ── GET /{id} ─────────────────────────────────────────────────────────────

    /// <summary>Get a single product by its GUID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JsonApiSingleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(JsonApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var entity = await _uow.GetByIdAsync<Product>(id);
        if (entity == null)
            return JsonApiError(404, "Not Found", $"Product '{id}' not found.");
        return Ok(JsonApiDocument.FromSingle(entity, Type));
    }

    // ── POST ──────────────────────────────────────────────────────────────────

    /// <summary>Create a new product. Returns the created resource with its generated ID.</summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(JsonApiSingleResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] JsonApiWriteRequest<ProductAttributes> request)
    {
        var attrs  = request.Data.Attributes;
        var entity = new Product
        {
            Id            = Guid.NewGuid(),
            Name          = attrs.Name,
            Category      = attrs.Category,
            Price         = attrs.Price,
            StockQuantity = attrs.StockQuantity,
            Barcode       = attrs.Barcode ?? string.Empty,
            Description   = attrs.Description
        };
        var created = await _uow.InsertAsync(entity, PersonName);
        return CreatedAtAction(nameof(GetById), new { id = created.Id },
            JsonApiDocument.FromSingle(created, Type));
    }

    // ── PATCH ─────────────────────────────────────────────────────────────────

    /// <summary>Update an existing product (full replacement of attributes).</summary>
    [HttpPatch("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(JsonApiSingleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(JsonApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] JsonApiWriteRequest<ProductAttributes> request)
    {
        var existing = await _uow.GetByIdAsync<Product>(id);
        if (existing == null)
            return JsonApiError(404, "Not Found", $"Product '{id}' not found.");

        var attrs = request.Data.Attributes;
        existing.Name          = attrs.Name;
        existing.Category      = attrs.Category;
        existing.Price         = attrs.Price;
        existing.StockQuantity = attrs.StockQuantity;
        existing.Barcode       = attrs.Barcode ?? existing.Barcode;
        existing.Description   = attrs.Description ?? existing.Description;

        var updated = await _uow.UpdateAsync(existing, PersonName);
        return Ok(JsonApiDocument.FromSingle(updated, Type));
    }

    // ── DELETE ────────────────────────────────────────────────────────────────

    /// <summary>Delete a product by ID.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(JsonApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _uow.DeleteAsync<Product>(id, PersonName);
        if (!deleted)
            return JsonApiError(404, "Not Found", $"Product '{id}' not found.");
        return NoContent();
    }
}

/// <summary>Product writable attributes.</summary>
public class ProductAttributes
{
    /// <summary>Display name (e.g. "Apple").</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Category: Fruits, Vegetables, Dairy, Bakery, Beverages.</summary>
    public string Category { get; set; } = string.Empty;
    /// <summary>Unit price.</summary>
    public decimal Price { get; set; }
    /// <summary>Available stock units.</summary>
    public int StockQuantity { get; set; }
    /// <summary>EAN barcode (optional).</summary>
    public string? Barcode { get; set; }
    /// <summary>Free-text description (optional).</summary>
    public string? Description { get; set; }
}
