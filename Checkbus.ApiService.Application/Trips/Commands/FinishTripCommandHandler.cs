using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // CU-08: flips an EnProceso trip to Completado. Admin-initiated (Planificador/Administrador),
    // same tenant-check/role-gate posture as StartTripCommandHandler. Also closes boarding (any
    // still-Pendiente BoardingRecord becomes Ausente) as part of finishing, instead of requiring
    // the driver to separately call CloseBoardingCommand.
    public class FinishTripCommandHandler : IRequestHandler<FinishTripCommand>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IBoardingRecordRepository _boardingRecordRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IPassengerRepository _passengerRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IEmailService _emailService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<FinishTripCommandHandler> _logger;

        public FinishTripCommandHandler(
            ITripRepository tripRepository,
            IBoardingRecordRepository boardingRecordRepository,
            IReservationRepository reservationRepository,
            IPassengerRepository passengerRepository,
            ISeatRepository seatRepository,
            IEmailService emailService,
            ICurrentUserService currentUser,
            ILogger<FinishTripCommandHandler> logger)
        {
            _tripRepository = tripRepository;
            _boardingRecordRepository = boardingRecordRepository;
            _reservationRepository = reservationRepository;
            _passengerRepository = passengerRepository;
            _seatRepository = seatRepository;
            _emailService = emailService;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task Handle(FinishTripCommand request, CancellationToken cancellationToken)
        {
            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var trip = await _tripRepository.GetByIdAsync(request.Id, cancellationToken);
            if (trip is null || trip.OrganizationId != callerOrganizationId)
            {
                // IDOR-safe: cross-tenant existence is never leaked to the caller.
                throw new TripNotFoundException();
            }

            if (trip.Status != TripStatus.EnProceso)
            {
                throw new TripNotEligibleException(
                    "El viaje no puede finalizarse: solo un viaje en estado En proceso puede finalizarse.");
            }

            // Pendiente -> Ausente for every BoardingRecord on this trip, same mechanism
            // CloseBoardingCommandHandler uses — folded into finishing instead of requiring the
            // driver to call it as a separate step.
            await _boardingRecordRepository.CloseBoardingAsync(trip.Id, cancellationToken);

            trip.Status = TripStatus.Completado;
            trip.ActualArrivalDate = request.ActualArrivalDate;
            trip.Observations = request.Observations;
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
                    var message = BuildFinishedEmail(request.ActualArrivalDate, passenger.Name, passenger.Email, seatNumber);
                    await _emailService.SendAsync(message, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to send trip-finished email for ReservationId {ReservationId}",
                        reservation.Id);
                }
            }
        }

        private static EmailMessage BuildFinishedEmail(
            DateTime actualArrivalDate, string passengerName, string passengerEmail, int seatNumber)
        {
            const string subject = "Tu viaje finalizó";
            var body = $"""
                <h2>Viaje finalizado</h2>
                <p>Hola {passengerName},</p>
                <p>Tu viaje llegó a destino. ¡Gracias por viajar con nosotros!</p>
                <ul>
                    <li>Hora de llegada: {actualArrivalDate:dd/MM/yyyy HH:mm}</li>
                    <li>Asiento: {seatNumber}</li>
                </ul>
                """;

            return new EmailMessage(passengerEmail, subject, body);
        }
    }
}
