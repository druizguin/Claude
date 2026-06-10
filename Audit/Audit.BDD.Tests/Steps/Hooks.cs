using TechTalk.SpecFlow;

namespace Audit.BDD.Tests.Steps;

[Binding]
public sealed class Hooks
{
    private readonly AuditTestContext _context;

    public Hooks(AuditTestContext context)
    {
        _context = context;
    }

    [BeforeScenario(Order = 1)]
    public async Task BeforeScenario()
    {
        await _context.InitializeAsync();
    }

    [AfterScenario(Order = 1)]
    public void AfterScenario()
    {
        _context.Cleanup();
    }
}
