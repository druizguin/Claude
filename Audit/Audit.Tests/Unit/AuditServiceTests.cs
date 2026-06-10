using Audit.Dom.Entities;
using Audit.Dom.Enums;
using Audit.Dom.Interfaces;
using Audit.Svc;
using FluentAssertions;
using Moq;

namespace Audit.Tests.Unit;

public class AuditServiceTests
{
    private readonly Mock<IAuditRepository> _repoMock = new();
    private readonly AuditService _sut;

    public AuditServiceTests()
    {
        _sut = new AuditService(_repoMock.Object);
    }

    [Fact]
    public async Task CreateAuditAsync_AssignsIdAndTimestamp_WhenNotProvided()
    {
        var entry = new AuditEntry
        {
            UserId = "user-1",
            EntityId = Guid.NewGuid(),
            EntityName = "Persona",
            Action = AuditAction.Create
        };

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<AuditEntry>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((AuditEntry e, CancellationToken _) => e.Id);

        await _sut.CreateAuditAsync(entry);

        entry.Id.Should().NotBe(Guid.Empty);
        entry.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateAuditAsync_AssignsDetailIds_WhenMissing()
    {
        var entry = new AuditEntry
        {
            UserId = "user-1",
            EntityId = Guid.NewGuid(),
            EntityName = "Producto",
            Action = AuditAction.Update,
            Details =
            [
                new AuditDetail { PropertyName = "Precio", OldValue = "10", NewValue = "20" }
            ]
        };

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<AuditEntry>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((AuditEntry e, CancellationToken _) => e.Id);

        await _sut.CreateAuditAsync(entry);

        entry.Details.Should().AllSatisfy(d =>
        {
            d.Id.Should().NotBe(Guid.Empty);
            d.AuditId.Should().Be(entry.Id);
        });
    }

    [Fact]
    public async Task CreateAuditAsync_PreservesExistingId_WhenProvided()
    {
        var existingId = Guid.NewGuid();
        var entry = new AuditEntry
        {
            Id = existingId,
            UserId = "user-1",
            EntityId = Guid.NewGuid(),
            EntityName = "Persona",
            Action = AuditAction.Read
        };

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<AuditEntry>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(existingId);

        var resultId = await _sut.CreateAuditAsync(entry);

        resultId.Should().Be(existingId);
        entry.Id.Should().Be(existingId);
    }

    [Fact]
    public async Task GetAuditByIdAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((AuditEntry?)null);

        var result = await _sut.GetAuditByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAuditsByUserIdAsync_CallsRepositoryWithCorrectParameters()
    {
        const string userId = "test-user";
        const int skip = 10;
        const int take = 5;

        _repoMock.Setup(r => r.GetByUserIdAsync(userId, skip, take, It.IsAny<CancellationToken>()))
                 .ReturnsAsync([]);

        await _sut.GetAuditsByUserIdAsync(userId, skip, take);

        _repoMock.Verify(r => r.GetByUserIdAsync(userId, skip, take, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTotalCountAsync_DelegatesToRepository()
    {
        _repoMock.Setup(r => r.CountAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(42);

        var count = await _sut.GetTotalCountAsync();

        count.Should().Be(42);
    }

    [Fact]
    public async Task GetCountByUserIdAsync_DelegatesToRepository()
    {
        const string userId = "user-x";
        _repoMock.Setup(r => r.CountByUserIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(7);

        var count = await _sut.GetCountByUserIdAsync(userId);

        count.Should().Be(7);
    }
}
