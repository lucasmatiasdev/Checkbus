using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.SupportTickets.Queries;
using Checkbus.ApiService.Domain.Entities.Support;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.Tests.SupportTickets;

public class CheckDuplicateSupportTicketQueryHandlerTests
{
    private sealed class FakeSupportTicketRepository(IReadOnlyList<SupportTicket> tickets) : ISupportTicketRepository
    {
        public Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CheckDuplicateSupportTicketQueryHandler.");

        public Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CheckDuplicateSupportTicketQueryHandler.");

        public Task<IReadOnlyList<SupportTicket>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(tickets.Where(t => t.OrganizationId == organizationId).ToList() as IReadOnlyList<SupportTicket>);

        public Task<IReadOnlyList<SupportTicket>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CheckDuplicateSupportTicketQueryHandler.");

        public Task UpdateAsync(SupportTicket ticket, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CheckDuplicateSupportTicketQueryHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Planificador";
        public string? Email => "planificador@checkbus-demo.com";
    }

    private static SupportTicket CreateTicket(
        Guid organizationId, SupportTicketCategory category, SupportTicketStatus status, DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        CreatedByUserId = Guid.NewGuid(),
        Category = category,
        Subject = "Asunto de prueba",
        Description = "Descripcion de prueba con longitud suficiente.",
        Status = status,
        CreatedAt = createdAt
    };

    private static CheckDuplicateSupportTicketQueryHandler CreateHandler(
        IReadOnlyList<SupportTicket> tickets, Guid? organizationId)
        => new(new FakeSupportTicketRepository(tickets), new FakeCurrentUserService(organizationId));

    [Fact]
    public async Task Handle_OpenTicketSameCategoryWithinWindow_ReturnsTrue()
    {
        var organizationId = Guid.NewGuid();
        var existing = CreateTicket(
            organizationId, SupportTicketCategory.ProblemaPago, SupportTicketStatus.Abierto, DateTime.UtcNow.AddHours(-1));
        var handler = CreateHandler([existing], organizationId);

        var result = await handler.Handle(
            new CheckDuplicateSupportTicketQuery { Category = SupportTicketCategory.ProblemaPago },
            TestContext.Current.CancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task Handle_NoTicketsInCategory_ReturnsFalse()
    {
        var organizationId = Guid.NewGuid();
        var existing = CreateTicket(
            organizationId, SupportTicketCategory.ProblemaViaje, SupportTicketStatus.Abierto, DateTime.UtcNow.AddHours(-1));
        var handler = CreateHandler([existing], organizationId);

        var result = await handler.Handle(
            new CheckDuplicateSupportTicketQuery { Category = SupportTicketCategory.ProblemaPago },
            TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_SameCategoryOutsideWindow_ReturnsFalse()
    {
        var organizationId = Guid.NewGuid();
        var existing = CreateTicket(
            organizationId, SupportTicketCategory.ProblemaPago, SupportTicketStatus.Abierto, DateTime.UtcNow.AddHours(-25));
        var handler = CreateHandler([existing], organizationId);

        var result = await handler.Handle(
            new CheckDuplicateSupportTicketQuery { Category = SupportTicketCategory.ProblemaPago },
            TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Theory]
    [InlineData(SupportTicketStatus.Resuelto)]
    [InlineData(SupportTicketStatus.Cerrado)]
    public async Task Handle_SameCategoryButClosedOrResolved_ReturnsFalse(SupportTicketStatus status)
    {
        var organizationId = Guid.NewGuid();
        var existing = CreateTicket(organizationId, SupportTicketCategory.ProblemaPago, status, DateTime.UtcNow.AddHours(-1));
        var handler = CreateHandler([existing], organizationId);

        var result = await handler.Handle(
            new CheckDuplicateSupportTicketQuery { Category = SupportTicketCategory.ProblemaPago },
            TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_MatchingTicketBelongsToAnotherOrganization_ReturnsFalse()
    {
        var organizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();
        var existing = CreateTicket(
            otherOrganizationId, SupportTicketCategory.ProblemaPago, SupportTicketStatus.Abierto, DateTime.UtcNow.AddHours(-1));
        var handler = CreateHandler([existing], organizationId);

        var result = await handler.Handle(
            new CheckDuplicateSupportTicketQuery { Category = SupportTicketCategory.ProblemaPago },
            TestContext.Current.CancellationToken);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = CreateHandler([], organizationId: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new CheckDuplicateSupportTicketQuery { Category = SupportTicketCategory.ProblemaPago },
            TestContext.Current.CancellationToken));
    }
}
