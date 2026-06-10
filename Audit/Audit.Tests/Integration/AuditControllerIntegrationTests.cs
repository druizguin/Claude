using Audit.Api.Models;
using Audit.Dom.Entities;
using Audit.Dom.Enums;
using Audit.Dom.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Json;

namespace Audit.Tests.Integration;

public class AuditControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IAuditService> _serviceMock = new();

    public AuditControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remover el servicio real y usar mock
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IAuditService));
                if (descriptor != null) services.Remove(descriptor);

                services.AddScoped<IAuditService>(_ => _serviceMock.Object);
            });
        });
    }

    [Fact]
    public async Task GET_api_audits_Returns200WithData()
    {
        var entries = new List<AuditEntry>
        {
            new() { Id = Guid.NewGuid(), UserId = "u1", EntityName = "Persona", Action = AuditAction.Create, Timestamp = DateTime.UtcNow }
        };

        _serviceMock.Setup(s => s.GetAllAuditsAsync(0, 20, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(entries);
        _serviceMock.Setup(s => s.GetTotalCountAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(1);

        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/audits");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Persona");
    }

    [Fact]
    public async Task GET_api_audits_ById_Returns404_WhenNotFound()
    {
        _serviceMock.Setup(s => s.GetAuditByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((AuditEntry?)null);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/audits/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_api_audits_ById_Returns200_WhenFound()
    {
        var id = Guid.NewGuid();
        var entry = new AuditEntry { Id = id, UserId = "u1", EntityName = "Producto", Action = AuditAction.Update, Timestamp = DateTime.UtcNow };

        _serviceMock.Setup(s => s.GetAuditByIdAsync(id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(entry);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/audits/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_api_audits_Returns201_WithLocation()
    {
        var newId = Guid.NewGuid();
        _serviceMock.Setup(s => s.CreateAuditAsync(It.IsAny<AuditEntry>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(newId);

        var request = new CreateAuditRequest
        {
            UserId = "test-user",
            EntityId = Guid.NewGuid(),
            EntityName = "Persona",
            Action = AuditAction.Create,
            Details =
            [
                new CreateAuditDetailRequest { PropertyName = "Nombre", OldValue = null, NewValue = "Juan" }
            ]
        };

        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/audits", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task GET_api_audits_user_Returns200WithFilteredData()
    {
        const string userId = "filtered-user";
        var entries = new List<AuditEntry>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, EntityName = "Persona", Action = AuditAction.Update, Timestamp = DateTime.UtcNow }
        };

        _serviceMock.Setup(s => s.GetAuditsByUserIdAsync(userId, 0, 20, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(entries);
        _serviceMock.Setup(s => s.GetCountByUserIdAsync(userId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(1);

        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/audits/user/{userId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain(userId);
    }
}
