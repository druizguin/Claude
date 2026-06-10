using DataBridge.Api.JsonApi;
using Microsoft.AspNetCore.Mvc;

namespace DataBridge.Api.Controllers;

[ApiController]
[Produces("application/vnd.api+json")]
public abstract class BaseController : ControllerBase
{
    protected string PersonName =>
        Request.Headers.TryGetValue("X-User-Name", out var v) && !string.IsNullOrWhiteSpace(v)
            ? v.ToString()
            : "anonymous";

    protected IActionResult JsonApiError(int statusCode, string title, string detail)
    {
        Response.ContentType = "application/vnd.api+json";
        return StatusCode(statusCode, JsonApiDocument.Error(statusCode, title, detail));
    }
}
