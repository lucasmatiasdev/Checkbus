using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.SupportTickets.Queries;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Support;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.SupportTickets;

namespace Checkbus.Tests.SupportTickets;

public class GetAllSupportTicketsQueryHandlerTests
{
    // Local fake, not the shared one in GetMySupportTicketsQueryHandlerTests/
    // CreateSupportTicketCommandHandlerTests/GetSupportTicketAttachmentQueryHandlerTests —
    // this handler needs GetAllAsync wired to real data while the other three stub it out.
    private sealed class FakeSupportTicketRepository(IReadOnlyList<SupportTicket> tickets) : ISupportTicketRepository
    {
        public Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetAllSupportTicketsQueryHandler.");

        public Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetAllSupportTicketsQueryHandler.");

        public Task<IReadOnlyList<SupportTicket>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetAllSupportTicketsQueryHandler.");

        public Task<IReadOnlyList<SupportTicket>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult(tickets);

        public Task UpdateAsync(SupportTicket ticket, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetAllSupportTicketsQueryHandler.");
    }

    private sealed class FakeOrganizationRepository(string? callerSlug, IReadOnlyDictionary<Guid, string?> namesById) : IOrganizationRepository
    {
        public Task<string?> GetSlugByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(callerSlug);

        public Task<string?> GetNameByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(namesById.TryGetValue(organizationId, out var name) ? name : null);

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetAllSupportTicketsQueryHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "CheckbusSoporte";
        public string? Email => "soporte@checkbus-interno.com";
    }

    private static SupportTicket CreateTicket(Guid organizationId, string subject) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        CreatedByUserId = Guid.NewGuid(),
        Category = SupportTicketCategory.ConsultaGeneral,
        Subject = subject,
        Description = "Descripcion de prueba",
        Priority = SupportTicketPriority.Media,
        Status = SupportTicketStatus.Abierto,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_CallerFromSentinelOrganization_ReturnsAllTicketsAcrossOrganizationsWithResolvedNames()
    {
        var sentinelOrganizationId = Guid.NewGuid();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var ticketA = CreateTicket(organizationA, "Ticket de Org A");
        var ticketB = CreateTicket(organizationB, "Ticket de Org B");
        var handler = new GetAllSupportTicketsQueryHandler(
            new FakeSupportTicketRepository([ticketA, ticketB]),
            new FakeOrganizationRepository(
                CheckbusInternalOrganization.Slug,
                new Dictionary<Guid, string?> { [organizationA] = "Organizacion A", [organizationB] = "Organizacion B" }),
            new FakeCurrentUserService(sentinelOrganizationId));

        var result = await handler.Handle(new GetAllSupportTicketsQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        var dtoA = Assert.Single(result, dto => dto.Id == ticketA.Id);
        Assert.Equal("Organizacion A", dtoA.OrganizationName);
        var dtoB = Assert.Single(result, dto => dto.Id == ticketB.Id);
        Assert.Equal("Organizacion B", dtoB.OrganizationName);
    }

    [Fact]
    public async Task Handle_CallerOrganizationIsNotSentinel_ThrowsSupportTicketAccessDeniedException()
    {
        // The security-critical case: having the CheckbusSoporte Role claim is not enough — the
        // caller's own OrganizationId must resolve to CheckbusInternalOrganization.Slug.
        var callerOrganizationId = Guid.NewGuid();
        var handler = new GetAllSupportTicketsQueryHandler(
            new FakeSupportTicketRepository([CreateTicket(Guid.NewGuid(), "Ticket ajeno")]),
            new FakeOrganizationRepository("otra-organizacion", new Dictionary<Guid, string?>()),
            new FakeCurrentUserService(callerOrganizationId));

        await Assert.ThrowsAsync<SupportTicketAccessDeniedException>(
            () => handler.Handle(new GetAllSupportTicketsQuery(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_OrganizationNameNotFound_FallsBackToDesconocida()
    {
        var sentinelOrganizationId = Guid.NewGuid();
        var ticket = CreateTicket(Guid.NewGuid(), "Ticket sin organizacion resoluble");
        var handler = new GetAllSupportTicketsQueryHandler(
            new FakeSupportTicketRepository([ticket]),
            new FakeOrganizationRepository(CheckbusInternalOrganization.Slug, new Dictionary<Guid, string?>()),
            new FakeCurrentUserService(sentinelOrganizationId));

        var result = await handler.Handle(new GetAllSupportTicketsQuery(), TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal("(desconocida)", dto.OrganizationName);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetAllSupportTicketsQueryHandler(
            new FakeSupportTicketRepository([]),
            new FakeOrganizationRepository(CheckbusInternalOrganization.Slug, new Dictionary<Guid, string?>()),
            new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetAllSupportTicketsQuery(), TestContext.Current.CancellationToken));
    }
}
