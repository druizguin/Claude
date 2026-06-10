using Audit.Dom.Entities;
using Audit.Dom.Enums;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace Audit.BDD.Tests.Steps;

[Binding]
public class AuditCreationSteps
{
    private readonly AuditTestContext _ctx;

    public AuditCreationSteps(AuditTestContext ctx) => _ctx = ctx;

    [Given(@"the audit system is initialized")]
    public void GivenTheAuditSystemIsInitialized()
    {
        _ctx.AuditService.Should().NotBeNull();
    }

    [Given(@"I have a valid audit entry for entity ""(.*)"" with action ""(.*)""")]
    public void GivenIHaveAValidAuditEntry(string entityName, string action)
    {
        _ctx.CurrentEntry = new AuditEntry
        {
            EntityId = Guid.NewGuid(),
            EntityName = entityName,
            Action = Enum.Parse<AuditAction>(action)
        };
    }

    [Given(@"the user id is ""(.*)""")]
    public void GivenTheUserIdIs(string userId)
    {
        _ctx.CurrentEntry.UserId = userId;
    }

    [Given(@"the audit has a property change for ""(.*)"" from ""(.*)"" to ""(.*)""")]
    public void GivenAuditHasPropertyChange(string property, string oldVal, string newVal)
    {
        _ctx.CurrentEntry.Details.Add(new AuditDetail
        {
            PropertyName = property,
            OldValue = oldVal,
            NewValue = newVal
        });
    }

    [When(@"I create the audit entry")]
    public async Task WhenICreateTheAuditEntry()
    {
        _ctx.LastCreatedId = await _ctx.AuditService.CreateAuditAsync(_ctx.CurrentEntry);
    }

    [When(@"I retrieve the audit by its id")]
    public async Task WhenIRetrieveTheAuditById()
    {
        _ctx.RetrievedEntry = await _ctx.AuditService.GetAuditByIdAsync(_ctx.LastCreatedId);
    }

    [Then(@"the audit entry should be saved successfully")]
    public void ThenTheAuditEntryShouldBeSaved()
    {
        _ctx.LastCreatedId.Should().NotBe(Guid.Empty);
    }

    [Then(@"the returned id should not be empty")]
    public void ThenReturnedIdShouldNotBeEmpty()
    {
        _ctx.LastCreatedId.Should().NotBe(Guid.Empty);
    }

    [Then(@"the audit timestamp should be set automatically")]
    public void ThenTimestampShouldBeSet()
    {
        _ctx.CurrentEntry.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Then(@"the audit should have (\d+) detail records")]
    public async Task ThenAuditShouldHaveDetailRecords(int count)
    {
        var entry = await _ctx.AuditService.GetAuditByIdAsync(_ctx.LastCreatedId);
        entry!.Details.Should().HaveCount(count);
    }

    [Then(@"the retrieved audit should not be null")]
    public void ThenRetrievedAuditShouldNotBeNull()
    {
        _ctx.RetrievedEntry.Should().NotBeNull();
    }

    [Then(@"the retrieved audit entity name should be ""(.*)""")]
    public void ThenEntityNameShouldBe(string entityName)
    {
        _ctx.RetrievedEntry!.EntityName.Should().Be(entityName);
    }

    [Then(@"the retrieved audit action should be ""(.*)""")]
    public void ThenActionShouldBe(string action)
    {
        _ctx.RetrievedEntry!.Action.Should().Be(Enum.Parse<AuditAction>(action));
    }
}
