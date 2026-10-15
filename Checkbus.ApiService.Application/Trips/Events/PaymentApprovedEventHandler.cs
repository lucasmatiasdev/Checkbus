using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Trips;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Trips.Events
{
    // Fase D5 (odd/tasks/pagos-mercadopago.md): sends the "reserva confirmada" ticket email,
    // now triggered from the webhook flow instead of inline in
    // CreatePublicReservationCommandHandler (which Fase C removed it from — sending it before
    // payment was confirmed would have been factually wrong). Content/style reused from that
    // handler's original BuildTicketEmail (git show a8cc6b3), adapted to re-resolve its own data
    // from just the event's ids instead of already having everything in local variables.
    //
    // Same defensive posture as every other email-send in this codebase
    // (CancelReservationCommandHandler.Handle): a flaky SMTP relay must never fail webhook
    // processing, so every reservation's send is wrapped in its own try/catch — one passenger's
    // bad email address must not block another passenger's email in the same batch, and this
    // Handle method must never throw (it runs inside HandlePaymentWebhookCommandHandler's
    // MediatR.Publish call).
    public class PaymentApprovedEventHandler : INotificationHandler<PaymentApprovedEvent>
    {
        private readonly IReservationRepository _reservationRepository;
        private readonly ITripRepository _tripRepository;
        private readonly IPassengerRepository _passengerRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IEmailService _emailService;
        private readonly PublicWebOptions _publicWebOptions;
        private readonly ILogger<PaymentApprovedEventHandler> _logger;

        public PaymentApprovedEventHandler(
            IReservationRepository reservationRepository,
            ITripRepository tripRepository,
            IPassengerRepository passengerRepository,
            ISeatRepository seatRepository,
            IEventRepository eventRepository,
            IEmailService emailService,
            PublicWebOptions publicWebOptions,
            ILogger<PaymentApprovedEventHandler> logger)
        {
            _reservationRepository = reservationRepository;
            _tripRepository = tripRepository;
            _passengerRepository = passengerRepository;
            _seatRepository = seatRepository;
            _eventRepository = eventRepository;
            _emailService = emailService;
            _publicWebOptions = publicWebOptions;
            _logger = logger;
        }

        public async Task Handle(PaymentApprovedEvent notification, CancellationToken cancellationToken)
        {
            foreach (var reservationId in notification.ReservationIds)
            {
                try
                {
                    await SendConfirmationEmailAsync(reservationId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to send payment-approved confirmation email for ReservationId {ReservationId}",
                        reservationId);
                }
            }
        }

        private async Task SendConfirmationEmailAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            var reservation = await _reservationRepository.GetByIdAsync(reservationId, cancellationToken);
            if (reservation is null)
            {
                return;
            }

            var passenger = await _passengerRepository.GetByIdAsync(reservation.PassengerId, cancellationToken);
            if (passenger is null || string.IsNullOrWhiteSpace(passenger.Email))
            {
                return;
            }

            var trip = await _tripRepository.GetByIdAsync(reservation.TripId, cancellationToken);
            if (trip is null)
            {
                return;
            }

            var seats = await _seatRepository.GetByVehicleIdAsync(trip.VehicleId, cancellationToken);
            var seatNumber = seats.FirstOrDefault(s => s.Id == reservation.SeatId)?.Number;

            var @event = await _eventRepository.GetByIdAsync(trip.EventId, cancellationToken);

            var message = BuildConfirmationEmail(@event?.Name, trip, passenger, reservation, seatNumber);
            await _emailService.SendAsync(message, cancellationToken);
        }

        private EmailMessage BuildConfirmationEmail(
            string? eventName, Trip trip, Passenger passenger, Reservation reservation, int? seatNumber)
        {
            var confirmationLink = $"{_publicWebOptions.BaseUrl}/reservas/confirmacion/{reservation.Id}";
            var subject = $"Tu reserva para {eventName ?? "tu viaje"} está confirmada";
            var seatLine = seatNumber is null ? string.Empty : $"<li>Asiento: {seatNumber}</li>";
            var body = $"""
                <h2>¡Reserva confirmada!</h2>
                <p>Hola {passenger.Name} {passenger.Surname},</p>
                <p>Tu lugar para <strong>{eventName ?? "el evento"}</strong> quedó confirmado.</p>
                <ul>
                    <li>Fecha de salida: {trip.DepartureDate:dd/MM/yyyy HH:mm}</li>
                    {seatLine}
                    <li>Precio: {reservation.Price:C}</li>
                </ul>
                <p>Podés ver el detalle de tu reserva en el siguiente enlace:</p>
                <p><a href="{confirmationLink}">{confirmationLink}</a></p>
                """;

            return new EmailMessage(passenger.Email, subject, body);
        }
    }
}
