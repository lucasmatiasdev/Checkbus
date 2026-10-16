using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Queries;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    public class CloseBoardingCommandHandler : IRequestHandler<CloseBoardingCommand>
    {
        private readonly ITripRepository _tripRepository;
        private readonly IBoardingRecordRepository _boardingRecordRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IPassengerRepository _passengerRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IEmailService _emailService;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<CloseBoardingCommandHandler> _logger;

        public CloseBoardingCommandHandler(
            ITripRepository tripRepository,
            IBoardingRecordRepository boardingRecordRepository,
            IReservationRepository reservationRepository,
            IPassengerRepository passengerRepository,
            IEventRepository eventRepository,
            IEmailService emailService,
            ICurrentUserService currentUser,
            ILogger<CloseBoardingCommandHandler> logger)
        {
            _tripRepository = tripRepository;
            _boardingRecordRepository = boardingRecordRepository;
            _reservationRepository = reservationRepository;
            _passengerRepository = passengerRepository;
            _eventRepository = eventRepository;
            _emailService = emailService;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task Handle(CloseBoardingCommand request, CancellationToken cancellationToken)
        {
            var trip = await GetTripBoardingListQueryHandler.AuthorizeDriverOwnsTripAsync(
                request.TripId, _tripRepository, _currentUser, cancellationToken);

            await _boardingRecordRepository.CloseBoardingAsync(trip.Id, cancellationToken);

            await NotifyAbsentPassengersAsync(trip.Id, trip.EventId, cancellationToken);
        }

        // CU-12: notify every passenger left Ausente by the close — not everyone on the trip,
        // since a passenger who already boarded (Abordado) needs no email. Same defensive
        // posture as every other email-send in this codebase (PaymentApprovedEventHandler):
        // one passenger's bad/missing email must not block another's, and this must never
        // throw — CloseBoardingAsync already succeeded by this point.
        private async Task NotifyAbsentPassengersAsync(Guid tripId, Guid eventId, CancellationToken cancellationToken)
        {
            var boardingRecords = await _boardingRecordRepository.GetByTripIdAsync(tripId, cancellationToken);
            var absentReservationIds = boardingRecords
                .Where(record => record.Status == BoardingStatus.Ausente)
                .Select(record => record.ReservationId)
                .ToHashSet();

            if (absentReservationIds.Count == 0)
            {
                return;
            }

            var reservations = await _reservationRepository.GetByTripIdAsync(tripId, cancellationToken);
            var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);

            foreach (var reservation in reservations.Where(r => absentReservationIds.Contains(r.Id)))
            {
                try
                {
                    var passenger = await _passengerRepository.GetByIdAsync(reservation.PassengerId, cancellationToken);
                    if (passenger is null || string.IsNullOrWhiteSpace(passenger.Email))
                    {
                        continue;
                    }

                    await _emailService.SendAsync(BuildAbsentEmail(passenger, @event?.Name), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to send boarding-closed notification for ReservationId {ReservationId}",
                        reservation.Id);
                }
            }
        }

        private static EmailMessage BuildAbsentEmail(Passenger passenger, string? eventName)
        {
            var subject = $"No pudiste abordar tu viaje a {eventName ?? "tu destino"}";
            var body = $"""
                <h2>Embarque cerrado</h2>
                <p>Hola {passenger.Name} {passenger.Surname},</p>
                <p>El embarque de tu viaje a <strong>{eventName ?? "tu destino"}</strong> ya cerró
                y no llegaste a abordar.</p>
                """;

            return new EmailMessage(passenger.Email!, subject, body);
        }
    }
}
