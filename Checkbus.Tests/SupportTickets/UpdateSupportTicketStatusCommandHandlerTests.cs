using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.SupportTickets.Commands;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Support;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.SupportTickets;

namespace Checkbus.Tests.SupportTickets;

public class UpdateSupportTicketStatusCommandHandlerTests
{
    // Local fake, not the shared one in the other *CommandHandlerTests files — this one needs
    // GetByIdAsync/UpdateAsync wired to real behavior and tracks whether UpdateAsync was reached,
    // which the other handlers' fakes have no reason to do.
    private sealed class FakeSupportTicketRepository(SupportTicket? ticket) : ISupportTicketRepository
    {
        public SupportTicket? UpdatedTicket { get; private set; }

        public Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateSupportTicketStatusCommandHandler.");

        public Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(ticket is not null && ticket.Id == id ? ticket : null);

        public Task<IReadOnlyList<SupportTicket>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateSupportTicketStatusCommandHandler.");

        public Task<IReadOnlyList<SupportTicket>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateSupportTicketStatusCommandHandler.");

        public Task UpdateAsync(SupportTicket ticket, CancellationToken cancellationToken)
        {
            UpdatedTicket = ticket;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeOrganizationRepository(string? callerSlug) : IOrganizationRepository
    {
        public Task<string?> GetSlugByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(callerSlug);

        public Task<string?> GetNameByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateSupportTicketStatusCommandHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateSupportTicketStatusCommandHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "CheckbusSoporte";
        public string? Email => "soporte@checkbus-interno.com";
    }

    private static SupportTicket CreateTicket(Guid organizationId) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        CreatedByUserId = Guid.NewGuid(),
        Category = SupportTicketCategory.ConsultaGeneral,
        Subject = "Ticket de prueba",
        Description = "Descripcion de prueba",
        Priority = SupportTicketPriority.Media,
        Status = SupportTicketStatus.Abierto,
        CreatedAt = DateTime.UtcNow.AddDays(-1)
    };

    [Fact]
    public async Task Handle_CallerFromSentinelOrganization_UpdatesStatusAndUpdatedAt_RegardlessOfTicketOwner()
    {
        // Cross-tenant on purpose: the ticket belongs to some client organization, not the
        // caller's (sentinel) one — that is exactly the point of this panel.
        var ticket = CreateTicket(Guid.NewGuid());
        var ticketRepository = new FakeSupportTicketRepository(ticket);
        var handler = new UpdateSupportTicketStatusCommandHandler(
            ticketRepository,
            new FakeOrganizationRepository(CheckbusInternalOrganization.Slug),
            new FakeCurrentUserService(Guid.NewGuid()));

        await handler.Handle(
            new UpdateSupportTicketStatusCommand { TicketId = ticket.Id, NewStatus = SupportTicketStatus.Resuelto },
            TestContext.Current.CancellationToken);

        Assert.NotNull(ticketRepository.UpdatedTicket);
        Assert.Equal(ticket.Id, ticketRepository.UpdatedTicket!.Id);
        Assert.Equal(SupportTicketStatus.Resuelto, ticketRepository.UpdatedTicket.Status);
        Assert.NotNull(ticketRepository.UpdatedTicket.UpdatedAt);
    }

    [Fact]
    public async Task Handle_CallerOrganizationIsNotSentinel_ThrowsSupportTicketAccessDeniedException_AndNeverCallsUpdate()
    {
        var ticket = CreateTicket(Guid.NewGuid());
        var ticketRepository = new FakeSupportTicketRepository(ticket);
        var handler = new UpdateSupportTicketStatusCommandHandler(
            ticketRepository,
            new FakeOrganizationRepository("otra-organizacion"),
            new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<SupportTicketAccessDeniedException>(() => handler.Handle(
            new UpdateSupportTicketStatusCommand { TicketId = ticket.Id, NewStatus = SupportTicketStatus.Resuelto },
            TestContext.Current.CancellationToken));

        Assert.Null(ticketRepository.UpdatedTicket);
    }

    [Fact]
    public async Task Handle_TicketIdDoesNotExist_ThrowsSupportTicketNotFoundException()
    {
        // Unlike GetSupportTicketAttachmentQueryHandler (which collapses "not found" and
        // "belongs to another organization" into the same IDOR-safe null, because a regular
        // caller must not learn whether a ticket exists in some other tenant), this is the
        // Checkbus internal staff panel: the caller legitimately sees every organization's
        // tickets, so a missing TicketId is a genuine 404 with nothing to hide.
        var ticketRepository = new FakeSupportTicketRepository(ticket: null);
        var handler = new UpdateSupportTicketStatusCommandHandler(
            ticketRepository,
            new FakeOrganizationRepository(CheckbusInternalOrganization.Slug),
            new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<SupportTicketNotFoundException>(() => handler.Handle(
            new UpdateSupportTicketStatusCommand { TicketId = Guid.NewGuid(), NewStatus = SupportTicketStatus.Resuelto },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new UpdateSupportTicketStatusCommandHandler(
            new FakeSupportTicketRepository(ticket: null),
            new FakeOrganizationRepository(CheckbusInternalOrganization.Slug),
            new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new UpdateSupportTicketStatusCommand { TicketId = Guid.NewGuid(), NewStatus = SupportTicketStatus.Resuelto },
            TestContext.Current.CancellationToken));
    }
}
