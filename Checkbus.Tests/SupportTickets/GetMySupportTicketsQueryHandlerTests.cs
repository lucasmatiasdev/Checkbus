using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.SupportTickets.Queries;
using Checkbus.ApiService.Domain.Entities.Support;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.Tests.SupportTickets;

public class GetMySupportTicketsQueryHandlerTests
{
    private sealed class FakeSupportTicketRepository(IReadOnlyList<SupportTicket> tickets) : ISupportTicketRepository
    {
        public Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetMySupportTicketsQueryHandler.");

        public Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetMySupportTicketsQueryHandler.");

        public Task<IReadOnlyList<SupportTicket>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(tickets.Where(t => t.OrganizationId == organizationId).ToList() as IReadOnlyList<SupportTicket>);

        public Task<IReadOnlyList<SupportTicket>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetMySupportTicketsQueryHandler.");

        public Task UpdateAsync(SupportTicket ticket, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetMySupportTicketsQueryHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Planificador";
        public string? Email => "planificador@checkbus-demo.com";
    }

    private static SupportTicket CreateTicket(Guid organizationId, string subject, DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        CreatedByUserId = Guid.NewGuid(),
        Category = SupportTicketCategory.ConsultaGeneral,
        Subject = subject,
        Description = "Descripcion de prueba",
        Priority = SupportTicketPriority.Media,
        Status = SupportTicketStatus.Abierto,
        CreatedAt = createdAt
    };

    [Fact]
    public async Task Handle_ReturnsOnlyCallersOrganizationTickets_OrderedByCreatedAtDescending()
    {
        var organizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();
        var oldest = CreateTicket(organizationId, "Mas viejo", DateTime.UtcNow.AddDays(-2));
        var newest = CreateTicket(organizationId, "Mas nuevo", DateTime.UtcNow.AddDays(-1));
        var otherOrgTicket = CreateTicket(otherOrganizationId, "Ajeno", DateTime.UtcNow);
        var handler = new GetMySupportTicketsQueryHandler(
            new FakeSupportTicketRepository([oldest, newest, otherOrgTicket]), new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(new GetMySupportTicketsQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Equal(newest.Id, result[0].Id);
        Assert.Equal(oldest.Id, result[1].Id);
        Assert.All(result, dto => Assert.NotEqual(otherOrgTicket.Id, dto.Id));
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetMySupportTicketsQueryHandler(
            new FakeSupportTicketRepository([]), new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetMySupportTicketsQuery(), TestContext.Current.CancellationToken));
    }
}
