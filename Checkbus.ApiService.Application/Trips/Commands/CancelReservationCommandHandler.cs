using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Reservations;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // Public, unauthenticated cancellation (odd/tasks/reservas-publicas.md Fase 4 / C2). Same
    // IDOR-safe shape as ValidateBoardingCommandHandler (load by id -> not-found if null ->
    // business-rule checks -> delegate the actual mutation to the repository), but with NO
    // ownership/auth check at all — unlike the Chofer-facing boarding surface, this public
    // surface has no authenticated caller to check ownership against; Reservation.Id alone is
    // the access control (same reasoning every other public handler in this feature uses).
    //
    // Two independent rejection reasons, either one alone is enough to block cancellation:
    //   1. The Reservation's BoardingRecord is already Abordado (mirrors A4's "no
    //      re-validating" rejection posture — BoardingRecord itself is NEVER touched here).
    //   2. Fewer than CancellationWindowHours remain before Trip.DepartureDate.
    // Both map to the same ReservationNotCancellableException type (409), with a contextual
    // Spanish message built per call site that is shown verbatim to the passenger.
    public class CancelReservationCommandHandler : IRequestHandler<CancelReservationCommand>
    {
        // Hardcoded, not external config — same reasoning Fase 1 R6 used for the 20/min
        // rate-limit number: no measured traffic/business data yet to justify making this
        // configurable.
        public const int CancellationWindowHours = 24;

        private readonly IReservationRepository _reservationRepository;
        private readonly ITripRepository _tripRepository;
        private readonly IBoardingRecordRepository _boardingRecordRepository;
        private readonly IPassengerRepository _passengerRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IEmailService _emailService;
        private readonly ILogger<CancelReservationCommandHandler> _logger;

        public CancelReservationCommandHandler(
            IReservationRepository reservationRepository,
            ITripRepository tripRepository,
            IBoardingRecordRepository boardingRecordRepository,
            IPassengerRepository passengerRepository,
            ISeatRepository seatRepository,
            IEmailService emailService,
            ILogger<CancelReservationCommandHandler> logger)
        {
            _reservationRepository = reservationRepository;
            _tripRepository = tripRepository;
            _boardingRecordRepository = boardingRecordRepository;
            _passengerRepository = passengerRepository;
            _seatRepository = seatRepository;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task Handle(CancelReservationCommand request, CancellationToken cancellationToken)
        {
            var reservation = await _reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken)
                ?? throw new ReservationNotFoundException();

            // Should not happen given FK integrity, but a Reservation whose Trip row is
            // missing cannot have its cancellation window checked — same "doesn't exist" IDOR
            // framing as every other *NotFoundException in this codebase.
            var trip = await _tripRepository.GetByIdAsync(reservation.TripId, cancellationToken)
                ?? throw new ReservationNotFoundException();

            var boardingRecord = await _boardingRecordRepository.GetByReservationIdAsync(
                reservation.Id, cancellationToken);

            if (boardingRecord?.Status == BoardingStatus.Abordado)
            {
                throw new ReservationNotCancellableException(
                    "Ya abordaste este viaje, no se puede cancelar la reserva.");
            }

            // DepartureDate is always stored as UTC (CreateTripCommandHandler normalizes it
            // via DateTime.SpecifyKind(..., DateTimeKind.Utc) on creation) — safe to compare
            // directly against DateTime.UtcNow.
            if (trip.DepartureDate - DateTime.UtcNow < TimeSpan.FromHours(CancellationWindowHours))
            {
                throw new ReservationNotCancellableException(
                    "Faltan menos de 24 horas para la salida de este viaje, no se puede cancelar la reserva.");
            }

            await _reservationRepository.CancelAsync(reservation.Id, cancellationToken);

            // Sending happens after the cancellation is durably persisted: it is already real
            // at this point, so a flaky SMTP relay must never turn into a failed response for
            // the passenger (same posture M2's CreatePublicReservationCommandHandler uses for
            // the original confirmation email).
            var passenger = await _passengerRepository.GetByIdAsync(reservation.PassengerId, cancellationToken);
            if (passenger is null || string.IsNullOrWhiteSpace(passenger.Email))
            {
                return;
            }

            try
            {
                var seats = await _seatRepository.GetByVehicleIdAsync(trip.VehicleId, cancellationToken);
                var seatNumber = seats.FirstOrDefault(s => s.Id == reservation.SeatId)?.Number;
                var message = BuildCancellationEmail(trip.DepartureDate, passenger.Name, passenger.Email, seatNumber);
                await _emailService.SendAsync(message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to send reservation cancellation email for ReservationId {ReservationId}",
                    reservation.Id);
            }
        }

        private static EmailMessage BuildCancellationEmail(
            DateTime departureDate, string passengerName, string passengerEmail, int? seatNumber)
        {
            const string subject = "Tu reserva fue cancelada";
            var seatLine = seatNumber is null ? string.Empty : $"<li>Asiento: {seatNumber}</li>";
            var body = $"""
                <h2>Reserva cancelada</h2>
                <p>Hola {passengerName},</p>
                <p>Tu reserva fue cancelada correctamente.</p>
                <ul>
                    <li>Fecha de salida: {departureDate:dd/MM/yyyy HH:mm}</li>
                    {seatLine}
                </ul>
                """;

            return new EmailMessage(passengerEmail, subject, body);
        }
    }
}
