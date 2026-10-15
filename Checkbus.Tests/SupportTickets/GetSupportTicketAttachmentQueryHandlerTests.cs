using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.SupportTickets.Queries;
using Checkbus.ApiService.Domain.Entities.Support;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.Tests.SupportTickets;

public class GetSupportTicketAttachmentQueryHandlerTests
{
    private sealed class FakeSupportTicketRepository(SupportTicket? ticket) : ISupportTicketRepository
    {
        public Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetSupportTicketAttachmentQueryHandler.");

        public Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(ticket);

        public Task<IReadOnlyList<SupportTicket>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetSupportTicketAttachmentQueryHandler.");

        public Task<IReadOnlyList<SupportTicket>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetSupportTicketAttachmentQueryHandler.");

        public Task UpdateAsync(SupportTicket ticket, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetSupportTicketAttachmentQueryHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Planificador";
        public string? Email => "planificador@checkbus-demo.com";
    }

    private static SupportTicket CreateTicket(Guid organizationId, string? attachmentKey) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        CreatedByUserId = Guid.NewGuid(),
        Category = SupportTicketCategory.ProblemaTecnico,
        Subject = "No puedo ver mis reservas",
        Description = "Descripcion de prueba",
        Priority = SupportTicketPriority.Media,
        Status = SupportTicketStatus.Abierto,
        AttachmentKey = attachmentKey,
        AttachmentFileName = attachmentKey is not null ? "captura.png" : null,
        AttachmentContentType = attachmentKey is not null ? "image/png" : null,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_TicketDoesNotExist_ReturnsNull()
    {
        var handler = new GetSupportTicketAttachmentQueryHandler(
            new FakeSupportTicketRepository(ticket: null), new FakeCurrentUserService(Guid.NewGuid()));

        var result = await handler.Handle(
            new GetSupportTicketAttachmentQuery { TicketId = Guid.NewGuid() }, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_TicketBelongsToOtherOrganization_ReturnsNull()
    {
        // IDOR-safe: a ticket owned by another organization collapses into the same null
        // result as "does not exist" — the caller must not be able to distinguish the two.
        var ticket = CreateTicket(Guid.NewGuid(), "support-tickets/ticket/file.png");
        var handler = new GetSupportTicketAttachmentQueryHandler(
            new FakeSupportTicketRepository(ticket), new FakeCurrentUserService(Guid.NewGuid()));

        var result = await handler.Handle(
            new GetSupportTicketAttachmentQuery { TicketId = ticket.Id }, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_TicketHasNoAttachment_ReturnsNull()
    {
        var organizationId = Guid.NewGuid();
        var ticket = CreateTicket(organizationId, attachmentKey: null);
        var handler = new GetSupportTicketAttachmentQueryHandler(
            new FakeSupportTicketRepository(ticket), new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(
            new GetSupportTicketAttachmentQuery { TicketId = ticket.Id }, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_TicketBelongsToCallerAndHasAttachment_ReturnsDto()
    {
        var organizationId = Guid.NewGuid();
        var ticket = CreateTicket(organizationId, "support-tickets/ticket/file.png");
        var handler = new GetSupportTicketAttachmentQueryHandler(
            new FakeSupportTicketRepository(ticket), new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(
            new GetSupportTicketAttachmentQuery { TicketId = ticket.Id }, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(ticket.AttachmentKey, result!.AttachmentKey);
        Assert.Equal(ticket.AttachmentFileName, result.AttachmentFileName);
        Assert.Equal(ticket.AttachmentContentType, result.AttachmentContentType);
    }
}
