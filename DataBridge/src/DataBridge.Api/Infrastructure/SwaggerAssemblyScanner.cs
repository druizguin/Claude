using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DataBridge.Api.Infrastructure;

/// <summary>
/// Utility that inspects assemblies, discovers all ApiController classes and their actions,
/// and enriches the Swagger document with any operations that Swashbuckle may have missed
/// or under-documented (missing tags, summaries, response schemas).
///
/// Usage in Program.cs:
///   c.DocumentFilter&lt;AssemblyOperationsDocumentFilter&gt;();
///   SwaggerAssemblyScanner.RegisterXmlDocs(c, typeof(Program).Assembly, ...);
/// </summary>
public static class SwaggerAssemblyScanner
{
    /// <summary>
    /// Registers XML doc comment files for every assembly whose .xml file exists alongside the binary.
    /// Also emits a console summary of discovered controllers / actions.
    /// </summary>
    public static void RegisterXmlDocs(SwaggerGenOptions options, params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var xmlPath = Path.Combine(AppContext.BaseDirectory, assembly.GetName().Name + ".xml");
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            }

            var controllers = DiscoverControllers(assembly);
            Console.WriteLine(
                $"[SwaggerScanner] {assembly.GetName().Name}: " +
                $"{controllers.Count} controller(s), " +
                $"{controllers.Sum(c => c.Actions.Count)} action(s)");

            foreach (var c in controllers)
                foreach (var a in c.Actions)
                    Console.WriteLine($"   {a.HttpMethod,-8} {c.RoutePrefix}/{a.Template}  → {c.Name}.{a.MethodName}");
        }
    }

    /// <summary>Returns a description of all ApiController types found in the assembly.</summary>
    public static List<ControllerDescriptor> DiscoverControllers(Assembly assembly)
    {
        var result = new List<ControllerDescriptor>();

        var controllerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t))
            .OrderBy(t => t.Name);

        foreach (var type in controllerTypes)
        {
            var routeAttr  = type.GetCustomAttribute<RouteAttribute>();
            var routePrefix = routeAttr?.Template ?? type.Name.Replace("Controller", "").ToLowerInvariant();

            var actions = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(m => BuildActionDescriptor(m))
                .Where(a => a != null)
                .Cast<ActionDescriptor>()
                .ToList();

            result.Add(new ControllerDescriptor
            {
                Name        = type.Name,
                FullTypeName= type.FullName ?? type.Name,
                RoutePrefix = routePrefix,
                Actions     = actions
            });
        }

        return result;
    }

    private static ActionDescriptor? BuildActionDescriptor(MethodInfo m)
    {
        string? httpMethod = null;
        string  template   = "";

        if (m.GetCustomAttribute<HttpGetAttribute>() is { } get)
        {
            httpMethod = "GET";
            template   = get.Template ?? "";
        }
        else if (m.GetCustomAttribute<HttpPostAttribute>() is { } post)
        {
            httpMethod = "POST";
            template   = post.Template ?? "";
        }
        else if (m.GetCustomAttribute<HttpPatchAttribute>() is { } patch)
        {
            httpMethod = "PATCH";
            template   = patch.Template ?? "";
        }
        else if (m.GetCustomAttribute<HttpDeleteAttribute>() is { } del)
        {
            httpMethod = "DELETE";
            template   = del.Template ?? "";
        }
        else if (m.GetCustomAttribute<HttpPutAttribute>() is { } put)
        {
            httpMethod = "PUT";
            template   = put.Template ?? "";
        }

        if (httpMethod == null) return null;

        var summary = m.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description ?? "";

        return new ActionDescriptor
        {
            MethodName   = m.Name,
            HttpMethod   = httpMethod,
            Template     = template,
            Summary      = summary,
            HasBody      = m.GetCustomAttribute<ConsumesAttribute>() != null ||
                           m.GetParameters().Any(p => p.GetCustomAttribute<FromBodyAttribute>() != null),
            ResponseTypes = m.GetCustomAttributes<ProducesResponseTypeAttribute>()
                             .Select(a => a.StatusCode)
                             .ToList()
        };
    }
}

// ── Descriptor models ─────────────────────────────────────────────────────────

public class ControllerDescriptor
{
    public string Name         { get; init; } = "";
    public string FullTypeName { get; init; } = "";
    public string RoutePrefix  { get; init; } = "";
    public List<ActionDescriptor> Actions { get; init; } = new();
}

public class ActionDescriptor
{
    public string     MethodName    { get; init; } = "";
    public string     HttpMethod    { get; init; } = "";
    public string     Template      { get; init; } = "";
    public string     Summary       { get; init; } = "";
    public bool       HasBody       { get; init; }
    public List<int>  ResponseTypes { get; init; } = new();
}

// ── Document filter — validates & enriches the Swagger doc ───────────────────

/// <summary>
/// IDocumentFilter that cross-checks discovered controller actions with what
/// Swashbuckle emitted. Logs any operation that is present in code but absent
/// from the Swagger document.
/// </summary>
public class AssemblyOperationsDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        var assembly    = typeof(Program).Assembly;
        var controllers = SwaggerAssemblyScanner.DiscoverControllers(assembly);

        foreach (var controller in controllers)
        {
            foreach (var action in controller.Actions)
            {
                var pathTemplate = BuildPath(controller.RoutePrefix, action.Template);
                var method       = new OperationType();

                bool parsed = Enum.TryParse<OperationType>(
                    char.ToUpperInvariant(action.HttpMethod[0]) + action.HttpMethod[1..].ToLowerInvariant(),
                    out method);

                if (!parsed) continue;

                if (!document.Paths.TryGetValue(pathTemplate, out var pathItem))
                {
                    Console.WriteLine(
                        $"[SwaggerScanner] WARNING — path not in Swagger: {action.HttpMethod} {pathTemplate}");
                    continue;
                }

                var hasOp = pathItem.Operations.ContainsKey(method);
                if (!hasOp)
                {
                    Console.WriteLine(
                        $"[SwaggerScanner] WARNING — operation not in Swagger: {action.HttpMethod} {pathTemplate}");
                }
            }
        }

        // Ensure all paths are tagged properly
        foreach (var (path, item) in document.Paths)
        {
            foreach (var (_, op) in item.Operations)
            {
                if (op.Tags.Count == 0)
                {
                    var segment = path.TrimStart('/').Split('/').Skip(1).FirstOrDefault() ?? "api";
                    op.Tags.Add(new OpenApiTag { Name = segment });
                }
            }
        }
    }

    private static string BuildPath(string prefix, string template)
    {
        var p = "/" + prefix.Trim('/');
        if (!string.IsNullOrWhiteSpace(template))
            p += "/" + template.Trim('/');
        return p;
    }
}
