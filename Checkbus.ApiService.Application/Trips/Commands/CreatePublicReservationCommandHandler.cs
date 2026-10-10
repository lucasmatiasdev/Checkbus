using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Reservations;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // All-or-nothing batch reservation create (odd/tasks/reservas-publicas.md R4). Same
    // rationale as GetPublicTripSeatMapQueryHandler: NO ICurrentUserService, no org scoping —
    // there is no authenticated caller on this public surface, the only gate is
    // Trip.Status == Publicado.
    //
    // Double-booking is prevented on two layers, deliberately:
    //   1. A fast pre-check against existing Reservations for the requested SeatIds — gives the
    //      common (non-racing) case a clear, cheap rejection without touching the DB's
    //      constraint machinery.
    //   2. The REAL guarantee: a unique index on Reservation(TripId, SeatId)
    //      (CheckbusDbContext.OnModelCreating). If another visitor's submit wins the race
    //      between this handler's pre-check and its SaveChangesAsync, the insert throws
    //      DbUpdateException, caught here and translated into the same domain-level failure.
    //      IReservationRepository.CreateBatchAsync persists the whole Passenger+Reservation
    //      batch in ONE SaveChangesAsync call, so a failed save rolls back everything —
    //      nothing partial, matching this feature's explicit all-or-nothing design.
    public class CreatePublicReservationCommandHandler
        : IRequestHandler<CreatePublicReservationCommand, CreatePublicReservationResultDto>
    {
        private readonly ITripRepository _tripRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IEventRepository _eventRepository;
        private readonly IEmailService _emailService;
        private readonly PublicWebOptions _publicWebOptions;
        private readonly ILogger<CreatePublicReservationCommandHandler> _logger;

        public CreatePublicReservationCommandHandler(
            ITripRepository tripRepository,
            ISeatRepository seatRepository,
            IReservationRepository reservationRepository,
            IEventRepository eventRepository,
            IEmailService emailService,
            PublicWebOptions publicWebOptions,
            ILogger<CreatePublicReservationCommandHandler> logger)
        {
            _tripRepository = tripRepository;
            _seatRepository = seatRepository;
            _reservationRepository = reservationRepository;
            _eventRepository = eventRepository;
            _emailService = emailService;
            _publicWebOptions = publicWebOptions;
            _logger = logger;
        }

        public async Task<CreatePublicReservationResultDto> Handle(
            CreatePublicReservationCommand request, CancellationToken cancellationToken)
        {
            var trip = await _tripRepository.GetByIdAsync(request.TripId, cancellationToken);
            if (trip is null || trip.Status != TripStatus.Publicado)
            {
                // Same reasoning as GetPublicTripSeatMapQueryHandler: IDOR-safe, "doesn't exist"
                // and "exists but not published" are indistinguishable to the caller.
                throw new TripNotFoundException();
            }

            // The seat map page must have been viewed first for Seat rows to exist (lazily
            // backfilled by ISeatRepository.EnsureSeatsForVehicleAsync in R3) — no need to
            // re-ensure them here, only look them up so every requested SeatId can be
            // validated against THIS trip's vehicle.
            var seats = await _seatRepository.GetByVehicleIdAsync(trip.VehicleId, cancellationToken);
            var seatsById = seats.ToDictionary(s => s.Id);

            // A malicious client could submit a SeatId belonging to an entirely different
            // vehicle/trip — reject the whole batch rather than silently accepting it.
            foreach (var entry in request.Passengers)
            {
                if (!seatsById.ContainsKey(entry.SeatId))
                {
                    throw new SeatsNoLongerAvailableException(
                        "One or more selected seats do not belong to this trip.");
                }
            }

            // Fast pre-check (not the real guarantee, see class doc comment): reject early if
            // any requested seat already has a Reservation for this Trip.
            var existingReservations = await _reservationRepository.GetByTripIdAsync(trip.Id, cancellationToken);
            var alreadyReservedSeatIds = existingReservations.Select(r => r.SeatId).ToHashSet();
            if (request.Passengers.Any(entry => alreadyReservedSeatIds.Contains(entry.SeatId)))
            {
                throw new SeatsNoLongerAvailableException();
            }

            var passengers = new List<Passenger>(request.Passengers.Count);
            var reservations = new List<Reservation>(request.Passengers.Count);
            var created = new List<CreatedReservationDto>(request.Passengers.Count);

            foreach (var entry in request.Passengers)
            {
                // Always a brand-new row, never looked up/deduped by DocumentNumber — explicit
                // design decision from R1 (Passenger is not a reusable catalog).
                var passenger = new Passenger
                {
                    Id = Guid.NewGuid(),
                    Name = entry.Name,
                    Surname = entry.Surname,
                    DocumentType = entry.DocumentType,
                    DocumentNumber = entry.DocumentNumber,
                    Email = entry.Email,
                    Phone = entry.Phone
                };

                var reservation = new Reservation
                {
                    Id = Guid.NewGuid(),
                    TripId = trip.Id,
                    SeatId = entry.SeatId,
                    PassengerId = passenger.Id,
                    Price = trip.Price,
                    Status = ReservationStatus.Confirmada
                };

                passengers.Add(passenger);
                reservations.Add(reservation);
                created.Add(new CreatedReservationDto
                {
                    ReservationId = reservation.Id,
                    SeatNumber = seatsById[entry.SeatId].Number,
                    PassengerName = passenger.Name,
                    PassengerSurname = passenger.Surname,
                    Price = trip.Price
                });
            }

            try
            {
                await _reservationRepository.CreateBatchAsync(passengers, reservations, cancellationToken);
            }
            catch (DbUpdateException)
            {
                // The race path: another visitor's submit took one of these seats between our
                // pre-check above and this SaveChangesAsync, caught here as a unique-constraint
                // violation on Reservation(TripId, SeatId). The failed SaveChangesAsync already
                // guarantees nothing from this batch was persisted — no partial cleanup needed.
                throw new SeatsNoLongerAvailableException();
            }

            // Event.Name is the only piece of ticket content not already in memory — one extra
            // lookup, not persisted, scoped to the email body only.
            var @event = await _eventRepository.GetByIdAsync(trip.EventId, cancellationToken);

            // Sending happens after the batch is durably persisted: the reservation is already
            // real at this point, so a flaky SMTP relay must never turn into a failed response
            // for the passenger (same posture as EventNew.razor's "created but image upload
            // failed" warning). One email per Passenger/Reservation row — a family of 3 may
            // enter 3 different addresses on their own mini-form, and each gets their own ticket.
            for (var i = 0; i < passengers.Count; i++)
            {
                var passenger = passengers[i];
                var reservation = reservations[i];

                if (string.IsNullOrWhiteSpace(passenger.Email))
                {
                    continue;
                }

                try
                {
                    var message = BuildTicketEmail(@event?.Name, trip, passenger, reservation, created[i].SeatNumber);
                    await _emailService.SendAsync(message, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to send reservation confirmation email for ReservationId {ReservationId}",
                        reservation.Id);
                }
            }

            return new CreatePublicReservationResultDto { Reservations = created };
        }

        private EmailMessage BuildTicketEmail(
            string? eventName, Trip trip, Passenger passenger, Reservation reservation, int seatNumber)
        {
            var confirmationLink = $"{_publicWebOptions.BaseUrl}/reservas/confirmacion/{reservation.Id}";
            var subject = $"Tu reserva para {eventName ?? "tu viaje"} está confirmada";
            var body = $"""
                <h2>¡Reserva confirmada!</h2>
                <p>Hola {passenger.Name} {passenger.Surname},</p>
                <p>Tu lugar para <strong>{eventName ?? "el evento"}</strong> quedó confirmado.</p>
                <ul>
                    <li>Fecha de salida: {trip.DepartureDate:dd/MM/yyyy HH:mm}</li>
                    <li>Asiento: {seatNumber}</li>
                    <li>Precio: {reservation.Price:C}</li>
                </ul>
                <p>Podés ver el detalle de tu reserva en el siguiente enlace:</p>
                <p><a href="{confirmationLink}">{confirmationLink}</a></p>
                """;

            return new EmailMessage(passenger.Email, subject, body);
        }
    }
}
