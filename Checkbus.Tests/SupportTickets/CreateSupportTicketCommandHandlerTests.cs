using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.Application.SupportTickets.Commands;
using Checkbus.ApiService.Domain.Entities.Support;
using Checkbus.ApiService.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace Checkbus.Tests.SupportTickets;

public class CreateSupportTicketCommandHandlerTests
{
    private sealed class FakeSupportTicketRepository : ISupportTicketRepository
    {
        public List<SupportTicket> AddedTickets { get; } = [];

        public Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken)
        {
            AddedTickets.Add(ticket);
            return Task.CompletedTask;
        }

        public Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateSupportTicketCommandHandler.");

        public Task<IReadOnlyList<SupportTicket>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateSupportTicketCommandHandler.");
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public List<string> SavedKeys { get; } = [];

        public Task<string> SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
        {
            SavedKeys.Add(key);
            return Task.FromResult(key);
        }

        public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateSupportTicketCommandHandler.");

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateSupportTicketCommandHandler.");

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateSupportTicketCommandHandler.");
    }

    // Same shape as CancelReservationCommandHandlerTests' FakeEmailService: an optional
    // throwOnSend switch proves the handler swallows a flaky SMTP relay without failing the
    // (already persisted) ticket creation.
    private sealed class FakeEmailService(bool throwOnSend = false) : IEmailService
    {
        public List<EmailMessage> SentMessages { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            if (throwOnSend)
            {
                throw new InvalidOperationException("Simulated SMTP failure.");
            }

            SentMessages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCurrentUserService(Guid? organizationId, Guid? userId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
        public Guid? OrganizationId => organizationId;
        public string? Role => "Planificador";
        public string? Email => "planificador@checkbus-demo.com";
    }

    private static CreateSupportTicketCommand CreateCommand(
        Stream? attachmentStream = null, string? attachmentFileName = null, string? attachmentContentType = null) => new()
    {
        Category = SupportTicketCategory.ProblemaTecnico,
        Subject = "No puedo ver mis reservas",
        Description = "El listado de reservas no carga desde ayer.",
        Priority = SupportTicketPriority.Alta,
        AttachmentStream = attachmentStream,
        AttachmentFileName = attachmentFileName,
        AttachmentContentType = attachmentContentType
    };

    private static CreateSupportTicketCommandHandler CreateHandler(
        FakeSupportTicketRepository ticketRepository,
        FakeFileStorageService fileStorage,
        FakeEmailService emailService,
        SupportOptions supportOptions,
        Guid? organizationId,
        Guid? userId)
    {
        return new CreateSupportTicketCommandHandler(
            ticketRepository,
            fileStorage,
            emailService,
            supportOptions,
            new FakeCurrentUserService(organizationId, userId),
            NullLogger<CreateSupportTicketCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_PersistsTicket_WithAbiertoStatusAndCallerIdentity()
    {
        var organizationId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ticketRepository = new FakeSupportTicketRepository();
        var handler = CreateHandler(
            ticketRepository, new FakeFileStorageService(), new FakeEmailService(), new SupportOptions(), organizationId, userId);

        var result = await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        var persisted = Assert.Single(ticketRepository.AddedTickets);
        Assert.Equal(SupportTicketStatus.Abierto, persisted.Status);
        Assert.Equal(organizationId, persisted.OrganizationId);
        Assert.Equal(userId, persisted.CreatedByUserId);
        Assert.Equal(persisted.Id, result.Id);
    }

    [Fact]
    public async Task Handle_WithAttachment_SavesFileAndSetsAttachmentFields()
    {
        var fileStorage = new FakeFileStorageService();
        var ticketRepository = new FakeSupportTicketRepository();
        var handler = CreateHandler(
            ticketRepository, fileStorage, new FakeEmailService(), new SupportOptions(), Guid.NewGuid(), Guid.NewGuid());
        using var attachment = new MemoryStream([1, 2, 3]);

        await handler.Handle(
            CreateCommand(attachment, "captura.png", "image/png"), TestContext.Current.CancellationToken);

        Assert.Single(fileStorage.SavedKeys);
        var persisted = Assert.Single(ticketRepository.AddedTickets);
        Assert.NotNull(persisted.AttachmentKey);
        Assert.Equal("captura.png", persisted.AttachmentFileName);
        Assert.Equal("image/png", persisted.AttachmentContentType);
    }

    [Fact]
    public async Task Handle_WithoutAttachment_DoesNotCallFileStorageAndAttachmentKeyStaysNull()
    {
        var fileStorage = new FakeFileStorageService();
        var ticketRepository = new FakeSupportTicketRepository();
        var handler = CreateHandler(
            ticketRepository, fileStorage, new FakeEmailService(), new SupportOptions(), Guid.NewGuid(), Guid.NewGuid());

        await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.Empty(fileStorage.SavedKeys);
        var persisted = Assert.Single(ticketRepository.AddedTickets);
        Assert.Null(persisted.AttachmentKey);
    }

    [Fact]
    public async Task Handle_WithNotificationEmailConfigured_SendsEmailOnce()
    {
        var emailService = new FakeEmailService();
        var handler = CreateHandler(
            new FakeSupportTicketRepository(), new FakeFileStorageService(), emailService,
            new SupportOptions { NotificationEmail = "soporte@checkbus.com" }, Guid.NewGuid(), Guid.NewGuid());

        await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.Single(emailService.SentMessages);
        Assert.Equal("soporte@checkbus.com", emailService.SentMessages[0].To);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithBlankNotificationEmail_DoesNotSendEmail(string notificationEmail)
    {
        var emailService = new FakeEmailService();
        var handler = CreateHandler(
            new FakeSupportTicketRepository(), new FakeFileStorageService(), emailService,
            new SupportOptions { NotificationEmail = notificationEmail }, Guid.NewGuid(), Guid.NewGuid());

        await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.Empty(emailService.SentMessages);
    }

    [Fact]
    public async Task Handle_EmailServiceThrows_DoesNotPropagateAndTicketStaysPersisted()
    {
        var ticketRepository = new FakeSupportTicketRepository();
        var emailService = new FakeEmailService(throwOnSend: true);
        var handler = CreateHandler(
            ticketRepository, new FakeFileStorageService(), emailService,
            new SupportOptions { NotificationEmail = "soporte@checkbus.com" }, Guid.NewGuid(), Guid.NewGuid());

        // Must not throw — the email failure is logged and swallowed; the ticket is already
        // persisted before the send attempt and the DTO is returned regardless.
        var result = await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Single(ticketRepository.AddedTickets);
        Assert.Empty(emailService.SentMessages);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = CreateHandler(
            new FakeSupportTicketRepository(), new FakeFileStorageService(), new FakeEmailService(),
            new SupportOptions(), organizationId: null, userId: Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_NullUserIdClaim_ThrowsInvalidOperationException()
    {
        var handler = CreateHandler(
            new FakeSupportTicketRepository(), new FakeFileStorageService(), new FakeEmailService(),
            new SupportOptions(), organizationId: Guid.NewGuid(), userId: null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(), TestContext.Current.CancellationToken));
    }
}
