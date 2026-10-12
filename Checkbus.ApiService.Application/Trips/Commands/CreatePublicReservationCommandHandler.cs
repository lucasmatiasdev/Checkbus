using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Reservations;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using Checkbus.ApiService.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // All-or-nothing batch reservation create (odd/tasks/reservas-publicas.md R4), reworked in
    // odd/tasks/pagos-mercadopago.md Fase C into a payment-gated seat hold: the batch is now
    // persisted as PendientePago (not Confirmada), covered by one Mercado Pago order (one
    // Payment row), and the caller is redirected to that order's checkout_url instead of
    // getting an immediate "reserva confirmada" response. Same posture as
    // GetPublicTripSeatMapQueryHandler: NO ICurrentUserService, no org scoping — there is no
    // authenticated caller on this public surface, the only gate is Trip.Status == Publicado.
    //
    // Double-booking is prevented on two layers, deliberately:
    //   1. A fast pre-check against existing Reservations for the requested SeatIds — gives the
    //      common (non-racing) case a clear, cheap rejection without touching the DB's
    //      constraint machinery.
    //   2. The REAL guarantee: a unique index on Reservation(TripId, SeatId)
    //      (CheckbusDbContext.OnModelCreating), which already excludes only Cancelada rows —
    //      a PendientePago row blocks the seat for everyone else exactly like Confirmada did,
    //      confirmed in Fase A. If another visitor's submit wins the race between this
    //      handler's pre-check and its SaveChangesAsync, the insert throws DbUpdateException,
    //      caught here and translated into the same domain-level failure.
    //      IReservationRepository.CreateBatchAsync persists the whole Payment+Passenger+
    //      Reservation batch in ONE SaveChangesAsync call, so a failed save rolls back
    //      everything — nothing partial, matching this feature's explicit all-or-nothing
    //      design.
    //
    // Ordering that avoids needing a rollback (Fase C design decision): Payment.Id is
    // generated client-side, and IPaymentService.CreateOrderAsync is called BEFORE anything is
    // persisted. Only once that gateway call succeeds does the handler write the Payment +
    // Passengers + PendientePago Reservations in the single batch above. If CreateOrderAsync
    // throws OrderCreationFailedException, nothing has been persisted yet, so there is nothing
    // to roll back — the exception simply propagates to the controller like any other failure.
    // The inverse (rare) failure — the gateway call succeeds but the subsequent DB write then
    // fails for an unrelated reason — leaves a real Mercado Pago order with no matching local
    // Payment/Reservation state; this is the exact "orphaned payment" case Fase D's webhook
    // handler is designed to auto-refund once a notification eventually arrives for it, so no
    // extra handling is added here for that rare path.
    //
    // Sending the "reserva confirmada" ticket email moved out of this handler entirely: a
    // PendientePago hold has not been paid for yet, so confirming it by email here would be
    // actively wrong. Fase D's PaymentApprovedEvent handler now owns building and sending that
    // email once Mercado Pago's webhook actually confirms payment — this handler no longer
    // depends on IEventRepository/IEmailService at all.
    //
    // C5: PublicWebOptions IS still a dependency again, but only to build Mercado Pago's
    // success/failure/pending back-URLs (config.online in the order-create request) — NOT to
    // send an email. All three outcomes point at the same /reservas/confirmacion/{ids} route
    // ReservationConfirmation.razor already renders sensibly for every ReservationStatus
    // (including PendientePago), same route/format TripReservation.razor:268 already uses on
    // its own synchronous-path redirect. Same defensive posture as every other permissive
    // options class in this codebase (PublicWebOptions.BaseUrl is blank-by-default, never
    // throw-if-missing): a blank BaseUrl means skip the back-URLs entirely rather than send
    // Mercado Pago a broken relative URL.
    public class CreatePublicReservationCommandHandler
        : IRequestHandler<CreatePublicReservationCommand, CreatePublicReservationResultDto>
    {
        // Hardcoded, not external config — same reasoning
        // CancelReservationCommandHandler.CancellationWindowHours already uses (Fase 1 R6's
        // 20/min rate-limit number): no measured traffic/business data yet to justify making
        // this configurable. 15 minutes is the de-facto industry default for "time to
        // complete an external checkout redirect" (odd/tasks/pagos-mercadopago.md Fase E,
        // user-confirmed no stronger preference). Lives here (not a new shared file) for the
        // same reason CancellationWindowHours lives on the handler that most directly owns the
        // policy — GetPublicTripSeatMapQueryHandler/GetTripSeatMapQueryHandler reference this
        // same constant by name rather than duplicating the number.
        public const int HoldExpirationMinutes = 15;

        private readonly ITripRepository _tripRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IPaymentService _paymentService;
        private readonly PublicWebOptions _publicWebOptions;
        private readonly ILogger<CreatePublicReservationCommandHandler> _logger;

        public CreatePublicReservationCommandHandler(
            ITripRepository tripRepository,
            ISeatRepository seatRepository,
            IReservationRepository reservationRepository,
            IPaymentService paymentService,
            PublicWebOptions publicWebOptions,
            ILogger<CreatePublicReservationCommandHandler> logger)
        {
            _tripRepository = tripRepository;
            _seatRepository = seatRepository;
            _reservationRepository = reservationRepository;
            _paymentService = paymentService;
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

            // Fase E: lazily release any PendientePago hold on this Trip that is older than
            // HoldExpirationMinutes, before the pre-check below reads current reservations —
            // same "clean up state the next time something reads it" convention as
            // ISeatRepository.EnsureSeatsForVehicleAsync, applied so an abandoned checkout
            // doesn't block a brand-new booking attempt on the same seat.
            await _reservationRepository.ReleaseExpiredHoldsAsync(trip.Id, HoldExpirationMinutes, cancellationToken);

            // Fast pre-check (not the real guarantee, see class doc comment): reject early if
            // any requested seat already has a Reservation for this Trip.
            var existingReservations = await _reservationRepository.GetByTripIdAsync(trip.Id, cancellationToken);
            var alreadyReservedSeatIds = existingReservations.Select(r => r.SeatId).ToHashSet();
            if (request.Passengers.Any(entry => alreadyReservedSeatIds.Contains(entry.SeatId)))
            {
                throw new SeatsNoLongerAvailableException();
            }

            // Payment.Id is generated client-side, BEFORE any gateway or DB call, so it can be
            // handed to Mercado Pago as external_reference up front (Fase C design decision —
            // see class doc comment) and stamped onto each Reservation below (Fase D7). Fase D's
            // webhook resolves a notification back to this Payment via that same value.
            var paymentId = Guid.NewGuid();

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
                    // Fase C: a seat hold, not an immediate confirmation — flips to Confirmada
                    // only once Fase D's webhook sees the Mercado Pago order approved.
                    Status = ReservationStatus.PendientePago,
                    // Fase D7: links this Reservation back to the Payment covering it.
                    PaymentId = paymentId
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

            var totalAmount = new Money(reservations.Sum(r => r.Price));

            // C5: all three outcomes (success/failure/pending) redirect the buyer back to the
            // same confirmation page, keyed by this batch's reservation ids — same
            // route/format TripReservation.razor:268 already uses. If BaseUrl is blank
            // (not provisioned for this environment yet), skip the back-URLs entirely rather
            // than build a broken relative URL.
            string? successUrl = null;
            string? failureUrl = null;
            string? pendingUrl = null;
            if (!string.IsNullOrWhiteSpace(_publicWebOptions.BaseUrl))
            {
                var reservationIds = string.Join(",", reservations.Select(r => r.Id));
                var confirmationUrl = $"{_publicWebOptions.BaseUrl}/reservas/confirmacion/{reservationIds}";
                successUrl = confirmationUrl;
                failureUrl = confirmationUrl;
                pendingUrl = confirmationUrl;
            }

            // The whole point of this ordering: nothing is written to our DB until Mercado
            // Pago confirms it opened the order. A failed gateway call
            // (OrderCreationFailedException) propagates untouched — there is nothing to roll
            // back, so it must not be caught/swallowed here.
            var orderResult = await _paymentService.CreateOrderAsync(
                new CreateOrderRequest(
                    totalAmount,
                    paymentId.ToString(),
                    SuccessUrl: successUrl,
                    FailureUrl: failureUrl,
                    PendingUrl: pendingUrl),
                cancellationToken);

            if (orderResult.CheckoutUrl is null)
            {
                // Documented-rare case (odd/tasks/pagos-mercadopago.md Fase B/C): Mercado
                // Pago's own docs say a type: "online" order's response always carries a
                // top-level checkout_url, so a null value here means this particular response
                // diverged from that contract. Log it and still proceed — the order and the
                // hold it is meant to cover are both real either way, and blocking the whole
                // flow over a missing redirect URL would strand a paid-for order with no seat
                // hold to show for it.
                _logger.LogWarning(
                    "Mercado Pago order {ExternalOrderId} for Payment {PaymentId} returned a null CheckoutUrl.",
                    orderResult.OrderId, paymentId);
            }

            var payment = new Payment
            {
                Id = paymentId,
                ExternalOrderId = orderResult.OrderId,
                TotalAmount = totalAmount,
                Status = PaymentStatus.Pendiente,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await _reservationRepository.CreateBatchAsync(payment, passengers, reservations, cancellationToken);
            }
            catch (DbUpdateException)
            {
                // The race path: another visitor's submit took one of these seats between our
                // pre-check above and this SaveChangesAsync, caught here as a unique-constraint
                // violation on Reservation(TripId, SeatId). The failed SaveChangesAsync already
                // guarantees nothing from this batch was persisted locally — the Mercado Pago
                // order itself was already created at this point and is left exactly as the
                // class doc comment describes: an orphaned payment for Fase D's webhook to
                // discover and auto-refund once its notification arrives.
                throw new SeatsNoLongerAvailableException();
            }

            return new CreatePublicReservationResultDto
            {
                Reservations = created,
                CheckoutUrl = orderResult.CheckoutUrl
            };
        }
    }
}
