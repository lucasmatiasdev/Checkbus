namespace Checkbus.ApiService.Application.Payments.Commands
{
    public class StartMercadoPagoConnectionResultDto
    {
        // The Mercado Pago-hosted authorization page URL to redirect the admin's browser to.
        // Deliberately returned in the response body rather than issued as a server-side
        // redirect — the actual browser navigation is the Blazor client's own job (I8), the same
        // way TripReservation.razor drives its own NavigationManager.NavigateTo for the
        // Mercado Pago checkout URL rather than the API redirecting on its behalf.
        public required string AuthorizationUrl { get; set; }
    }
}
