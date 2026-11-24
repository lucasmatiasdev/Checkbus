using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.SupportTickets.Commands;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Audit;
using Checkbus.ApiService.Domain.Entities.Support;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.SupportTickets;
using Microsoft.Extensions.Logging.Abstractions;

namespace Checkbus.Tests.SupportTickets;

public class UpdateSupportTicketStatusCommandHandlerTests
{

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
        public Task AddAsync(Organization organization, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateSupportTicketStatusCommandHandler.");

        public Task<string?> GetSlugByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(callerSlug);

        public Task<string?> GetNameByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateSupportTicketStatusCommandHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateSupportTicketStatusCommandHandler.");

        public Task<bool> CuitExistsAsync(string cuit, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateSupportTicketStatusCommandHandler.");

        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateSupportTicketStatusCommandHandler.");

        public Task<IReadOnlyList<Organization>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateSupportTicketStatusCommandHandler.");
    }

    private sealed class FakeAuditLogRepository(bool throwOnAdd = false) : IAuditLogRepository
    {
        public List<AuditLogEntry> AddedEntries { get; } = [];

        public Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken)
        {
            if (throwOnAdd)
            {
                throw new InvalidOperationException("Simulated audit log write failure.");
            }

            AddedEntries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditLogEntry>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateSupportTicketStatusCommandHandler.");

        public Task<IReadOnlyList<AuditLogEntry>> GetAllAsync(CancellationToken cancellationToken)
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
        Status = SupportTicketStatus.Abierto,
        CreatedAt = DateTime.UtcNow.AddDays(-1)
    };

    [Fact]
    public async Task Handle_CallerFromSentinelOrganization_UpdatesStatusAndUpdatedAt_RegardlessOfTicketOwner()
    {

        var ticket = CreateTicket(Guid.NewGuid());
        var ticketRepository = new FakeSupportTicketRepository(ticket);
        var handler = new UpdateSupportTicketStatusCommandHandler(
            ticketRepository,
            new FakeOrganizationRepository(CheckbusInternalOrganization.Slug),
            new FakeCurrentUserService(Guid.NewGuid()),
            new FakeAuditLogRepository(),
            NullLogger<UpdateSupportTicketStatusCommandHandler>.Instance);

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
            new FakeCurrentUserService(Guid.NewGuid()),
            new FakeAuditLogRepository(),
            NullLogger<UpdateSupportTicketStatusCommandHandler>.Instance);

        await Assert.ThrowsAsync<SupportTicketAccessDeniedException>(() => handler.Handle(
            new UpdateSupportTicketStatusCommand { TicketId = ticket.Id, NewStatus = SupportTicketStatus.Resuelto },
            TestContext.Current.CancellationToken));

        Assert.Null(ticketRepository.UpdatedTicket);
    }

    [Fact]
    public async Task Handle_TicketIdDoesNotExist_ThrowsSupportTicketNotFoundException()
    {

        var ticketRepository = new FakeSupportTicketRepository(ticket: null);
        var handler = new UpdateSupportTicketStatusCommandHandler(
            ticketRepository,
            new FakeOrganizationRepository(CheckbusInternalOrganization.Slug),
            new FakeCurrentUserService(Guid.NewGuid()),
            new FakeAuditLogRepository(),
            NullLogger<UpdateSupportTicketStatusCommandHandler>.Instance);

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
            new FakeCurrentUserService(organizationId: null),
            new FakeAuditLogRepository(),
            NullLogger<UpdateSupportTicketStatusCommandHandler>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new UpdateSupportTicketStatusCommand { TicketId = Guid.NewGuid(), NewStatus = SupportTicketStatus.Resuelto },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_CallerFromSentinelOrganization_WritesEstadoTicketSoporteCambiadoAuditLogEntry()
    {
        var ticket = CreateTicket(Guid.NewGuid());
        var ticketRepository = new FakeSupportTicketRepository(ticket);
        var auditLogRepository = new FakeAuditLogRepository();
        var handler = new UpdateSupportTicketStatusCommandHandler(
            ticketRepository,
            new FakeOrganizationRepository(CheckbusInternalOrganization.Slug),
            new FakeCurrentUserService(Guid.NewGuid()),
            auditLogRepository,
            NullLogger<UpdateSupportTicketStatusCommandHandler>.Instance);

        await handler.Handle(
            new UpdateSupportTicketStatusCommand { TicketId = ticket.Id, NewStatus = SupportTicketStatus.Resuelto },
            TestContext.Current.CancellationToken);

        var entry = Assert.Single(auditLogRepository.AddedEntries);
        Assert.Equal(AuditAction.EstadoTicketSoporteCambiado, entry.Action);
        Assert.Equal("SupportTicket", entry.EntityType);
        Assert.Equal(ticket.Id, entry.EntityId);
        Assert.Equal(ticket.OrganizationId, entry.OrganizationId);
    }

    [Fact]
    public async Task Handle_AuditLogWriteFails_StatusUpdateStillSucceeds()
    {
        var ticket = CreateTicket(Guid.NewGuid());
        var ticketRepository = new FakeSupportTicketRepository(ticket);
        var auditLogRepository = new FakeAuditLogRepository(throwOnAdd: true);
        var handler = new UpdateSupportTicketStatusCommandHandler(
            ticketRepository,
            new FakeOrganizationRepository(CheckbusInternalOrganization.Slug),
            new FakeCurrentUserService(Guid.NewGuid()),
            auditLogRepository,
            NullLogger<UpdateSupportTicketStatusCommandHandler>.Instance);

        await handler.Handle(
            new UpdateSupportTicketStatusCommand { TicketId = ticket.Id, NewStatus = SupportTicketStatus.Resuelto },
            TestContext.Current.CancellationToken);

        Assert.NotNull(ticketRepository.UpdatedTicket);
        Assert.Equal(SupportTicketStatus.Resuelto, ticketRepository.UpdatedTicket!.Status);
        Assert.Empty(auditLogRepository.AddedEntries);
    }
}
