using Checkbus.ApiService.Domain.ValueObjects;

namespace Checkbus.ApiService.Application.Interfaces.Payments
{
    /// <summary>Everything <see cref="IPaymentService.CreateOrderAsync"/> needs to open a
    /// Mercado Pago Orders API order. <see cref="ExternalReference"/> is the Application-chosen
    /// correlation id — Fase C sets it to the new <c>Payment.Id</c> so Fase D's webhook can
    /// resolve a notification back to the right <c>Payment</c>/<c>Reservation</c>s via
    /// <see cref="Checkbus.ApiService.Application.Interfaces.Repositories.IPaymentRepository.GetByExternalOrderIdAsync"/>.
    /// <see cref="PayerEmail"/> is optional — Mercado Pago does not require payer data to create
    /// an order, Fase C may or may not have it on hand depending on the checkout flow.</summary>
    public sealed record CreateOrderRequest(Money TotalAmount, string ExternalReference, string? PayerEmail = null);
}
