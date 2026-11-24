using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Audit;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Trips.Commands
{

    public class FinishTripCommandHandler : IRequestHandler<FinishTripCommand>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IBoardingRecordRepository _boardingRecordRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IPassengerRepository _passengerRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IEmailService _emailService;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ILogger<FinishTripCommandHandler> _logger;

        public FinishTripCommandHandler(
            ITripRepository tripRepository,
            IBoardingRecordRepository boardingRecordRepository,
            IReservationRepository reservationRepository,
            IPassengerRepository passengerRepository,
            ISeatRepository seatRepository,
            IEmailService emailService,
            ICurrentUserService currentUser,
            IAuditLogRepository auditLogRepository,
            ILogger<FinishTripCommandHandler> logger)
        {
            _tripRepository = tripRepository;
            _boardingRecordRepository = boardingRecordRepository;
            _reservationRepository = reservationRepository;
            _passengerRepository = passengerRepository;
            _seatRepository = seatRepository;
            _emailService = emailService;
            _currentUser = currentUser;
            _auditLogRepository = auditLogRepository;
            _logger = logger;
        }

        public async Task Handle(FinishTripCommand request, CancellationToken cancellationToken)
        {
            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var trip = await _tripRepository.GetByIdAsync(request.Id, cancellationToken);
            if (trip is null || trip.OrganizationId != callerOrganizationId)
            {

                throw new TripNotFoundException();
            }

            if (_currentUser.Role == nameof(Role.Chofer) && trip.DriverId != _currentUser.UserId)
            {

                throw new TripNotFoundException();
            }

            if (trip.Status != TripStatus.EnProceso)
            {
                throw new TripNotEligibleException(
                    "El viaje no puede finalizarse: solo un viaje en estado En proceso puede finalizarse.");
            }

            await _boardingRecordRepository.CloseBoardingAsync(trip.Id, cancellationToken);

            trip.Status = TripStatus.Completado;
            trip.ActualArrivalDate = request.ActualArrivalDate;
            trip.Observations = request.Observations;
            trip.UpdatedAt = DateTime.UtcNow;
            await _tripRepository.UpdateAsync(trip, cancellationToken);

            try
            {
                await _auditLogRepository.AddAsync(new AuditLogEntry
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = trip.OrganizationId,
                    ActorUserId = _currentUser.UserId,
                    ActorEmail = _currentUser.Email ?? string.Empty,
                    Action = AuditAction.ViajeFinalizado,
                    EntityType = "Trip",
                    EntityId = trip.Id,
                    Description = "Viaje finalizado",
                    CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to write audit log entry for {Action}", AuditAction.ViajeFinalizado);
            }

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
