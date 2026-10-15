namespace Checkbus.ApiService.Application.Payments.Commands
{
    public class MercadoPagoOAuthCallbackResultDto
    {
        public bool Success { get; set; }

        // Null on success. A short machine-readable token (e.g. "invalid_state",
        // "exchange_failed"), not a full sentence — the Web app's landing page (I8) is
        // responsible for turning this into a human-presentable message, same "never leak raw
        // exception detail to the client" posture as every outcome type in this codebase.
        public string? FailureReason { get; set; }

        // Where to send the admin's browser next — always the Web app's Payments settings page,
        // success or failure, differing only by query string. The controller does nothing but
        // Redirect() to this value; building it here (not in the controller) keeps the
        // controller a thin MediatR dispatcher, same posture as every other controller action in
        // this codebase.
        public required string RedirectUrl { get; set; }
    }
}
