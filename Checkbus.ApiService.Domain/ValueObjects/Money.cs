namespace Checkbus.ApiService.Domain.ValueObjects
{

    public sealed record Money(decimal Amount, string Currency = "ARS");
}
