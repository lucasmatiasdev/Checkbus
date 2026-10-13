using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Payments;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Trips.Events;
using Checkbus.ApiService.Domain.Entities.Invoices;
using Checkbus.ApiService.Domain.Entities.Payments;
using Checkbus.ApiService.Domain.Entities.Reservations;
using Checkbus.ApiService.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // Fase D4 (odd/tasks/pagos-mercadopago.md) — processes one Mercado Pago webhook
    // notification, already signature-validated by PaymentsWebhookController/D2 before this
    // command was ever dispatched.
    //
    // Dedupe tradeoff (D3) — RECORD BEFORE PROCESSING, deliberately: the ProcessedWebhookEvent
    // row is written immediately after the "already processed?" check, before GetOrderAsync or
    // any mutation. Accepted risk: if this process crashes between that record and finishing
    // processing, the event is never retried (Mercado Pago sees it as already-acknowledged once
    // the controller returns 200, and even if it retried, the dedupe check would now short-
    // circuit it) — a Payment could be stuck Pendiente, needing manual follow-up. Rejected
    // alternative (record AFTER processing): a crash before the record write means Mercado
    // Pago's aggressive retries would re-run the FULL approved/rejected branch on every retry
    // until the record finally succeeds — re-confirming reservations is a harmless no-op, but
    // re-creating Invoices, re-sending the confirmation email, and (worst case) re-calling
    // IPaymentService.RefundAsync for the orphaned-approval path are NOT idempotent operations
    // (each gets a fresh idempotency key in MercadoPagoPaymentService, so a replayed refund call
    // would be a second real refund). A single stuck-Pendiente Payment needing one manual
    // reconciliation is a strictly smaller failure than a multiplying side effect repeated on
    // every retry — same "log and surface for manual follow-up" posture this feature's own Fase
    // F2 already accepts for a flaky downstream refund call.
    //
    // Payment resolution: resolves via IPaymentRepository.GetByExternalOrderIdAsync(data.id) —
    // confirmed by reading Fase B's actual OrderStatusResult DTO first (Application/Interfaces/
    // Payments/OrderStatusResult.cs) that it carries only (OrderId, Status), no
    // external_reference — so there was never a second candidate path to choose between; this
    // is the only way to resolve a Payment from an authoritative source without trusting the
    // webhook body's own external_reference field, per this Fase's own rule.
    public class HandlePaymentWebhookCommandHandler : IRequestHandler<HandlePaymentWebhookCommand>
    {
        private readonly IProcessedWebhookEventRepository _processedWebhookEventRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IPaymentService _paymentService;
        private readonly IReservationRepository _reservationRepository;
        private readonly ITripRepository _tripRepository;
        private readonly IMercadoPagoSellerTokenResolver _sellerTokenResolver;
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IPassengerRepository _passengerRepository;
        private readonly IEmailService _emailService;
        private readonly IPublisher _publisher;
        private readonly ILogger<HandlePaymentWebhookCommandHandler> _logger;

        public HandlePaymentWebhookCommandHandler(
            IProcessedWebhookEventRepository processedWebhookEventRepository,
            IPaymentRepository paymentRepository,
            IPaymentService paymentService,
            IReservationRepository reservationRepository,
            ITripRepository tripRepository,
            IMercadoPagoSellerTokenResolver sellerTokenResolver,
            IInvoiceRepository invoiceRepository,
            IPassengerRepository passengerRepository,
            IEmailService emailService,
            IPublisher publisher,
            ILogger<HandlePaymentWebhookCommandHandler> logger)
        {
            _processedWebhookEventRepository = processedWebhookEventRepository;
            _paymentRepository = paymentRepository;
            _paymentService = paymentService;
            _reservationRepository = reservationRepository;
            _tripRepository = tripRepository;
            _sellerTokenResolver = sellerTokenResolver;
            _invoiceRepository = invoiceRepository;
            _passengerRepository = passengerRepository;
            _emailService = emailService;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task Handle(HandlePaymentWebhookCommand request, CancellationToken cancellationToken)
        {
            if (await _processedWebhookEventRepository.ExistsAsync(request.ExternalEventId, cancellationToken))
            {
                // Already handled (or at least already started) — Mercado Pago retrying the
                // exact same notification must be a complete no-op. See class doc comment for
                // the record-before-vs-after tradeoff.
                return;
            }

            await _processedWebhookEventRepository.RecordAsync(request.ExternalEventId, cancellationToken);

            var payment = await _paymentRepository.GetByExternalOrderIdAsync(request.ExternalOrderId, cancellationToken);
            if (payment is null)
            {
                _logger.LogWarning(
                    "Webhook notification for unknown Mercado Pago order {ExternalOrderId}.", request.ExternalOrderId);
                return;
            }

            // Fase I (Marketplace/Split Payments): resolve which Organization's own Mercado
            // Pago seller token covers this Payment — via any one of its linked Reservations'
            // Trip (the only entity in this module carrying OrganizationId) — BEFORE the
            // authoritative GetOrderAsync call below, since that call now needs it too. If
            // either hop can't be resolved (should not normally happen, FK integrity), log and
            // skip rather than crash the webhook — same defensive posture as the "unknown
            // ExternalOrderId" case just above. OrganizationNotConnectedToMercadoPagoException
            // itself is NOT caught here and propagates normally for the resolvable case, per
            // this Fase's own instruction.
            var reservationsForToken = await _reservationRepository.GetByPaymentIdAsync(payment.Id, cancellationToken);
            if (reservationsForToken.Count == 0)
            {
                _logger.LogWarning(
                    "No Reservations linked to Payment {PaymentId}; cannot resolve an Organization for its Mercado Pago seller token.",
                    payment.Id);
                return;
            }

            var trip = await _tripRepository.GetByIdAsync(reservationsForToken[0].TripId, cancellationToken);
            if (trip is null)
            {
                _logger.LogWarning(
                    "Trip {TripId} referenced by Payment {PaymentId}'s Reservations was not found; cannot resolve its Organization's Mercado Pago seller token.",
                    reservationsForToken[0].TripId, payment.Id);
                return;
            }

            var sellerAccessToken = await _sellerTokenResolver.ResolveAccessTokenAsync(trip.OrganizationId, cancellationToken);

            // Authoritative status, never the webhook body's own status field (this Fase's own
            // rule — see class doc comment).
            var orderStatus = await _paymentService.GetOrderAsync(request.ExternalOrderId, cancellationToken, sellerAccessToken);
            var mappedStatus = MapStatus(orderStatus.Status);
            if (mappedStatus is null)
            {
                _logger.LogWarning(
                    "Mercado Pago order {ExternalOrderId} returned an unmapped status ({Status}); Payment {PaymentId} left unchanged.",
                    request.ExternalOrderId, orderStatus.Status, payment.Id);
                return;
            }

            payment.Status = mappedStatus.Value;
            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            switch (mappedStatus.Value)
            {
                case PaymentStatus.Aprobado:
                    await HandleApprovedAsync(payment, request.ExternalOrderId, sellerAccessToken, cancellationToken);
                    break;

                case PaymentStatus.Rechazado:
                    await HandleRejectedAsync(payment.Id, cancellationToken);
                    break;

                default:
                    // Pending/in_process/etc — Payment.Status is already updated above; no
                    // reservation/invoice/email side effect for anything short of a terminal
                    // outcome.
                    break;
            }
        }

        private async Task HandleApprovedAsync(
            Payment payment, string externalOrderId, string sellerAccessToken, CancellationToken cancellationToken)
        {
            var reservations = await _reservationRepository.GetByPaymentIdAsync(payment.Id, cancellationToken);

            // The normal case: the hold is still active for every Reservation in this batch.
            // Any deviation (one or more already Cancelada/Confirmada by some other path, or no
            // linked Reservation at all) is treated conservatively as orphaned — real money was
            // charged for a hold this process can no longer honor as originally sold, so refund
            // rather than guess at a partial reconciliation.
            var allStillHeld = reservations.Count > 0
                && reservations.All(r => r.Status == ReservationStatus.PendientePago);

            if (!allStillHeld)
            {
                await HandleOrphanedApprovalAsync(payment, externalOrderId, reservations, sellerAccessToken, cancellationToken);
                return;
            }

            var confirmedReservationIds = new List<Guid>(reservations.Count);

            foreach (var reservation in reservations)
            {
                await _reservationRepository.ConfirmAsync(reservation.Id, cancellationToken);

                await _invoiceRepository.AddAsync(new Invoice
                {
                    Id = Guid.NewGuid(),
                    TripId = reservation.TripId,
                    PassengerId = reservation.PassengerId,
                    PaymentId = payment.Id,
                    IssuedAt = DateTime.UtcNow
                }, cancellationToken);

                confirmedReservationIds.Add(reservation.Id);
            }

            await _publisher.Publish(
                new PaymentApprovedEvent { PaymentId = payment.Id, ReservationIds = confirmedReservationIds },
                cancellationToken);
        }

        // Orphaned-approval case (odd/tasks/pagos-mercadopago.md design decisions): the order
        // was genuinely paid, but the hold it was meant to cover is gone — refund
        // unconditionally rather than queue for manual review, since real money was charged.
        private async Task HandleOrphanedApprovalAsync(
            Payment payment, string externalOrderId, IReadOnlyList<Reservation> reservations,
            string sellerAccessToken, CancellationToken cancellationToken)
        {
            await _paymentService.RefundAsync(externalOrderId, amount: null, cancellationToken, sellerAccessToken);

            payment.Status = PaymentStatus.Reembolsado;
            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            // A different message than D5's confirmation email — built inline here on purpose,
            // not forced through PaymentApprovedEvent (per this Fase's own instruction).
            foreach (var reservation in reservations)
            {
                var passenger = await _passengerRepository.GetByIdAsync(reservation.PassengerId, cancellationToken);
                if (passenger is null || string.IsNullOrWhiteSpace(passenger.Email))
                {
                    continue;
                }

                try
                {
                    var message = BuildOrphanedRefundEmail(passenger.Name, passenger.Email, payment.TotalAmount.Amount);
                    await _emailService.SendAsync(message, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex, "Failed to send orphaned-refund email for Payment {PaymentId}", payment.Id);
                }
            }
        }

        // Rejected path: release the holds immediately (don't wait for Fase E's lazy
        // expiration) — reuses the existing CancelAsync rather than inventing a new batch
        // method, per this Fase's own instruction not to duplicate logic that already exists.
        private async Task HandleRejectedAsync(Guid paymentId, CancellationToken cancellationToken)
        {
            var reservations = await _reservationRepository.GetByPaymentIdAsync(paymentId, cancellationToken);

            foreach (var reservation in reservations.Where(r => r.Status == ReservationStatus.PendientePago))
            {
                await _reservationRepository.CancelAsync(reservation.Id, cancellationToken);
            }
        }

        private static PaymentStatus? MapStatus(MercadoPagoOrderStatus status) => status switch
        {
            MercadoPagoOrderStatus.Approved => PaymentStatus.Aprobado,
            MercadoPagoOrderStatus.Rejected => PaymentStatus.Rechazado,
            MercadoPagoOrderStatus.Refunded => PaymentStatus.Reembolsado,
            MercadoPagoOrderStatus.Pending => PaymentStatus.Pendiente,
            _ => null
        };

        private static EmailMessage BuildOrphanedRefundEmail(string passengerName, string passengerEmail, decimal refundedAmount)
        {
            const string subject = "No pudimos confirmar tu reserva — ya procesamos tu reembolso";
            var body = $"""
                <h2>Tu pago fue reembolsado</h2>
                <p>Hola {passengerName},</p>
                <p>Lamentablemente no pudimos confirmar tu reserva porque el lugar ya no estaba disponible.</p>
                <p>Ya procesamos el reembolso completo de {refundedAmount:C} a tu medio de pago original.</p>
                """;

            return new EmailMessage(passengerEmail, subject, body);
        }
    }
}
