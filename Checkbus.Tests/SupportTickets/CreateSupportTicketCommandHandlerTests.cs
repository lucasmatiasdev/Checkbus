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

        public Task<IReadOnlyList<SupportTicket>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateSupportTicketCommandHandler.");

        public Task UpdateAsync(SupportTicket ticket, CancellationToken cancellationToken)
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

    private sealed class FakeCurrentUserService(Guid? organizationId, Guid? userId, string? email = "planificador@checkbus-demo.com") : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
        public Guid? OrganizationId => organizationId;
        public string? Role => "Planificador";
        public string? Email => email;
    }

    private static CreateSupportTicketCommand CreateCommand(
        Stream? attachmentStream = null, string? attachmentFileName = null, string? attachmentContentType = null) => new()
    {
        Category = SupportTicketCategory.ProblemaTecnico,
        Subject = "No puedo ver mis reservas",
        Description = "El listado de reservas no carga desde ayer.",
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
        Guid? userId,
        string? authorEmail = "planificador@checkbus-demo.com")
    {
        return new CreateSupportTicketCommandHandler(
            ticketRepository,
            fileStorage,
            emailService,
            supportOptions,
            new FakeCurrentUserService(organizationId, userId, authorEmail),
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
    public async Task Handle_WithNotificationEmailConfigured_SendsNotificationAndConfirmationEmails()
    {
        var emailService = new FakeEmailService();
        var handler = CreateHandler(
            new FakeSupportTicketRepository(), new FakeFileStorageService(), emailService,
            new SupportOptions { NotificationEmail = "soporte@checkbus.com" }, Guid.NewGuid(), Guid.NewGuid());

        await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(2, emailService.SentMessages.Count);
        Assert.Contains(emailService.SentMessages, m => m.To == "soporte@checkbus.com");
        Assert.Contains(emailService.SentMessages, m => m.To == "planificador@checkbus-demo.com");
    }

    [Fact]
    public async Task Handle_WithNotificationEmailConfigured_EmailBodiesShowShortDisplayId()
    {
        var ticketRepository = new FakeSupportTicketRepository();
        var emailService = new FakeEmailService();
        var handler = CreateHandler(
            ticketRepository, new FakeFileStorageService(), emailService,
            new SupportOptions { NotificationEmail = "soporte@checkbus.com" }, Guid.NewGuid(), Guid.NewGuid());

        await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        var persisted = Assert.Single(ticketRepository.AddedTickets);
        var expectedDisplayId = $"#TK-{persisted.Id.ToString("N")[..8].ToUpperInvariant()}";
        Assert.All(emailService.SentMessages, m => Assert.Contains(expectedDisplayId, m.HtmlBody));
        Assert.All(emailService.SentMessages, m => Assert.DoesNotContain(persisted.Id.ToString(), m.HtmlBody));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithBlankNotificationEmail_OnlySendsConfirmationToAuthor(string notificationEmail)
    {
        var emailService = new FakeEmailService();
        var handler = CreateHandler(
            new FakeSupportTicketRepository(), new FakeFileStorageService(), emailService,
            new SupportOptions { NotificationEmail = notificationEmail }, Guid.NewGuid(), Guid.NewGuid());

        await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.Single(emailService.SentMessages);
        Assert.Equal("planificador@checkbus-demo.com", emailService.SentMessages[0].To);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithBlankAuthorEmail_DoesNotSendConfirmationEmail(string? authorEmail)
    {
        var emailService = new FakeEmailService();
        var handler = CreateHandler(
            new FakeSupportTicketRepository(), new FakeFileStorageService(), emailService,
            new SupportOptions(), Guid.NewGuid(), Guid.NewGuid(), authorEmail);

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
