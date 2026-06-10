using Audit.Dom.Entities;
using Audit.Dom.Enums;
using FluentAssertions;
using TechTalk.SpecFlow;

namespace Audit.BDD.Tests.Steps;

[Binding]
public class AuditRetrievalSteps
{
    private readonly AuditTestContext _ctx;

    public AuditRetrievalSteps(AuditTestContext ctx) => _ctx = ctx;

    [Given(@"I have created (\d+) audit entries for user ""(.*)""")]
    public async Task GivenCreatedEntriesForUser(int count, string userId)
    {
        for (int i = 0; i < count; i++)
        {
            await _ctx.AuditService.CreateAuditAsync(new AuditEntry
            {
                UserId = userId,
                EntityId = Guid.NewGuid(),
                EntityName = "TestEntity",
                Action = AuditAction.Create
            });
        }
    }

    [Given(@"I have created an audit with a property change for ""(.*)"" from ""(.*)"" to ""(.*)""")]
    public async Task GivenCreatedAuditWithChange(string property, string oldVal, string newVal)
    {
        _ctx.LastCreatedId = await _ctx.AuditService.CreateAuditAsync(new AuditEntry
        {
            UserId = "detail-test-user",
            EntityId = Guid.NewGuid(),
            EntityName = "TestEntity",
            Action = AuditAction.Update,
            Details =
            [
                new AuditDetail { PropertyName = property, OldValue = oldVal, NewValue = newVal }
            ]
        });
    }

    [When(@"I query audits for user ""(.*)""")]
    public async Task WhenQueryAuditsForUser(string userId)
    {
        _ctx.QueryResult = await _ctx.AuditService.GetAuditsByUserIdAsync(userId, 0, 100);
    }

    [When(@"I query audits with page size (\d+) and page (\d+)")]
    public async Task WhenQueryAuditsWithPaging(int pageSize, int page)
    {
        _ctx.QueryResult = await _ctx.AuditService.GetAllAuditsAsync((page - 1) * pageSize, pageSize);
    }

    [When(@"I get the total count of audits for user ""(.*)""")]
    public async Task WhenGetTotalCount(string userId)
    {
        _ctx.CountResult = await _ctx.AuditService.GetCountByUserIdAsync(userId);
    }

    [Then(@"the result should contain (\d+) entries")]
    public void ThenResultShouldContain(int count)
    {
        _ctx.QueryResult.Should().HaveCount(count);
    }

    [Then(@"the result should contain at most (\d+) entries")]
    public void ThenResultShouldContainAtMost(int count)
    {
        _ctx.QueryResult.Count().Should().BeLessThanOrEqualTo(count);
    }

    [Then(@"all entries should belong to user ""(.*)""")]
    public void ThenAllEntriesBelongToUser(string userId)
    {
        _ctx.QueryResult.Should().AllSatisfy(e => e.UserId.Should().Be(userId));
    }

    [Then(@"the count should be at least (\d+)")]
    public void ThenCountAtLeast(int min)
    {
        _ctx.CountResult.Should().BeGreaterThanOrEqualTo(min);
    }

    [Then(@"the audit details should contain a change for property ""(.*)""")]
    public void ThenDetailsShouldContainProperty(string property)
    {
        _ctx.RetrievedEntry!.Details.Should().Contain(d => d.PropertyName == property);
    }

    [Then(@"the old value for ""(.*)"" should be ""(.*)""")]
    public void ThenOldValueShouldBe(string property, string value)
    {
        _ctx.RetrievedEntry!.Details.First(d => d.PropertyName == property).OldValue.Should().Be(value);
    }

    [Then(@"the new value for ""(.*)"" should be ""(.*)""")]
    public void ThenNewValueShouldBe(string property, string value)
    {
        _ctx.RetrievedEntry!.Details.First(d => d.PropertyName == property).NewValue.Should().Be(value);
    }
}
