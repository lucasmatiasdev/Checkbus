using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Passengers;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Entities.Trips;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Reservations;
using Checkbus.ApiService.Domain.Exceptions.Trips;
using Checkbus.ApiService.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Trips.Commands
{

    public class CreatePublicReservationCommandHandler
        : IRequestHandler<CreatePublicReservationCommand, CreatePublicReservationResultDto>
    {

        public const int HoldExpirationMinutes = 15;

        private readonly ITripRepository _tripRepository;
        private readonly ISeatRepository _seatRepository;
        private readonly IReservationRepository _reservationRepository;
        private readonly IPaymentService _paymentService;
        private readonly IMercadoPagoSellerTokenResolver _sellerTokenResolver;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly PublicWebOptions _publicWebOptions;
        private readonly ILogger<CreatePublicReservationCommandHandler> _logger;

        public CreatePublicReservationCommandHandler(
            ITripRepository tripRepository,
            ISeatRepository seatRepository,
            IReservationRepository reservationRepository,
            IPaymentService paymentService,
            IMercadoPagoSellerTokenResolver sellerTokenResolver,
            IUserRepository userRepository,
            IEmailService emailService,
            PublicWebOptions publicWebOptions,
            ILogger<CreatePublicReservationCommandHandler> logger)
        {
            _tripRepository = tripRepository;
            _seatRepository = seatRepository;
            _reservationRepository = reservationRepository;
            _paymentService = paymentService;
            _sellerTokenResolver = sellerTokenResolver;
            _userRepository = userRepository;
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

                throw new TripNotFoundException();
            }

            var seats = await _seatRepository.GetByVehicleIdAsync(trip.VehicleId, cancellationToken);
            var seatsById = seats.ToDictionary(s => s.Id);

            foreach (var entry in request.Passengers)
            {
                if (!seatsById.ContainsKey(entry.SeatId))
                {
                    throw new SeatsNoLongerAvailableException(
                        "One or more selected seats do not belong to this trip.");
                }
            }

            await _reservationRepository.ReleaseExpiredHoldsAsync(trip.Id, HoldExpirationMinutes, cancellationToken);

            var existingReservations = await _reservationRepository.GetByTripIdAsync(trip.Id, cancellationToken);
            var alreadyReservedSeatIds = existingReservations.Select(r => r.SeatId).ToHashSet();
            if (request.Passengers.Any(entry => alreadyReservedSeatIds.Contains(entry.SeatId)))
            {
                throw new SeatsNoLongerAvailableException();
            }

            var paymentId = Guid.NewGuid();

            var passengers = new List<Passenger>(request.Passengers.Count);
            var reservations = new List<Reservation>(request.Passengers.Count);
            var created = new List<CreatedReservationDto>(request.Passengers.Count);

            foreach (var entry in request.Passengers)
            {

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

                    Status = ReservationStatus.PendientePago,

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

            var sellerAccessToken = await _sellerTokenResolver.ResolveAccessTokenAsync(
                trip.OrganizationId, cancellationToken);

            var orderResult = await _paymentService.CreateOrderAsync(
                new CreateOrderRequest(
                    totalAmount,
                    paymentId.ToString(),
                    SuccessUrl: successUrl,
                    FailureUrl: failureUrl,
                    PendingUrl: pendingUrl),
                cancellationToken,
                sellerAccessToken);

            if (orderResult.CheckoutUrl is null)
            {

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

                throw new SeatsNoLongerAvailableException();
            }

            await NotifyOrganizationAdminsAsync(trip, reservations.Count, cancellationToken);

            return new CreatePublicReservationResultDto
            {
                Reservations = created,
                CheckoutUrl = orderResult.CheckoutUrl
            };
        }

        private async Task NotifyOrganizationAdminsAsync(
            Trip trip, int seatCount, CancellationToken cancellationToken)
        {
            var organizationUsers = await _userRepository.GetAllByOrganizationAsync(
                trip.OrganizationId, cancellationToken);
            var admins = organizationUsers.Where(u => u.Role == Role.Administrador).ToList();

            foreach (var admin in admins)
            {
                try
                {
                    await _emailService.SendAsync(BuildNewReservationEmail(admin, trip, seatCount), cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to send new-reservation notification to UserId {UserId} for TripId {TripId}",
                        admin.Id, trip.Id);
                }
            }
        }

        private static EmailMessage BuildNewReservationEmail(User admin, Trip trip, int seatCount)
        {
            var subject = $"Nueva reserva — viaje del {trip.DepartureDate:dd/MM/yyyy}";
            var seatsLabel = seatCount == 1 ? "1 asiento" : $"{seatCount} asientos";
            var body = $"""
                <h2>Nueva reserva recibida</h2>
                <p>Hola {admin.Name} {admin.Surname},</p>
                <p>Se registró una nueva reserva para el viaje con salida el
                <strong>{trip.DepartureDate:dd/MM/yyyy HH:mm}</strong>, por
                <strong>{seatsLabel}</strong>.</p>
                <p>TripId: {trip.Id}</p>
                """;

            return new EmailMessage(admin.Email, subject, body);
        }
    }
}
