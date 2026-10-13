namespace Checkbus.Web.Features.Payments.Contracts;

/// <summary>
/// Web-side mirror of
/// <c>Checkbus.ApiService.Application.Payments.Queries.MercadoPagoConnectionStatusDto</c>, the
/// <c>200</c> response body for <c>GET /api/mercadopago-connections/status</c>. Checkbus.Web does
/// not reference the API projects (pure BFF), so this DTO is duplicated here rather than shared —
/// matching the existing <c>UserListItemResponse</c> mirror-DTO convention.
/// </summary>
public sealed record MercadoPagoConnectionStatusResponse
{
    public required bool Connected { get; init; }
    public string? SellerUserId { get; init; }
}
