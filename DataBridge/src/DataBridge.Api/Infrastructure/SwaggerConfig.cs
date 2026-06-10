using DataBridge.Core.Models;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DataBridge.Api.Infrastructure;

/// <summary>
/// Adds the X-User-Name header as an optional parameter to every operation.
/// </summary>
public class UserNameHeaderFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name        = "X-User-Name",
            In          = ParameterLocation.Header,
            Required    = false,
            Description = "Name of the person performing the operation (audit logging).",
            Schema      = new OpenApiSchema { Type = "string", Default = new OpenApiString("anonymous") }
        });
    }
}

/// <summary>
/// For actions that carry OData query parameters ($filter, $orderby, etc.)
/// adds rich documentation with usage examples.
/// </summary>
public class ODataParametersDocumentationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Only enrich GET operations that have $filter param
        var hasOdata = context.ApiDescription.ParameterDescriptions
            .Any(p => p.Name.StartsWith('$'));
        if (!hasOdata) return;

        operation.Description ??= "";
        operation.Description +=
            "\n\n**OData query examples:**\n" +
            "```\n" +
            "GET /api/products?$filter=price gt 2 and category eq 'Fruits'\n" +
            "GET /api/products?$filter=contains(name,'apple')&$orderby=price asc&$top=5&$skip=0\n" +
            "GET /api/products?$filter=price ge 1 and price le 5&$select=name,price\n" +
            "GET /api/users?$filter=country eq 'USA' and age ge 18&$orderby=name asc\n" +
            "GET /api/users?$filter=not (status eq 'pending')&$select=name,email,AddressPrincipal.street\n" +
            "```\n\n" +
            "**Supported OData operators:** `eq`, `ne`, `gt`, `ge`, `lt`, `le`\n\n" +
            "**Supported functions:** `contains(field,'value')`, `startswith(field,'value')`, `endswith(field,'value')`, `in(field,'a','b')`\n\n" +
            "**Logical combinators:** `and`, `or`, `not`";

        // Add example values to params
        foreach (var param in operation.Parameters)
        {
            if (param.Name == "$filter" && param.Example == null)
                param.Example = new OpenApiString("price gt 2.0 and category eq 'Fruits'");
            if (param.Name == "$orderby" && param.Example == null)
                param.Example = new OpenApiString("price asc,name desc");
            if (param.Name == "$select" && param.Example == null)
                param.Example = new OpenApiString("name,price,category");
            if (param.Name == "$top" && param.Example == null)
                param.Example = new OpenApiInteger(20);
            if (param.Name == "$skip" && param.Example == null)
                param.Example = new OpenApiInteger(0);
        }
    }
}

/// <summary>
/// Provides a realistic OpenAPI example for the QuerySpec body (POST /query endpoints).
/// </summary>
public class QuerySpecExampleFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(QuerySpec)) return;

        schema.Example = new OpenApiObject
        {
            ["from"]   = new OpenApiString("products"),
            ["select"] = new OpenApiArray
            {
                new OpenApiString("name"),
                new OpenApiString("price"),
                new OpenApiString("category")
            },
            ["filter"] = new OpenApiObject
            {
                ["price"]    = new OpenApiObject { ["gt"] = new OpenApiDouble(2.0) },
                ["category"] = new OpenApiString("Fruits")
            },
            ["orderby"] = new OpenApiArray
            {
                new OpenApiObject
                {
                    ["field"]     = new OpenApiString("price"),
                    ["direction"] = new OpenApiString("asc")
                }
            },
            ["page"] = new OpenApiObject
            {
                ["from"]   = new OpenApiInteger(0),
                ["offset"] = new OpenApiInteger(20)
            }
        };
    }
}
