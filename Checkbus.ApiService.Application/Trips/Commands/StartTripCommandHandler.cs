using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // CU-08: flips a Publicado trip to EnProceso. Admin-initiated (Planificador/Administrador),
    // same tenant-check/role-gate posture as PublishTripCommandHandler/CancelTripCommandHandler.
    public class StartTripCommandHandler : IRequestHandler<StartTripCommand>
    {
        // ±30 minutes around the scheduled DepartureDate — neither too early (passengers may
        // still be arriving) nor so late the status would be stale/misleading.
        public static readonly TimeSpan StartWindow = TimeSpan.FromMinutes(30);

        private readonly ITripRepository _tripRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IPassengerRepository _passengerRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IEmailService _emailService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<StartTripCommandHandler> _logger;

        public StartTripCommandHandler(
            ITripRepository tripRepository,
            IReservationRepository reservationRepository,
            IPassengerRepository passengerRepository,
            ISeatRepository seatRepository,
            IEmailService emailService,
            ICurrentUserService currentUser,
            ILogger<StartTripCommandHandler> logger)
        {
            _tripRepository = tripRepository;
            _reservationRepository = reservationRepository;
            _passengerRepository = passengerRepository;
            _seatRepository = seatRepository;
            _emailService = emailService;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task Handle(StartTripCommand request, CancellationToken cancellationToken)
        {
            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var trip = await _tripRepository.GetByIdAsync(request.Id, cancellationToken);
            if (trip is null || trip.OrganizationId != callerOrganizationId)
            {
                // IDOR-safe: cross-tenant existence is never leaked to the caller.
                throw new TripNotFoundException();
            }

            if (trip.Status != TripStatus.Publicado)
            {
                throw new TripNotEligibleException(
                    "El viaje no puede iniciarse: solo un viaje en estado Publicado puede iniciarse.");
            }

            var now = DateTime.UtcNow;
            if (now < trip.DepartureDate - StartWindow || now > trip.DepartureDate + StartWindow)
            {
                throw new TripNotEligibleException(
                    $"El viaje solo puede iniciarse dentro de los {StartWindow.TotalMinutes:0} minutos " +
                    "antes o después de la hora de salida programada.");
            }

            trip.Status = TripStatus.EnProceso;
            trip.UpdatedAt = DateTime.UtcNow;
            await _tripRepository.UpdateAsync(trip, cancellationToken);

            var reservations = await _reservationRepository.GetByTripIdAsync(trip.Id, cancellationToken);
            var seats = await _seatRepository.GetByVehicleIdAsync(trip.VehicleId, cancellationToken);
            var seatNumbersBySeatId = seats.ToDictionary(s => s.Id, s => s.Number);

            foreach (var reservation in reservations)
            {
                try
                {
                    var passenger = await _passengerRepository.GetByIdAsync(reservation.PassengerId, cancellationToken);
                    if (passenger is null || string.IsNullOrWhiteSpace(passenger.Email))
                    {
                        continue;
                    }

                    seatNumbersBySeatId.TryGetValue(reservation.SeatId, out var seatNumber);
                    var message = BuildStartedEmail(trip.DepartureDate, passenger.Name, passenger.Email, seatNumber);
                    await _emailService.SendAsync(message, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to send trip-started email for ReservationId {ReservationId}",
                        reservation.Id);
                }
            }
        }

        private static EmailMessage BuildStartedEmail(
            DateTime departureDate, string passengerName, string passengerEmail, int seatNumber)
        {
            const string subject = "Tu viaje ya está en marcha";
            var body = $"""
                <h2>Tu viaje comenzó</h2>
                <p>Hola {passengerName},</p>
                <p>Tu viaje ya está en marcha.</p>
                <ul>
                    <li>Salida programada: {departureDate:dd/MM/yyyy HH:mm}</li>
                    <li>Asiento: {seatNumber}</li>
                </ul>
                """;

            return new EmailMessage(passengerEmail, subject, body);
        }
    }
}
