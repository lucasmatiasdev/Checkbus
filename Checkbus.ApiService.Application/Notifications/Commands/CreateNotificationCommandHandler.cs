using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Notifications;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Notifications.Commands
{
    // CU-04: standalone, manually-triggered notification send. Resolves the audience by scope
    // (Trip: just that Trip; Event: every Trip tied to it), persists the Notification row FIRST
    // (it is already real at that point — same "durable truth first, then best-effort notify"
    // posture as every other handler in this module), then emails every Confirmada passenger
    // with the same defensive per-recipient catch-and-log loop as StartTripCommandHandler.
    public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IPassengerRepository _passengerRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly IEmailService _emailService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<CreateNotificationCommandHandler> _logger;

        public CreateNotificationCommandHandler(
            ITripRepository tripRepository,
            IEventRepository eventRepository,
            IReservationRepository reservationRepository,
            IPassengerRepository passengerRepository,
            INotificationRepository notificationRepository,
            IEmailService emailService,
            ICurrentUserService currentUser,
            ILogger<CreateNotificationCommandHandler> logger)
        {
            _tripRepository = tripRepository;
            _eventRepository = eventRepository;
            _reservationRepository = reservationRepository;
            _passengerRepository = passengerRepository;
            _notificationRepository = notificationRepository;
            _emailService = emailService;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
        {
            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");
            var callerUserId = _currentUser.UserId
                ?? throw new InvalidOperationException("Authenticated caller has no UserId claim.");

            var affectedTrips = await ResolveAffectedTripsAsync(
                request.TargetType, request.TargetId, callerOrganizationId, cancellationToken);

            // Already real before anything is sent — mirrors CancelTripCommandHandler's
            // "flip status first, refund/notify best-effort after" ordering.
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                OrganizationId = callerOrganizationId,
                TargetType = request.TargetType,
                TargetId = request.TargetId,
                Reason = request.Reason,
                Message = request.Message,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = callerUserId
            };
            await _notificationRepository.AddAsync(notification, cancellationToken);

            foreach (var trip in affectedTrips)
            {
                var reservations = await _reservationRepository.GetByTripIdAsync(trip.Id, cancellationToken);

                // Only Confirmada reservations: PendientePago is an ephemeral hold, never a real
                // seat to notify about (see odd/tasks/notificaciones-viaje.md "Decisiones del
                // usuario").
                foreach (var reservation in reservations.Where(r => r.Status == ReservationStatus.Confirmada))
                {
                    try
                    {
                        var passenger = await _passengerRepository.GetByIdAsync(reservation.PassengerId, cancellationToken);
                        if (passenger is null || string.IsNullOrWhiteSpace(passenger.Email))
                        {
                            continue;
                        }

                        var message = BuildNotificationEmail(
                            passenger.Name, passenger.Email, request.Reason, request.Message);
                        await _emailService.SendAsync(message, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to send notification email for ReservationId {ReservationId} (NotificationId {NotificationId})",
                            reservation.Id, notification.Id);
                    }
                }
            }
        }

        private async Task<IReadOnlyList<Trip>> ResolveAffectedTripsAsync(
            NotificationTargetType targetType, Guid targetId, Guid callerOrganizationId, CancellationToken cancellationToken)
        {
            if (targetType == NotificationTargetType.Trip)
            {
                var trip = await _tripRepository.GetByIdAsync(targetId, cancellationToken);
                if (trip is null || trip.OrganizationId != callerOrganizationId)
                {
                    // IDOR-safe: cross-tenant existence is never leaked to the caller.
                    throw new TripNotFoundException();
                }

                return [trip];
            }

            var @event = await _eventRepository.GetByIdAsync(targetId, cancellationToken);
            if (@event is null || @event.OrganizationId != callerOrganizationId)
            {
                // IDOR-safe: cross-tenant existence is never leaked to the caller.
                throw new EventNotFoundException();
            }

            return await _tripRepository.GetByEventIdAsync(@event.Id, cancellationToken);
        }

        private static EmailMessage BuildNotificationEmail(
            string passengerName, string passengerEmail, NotificationReason reason, string freeTextMessage)
        {
            var subject = $"Notificación sobre tu viaje: {GetReasonLabel(reason)}";
            var body = $"""
                <h2>Novedad sobre tu viaje</h2>
                <p>Hola {passengerName},</p>
                <p><strong>Motivo:</strong> {GetReasonLabel(reason)}</p>
                <p>{freeTextMessage}</p>
                """;

            return new EmailMessage(passengerEmail, subject, body);
        }

        // Human-readable Spanish label, same style as GetPublicReservationsConfirmationQueryHandler
        // .MapTripStatus — the raw enum name is not something a passenger-facing email should show.
        private static string GetReasonLabel(NotificationReason reason) => reason switch
        {
            NotificationReason.CambioHorario => "Cambio de horario",
            NotificationReason.CambioVehiculo => "Cambio de vehículo",
            NotificationReason.Otro => "Aviso general",
            _ => reason.ToString()
        };
    }
}
