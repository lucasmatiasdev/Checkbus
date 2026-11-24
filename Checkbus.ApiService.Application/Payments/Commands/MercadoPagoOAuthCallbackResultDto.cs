namespace Checkbus.ApiService.Application.Payments.Commands
{
    public class MercadoPagoOAuthCallbackResultDto
    {
        public bool Success { get; set; }

        public string? FailureReason { get; set; }

        public required string RedirectUrl { get; set; }
    }
}
