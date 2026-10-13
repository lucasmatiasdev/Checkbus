using System.Net;
using System.Net.Http.Json;
using Checkbus.Web.Features.Payments.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Checkbus.Web.Features.Payments.Services;

/// <summary>
/// Typed client for the Planificador/Administrador-gated Mercado Pago connection endpoints
/// (<c>GET /api/mercadopago-connections/status</c>, <c>POST /api/mercadopago-connections/start</c>
/// — odd/tasks/pagos-mercadopago.md Fase I, I8). Maps every realistic server response into a
/// closed outcome so callers (the Payments settings Razor page) never need to inspect HTTP status
/// codes directly — same pattern as <see cref="Checkbus.Web.Features.Users.Services.UsersClient"/>.
/// </summary>
/// <remarks>
/// Takes the keyed <c>"apiservice"</c> <see cref="HttpClient"/> via <c>[FromKeyedServices]</c> for
/// the same reason <c>UsersClient</c> does — see that class's own doc comment.
/// </remarks>
public sealed class PaymentsClient([FromKeyedServices("apiservice")] HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<MercadoPagoConnectionStatusOutcome> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync("mercadopago-connections/status", cancellationToken);
        }
        catch (Exception)
        {
            return new MercadoPagoConnectionStatusOutcome.TransportError(
                "No pudimos conectar con el servidor. Probá de nuevo.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var status = await response.Content
                            .ReadFromJsonAsync<MercadoPagoConnectionStatusResponse>(cancellationToken);
                        return status is null
                            ? new MercadoPagoConnectionStatusOutcome.TransportError(
                                "Ocurrió un error inesperado. Probá de nuevo.")
                            : new MercadoPagoConnectionStatusOutcome.Success(status.Connected, status.SellerUserId);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new MercadoPagoConnectionStatusOutcome.Forbidden();

                    default:
                        return new MercadoPagoConnectionStatusOutcome.TransportError(
                            "Ocurrió un error inesperado. Probá de nuevo.");
                }
            }
            catch (Exception)
            {
                // A malformed or unparsable response body must not bubble up as an unhandled
                // exception — the caller only ever expects a typed outcome.
                return new MercadoPagoConnectionStatusOutcome.TransportError(
                    "Ocurrió un error inesperado. Probá de nuevo.");
            }
        }
    }

    public async Task<StartMercadoPagoConnectionOutcome> StartConnectionAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsync("mercadopago-connections/start", content: null, cancellationToken);
        }
        catch (Exception)
        {
            return new StartMercadoPagoConnectionOutcome.TransportError(
                "No pudimos conectar con el servidor. Probá de nuevo.");
        }

        using (response)
        {
            try
            {
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var result = await response.Content
                            .ReadFromJsonAsync<StartMercadoPagoConnectionResponse>(cancellationToken);
                        return result is null
                            ? new StartMercadoPagoConnectionOutcome.TransportError(
                                "Ocurrió un error inesperado. Probá de nuevo.")
                            : new StartMercadoPagoConnectionOutcome.Success(result.AuthorizationUrl);

                    case HttpStatusCode.Unauthorized:
                    case HttpStatusCode.Forbidden:
                        return new StartMercadoPagoConnectionOutcome.Forbidden();

                    default:
                        return new StartMercadoPagoConnectionOutcome.TransportError(
                            "Ocurrió un error inesperado. Probá de nuevo.");
                }
            }
            catch (Exception)
            {
                return new StartMercadoPagoConnectionOutcome.TransportError(
                    "Ocurrió un error inesperado. Probá de nuevo.");
            }
        }
    }
}
