using DataBridge.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var apiBase = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5000";
builder.Services.AddHttpClient<DataBridgeApiClient>(c =>
{
    c.BaseAddress = new Uri(apiBase.TrimEnd('/') + "/");
    c.DefaultRequestHeaders.Add("Accept", "application/vnd.api+json");
    c.DefaultRequestHeaders.Add("X-User-Name", "WebUI");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<DataBridge.Web.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
