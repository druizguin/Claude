using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Audit.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IConfiguration _config;

    public string ApiBaseUrl { get; private set; } = string.Empty;

    public IndexModel(IConfiguration config)
    {
        _config = config;
    }

    public void OnGet()
    {
        ApiBaseUrl = _config["AuditApi:BaseUrl"] ?? "http://localhost:5000/";
    }
}
