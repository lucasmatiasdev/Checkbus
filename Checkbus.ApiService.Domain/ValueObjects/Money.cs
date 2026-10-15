namespace Checkbus.ApiService.Domain.ValueObjects
{
    // Mercado Pago (Argentina) is the only payment gateway this system talks to, so "ARS" is
    // the sole currency processed today — kept explicit (not a bare decimal) so Payment.
    // TotalAmount/Refund.Amount can't silently lose track of which currency an amount was
    // charged in, and so the Mercado Pago SDK's own amount+currency shape maps 1:1 without
    // information loss (odd/tasks/pagos-mercadopago.md Fase A/B). This is a plain immutable
    // value object, not its own table — CheckbusDbContext maps it as an EF Core owned type
    // (OwnsOne) on Payment/Refund, same posture as every other entity here (FK-only, no
    // navigation-heavy aggregates) just applied to a value object instead of an entity.
    public sealed record Money(decimal Amount, string Currency = "ARS");
}
