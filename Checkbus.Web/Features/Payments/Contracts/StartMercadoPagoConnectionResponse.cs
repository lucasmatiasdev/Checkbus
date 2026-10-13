namespace Checkbus.Web.Features.Payments.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Payments.Commands.StartMercadoPagoConnectionResultDto</c>,
/// the <c>200</c> response body for <c>POST /api/mercadopago-connections/start</c>.
/// </summary>
public sealed record StartMercadoPagoConnectionResponse
{
    public required string AuthorizationUrl { get; init; }
}
