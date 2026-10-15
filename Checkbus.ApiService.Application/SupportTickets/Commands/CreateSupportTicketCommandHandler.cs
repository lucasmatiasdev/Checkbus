using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.Application.SupportTickets.Queries;
using Checkbus.ApiService.Domain.Entities.Support;
using Checkbus.ApiService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.SupportTickets.Commands
{
    public class CreateSupportTicketCommandHandler : IRequestHandler<CreateSupportTicketCommand, SupportTicketDto>
    {
        private readonly ISupportTicketRepository _ticketRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IEmailService _emailService;
        private readonly SupportOptions _supportOptions;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<CreateSupportTicketCommandHandler> _logger;

        public CreateSupportTicketCommandHandler(
            ISupportTicketRepository ticketRepository,
            IFileStorageService fileStorageService,
            IEmailService emailService,
            SupportOptions supportOptions,
            ICurrentUserService currentUser,
            ILogger<CreateSupportTicketCommandHandler> logger)
        {
            _ticketRepository = ticketRepository;
            _fileStorageService = fileStorageService;
            _emailService = emailService;
            _supportOptions = supportOptions;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<SupportTicketDto> Handle(CreateSupportTicketCommand request, CancellationToken cancellationToken)
        {
            // Tenant and author come exclusively from the validated claims - the command has
            // no OrganizationId/UserId field, so no request value can ever influence them.
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var userId = _currentUser.UserId
                ?? throw new InvalidOperationException("Authenticated caller has no UserId claim.");

            var ticket = new SupportTicket
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                CreatedByUserId = userId,
                Category = request.Category,
                Subject = request.Subject,
                Description = request.Description,
                Status = SupportTicketStatus.Abierto,
                CreatedAt = DateTime.UtcNow
            };

            if (request.AttachmentStream is not null)
            {
                var extension = Path.GetExtension(request.AttachmentFileName);
                var key = $"support-tickets/{ticket.Id}/{Guid.NewGuid()}{extension}";

                await _fileStorageService.SaveAsync(
                    key, request.AttachmentStream!, request.AttachmentContentType!, cancellationToken);

                ticket.AttachmentKey = key;
                ticket.AttachmentFileName = request.AttachmentFileName;
                ticket.AttachmentContentType = request.AttachmentContentType;
            }

            await _ticketRepository.AddAsync(ticket, cancellationToken);

            // Sending happens after the ticket is durably persisted: it is already real at this
            // point, so a flaky SMTP relay must never turn into a failed response for the
            // caller - same posture CancelReservationCommandHandler uses for its cancellation
            // email.
            if (!string.IsNullOrWhiteSpace(_supportOptions.NotificationEmail))
            {
                try
                {
                    var message = BuildNotificationEmail(ticket, organizationId, _supportOptions.NotificationEmail);
                    await _emailService.SendAsync(message, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to send support ticket notification email for SupportTicketId {SupportTicketId}",
                        ticket.Id);
                }
            }

            if (!string.IsNullOrWhiteSpace(_currentUser.Email))
            {
                try
                {
                    var confirmationMessage = BuildConfirmationEmail(ticket, _currentUser.Email);
                    await _emailService.SendAsync(confirmationMessage, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to send support ticket confirmation email for SupportTicketId {SupportTicketId}",
                        ticket.Id);
                }
            }

            return MapToDto(ticket);
        }

        private static EmailMessage BuildConfirmationEmail(SupportTicket ticket, string authorEmail)
        {
            var subject = $"Recibimos tu ticket de soporte: {ticket.Subject}";
            var body = $"""
                <h2>Recibimos tu ticket de soporte</h2>
                <p>Te vamos a contactar a la brevedad. Estos son los datos que registramos:</p>
                <ul>
                    <li>Categoria: {ticket.Category}</li>
                    <li>Asunto: {ticket.Subject}</li>
                    <li>Descripcion: {ticket.Description}</li>
                    <li>TicketId: {FormatDisplayId(ticket.Id)}</li>
                </ul>
                """;

            return new EmailMessage(authorEmail, subject, body);
        }

        private static EmailMessage BuildNotificationEmail(SupportTicket ticket, Guid organizationId, string notificationEmail)
        {
            var subject = $"Nuevo ticket de soporte: {ticket.Subject}";
            var body = $"""
                <h2>Nuevo ticket de soporte</h2>
                <ul>
                    <li>Categoria: {ticket.Category}</li>
                    <li>Asunto: {ticket.Subject}</li>
                    <li>Descripcion: {ticket.Description}</li>
                    <li>OrganizationId: {organizationId}</li>
                    <li>TicketId: {FormatDisplayId(ticket.Id)}</li>
                </ul>
                """;

            return new EmailMessage(notificationEmail, subject, body);
        }

        // Display-only label, never stored or used as a lookup key - truncating to 8 hex chars
        // (32 bits) trades a small, accepted collision risk (~50% past ~65k tickets) for a
        // human-readable reference. The real Guid in ticket.Id remains the sole source of truth.
        private static string FormatDisplayId(Guid id) => $"#TK-{id.ToString("N")[..8].ToUpperInvariant()}";

        private static SupportTicketDto MapToDto(SupportTicket ticket)
        {
            return new SupportTicketDto
            {
                Id = ticket.Id,
                Category = ticket.Category,
                Subject = ticket.Subject,
                Description = ticket.Description,
                Status = ticket.Status,
                AttachmentFileName = ticket.AttachmentFileName,
                CreatedAt = ticket.CreatedAt
            };
        }
    }
}
