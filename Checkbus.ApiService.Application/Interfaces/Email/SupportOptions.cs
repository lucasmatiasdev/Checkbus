namespace Checkbus.ApiService.Application.Interfaces.Email
{
    /// <summary>
    /// Support ticket notification configuration. Same permissive binding as
    /// <c>EmailOptions</c>: NOT throw-if-missing, because the real notification address may not
    /// be provisioned yet — a missing "Support" config section or blank value must not fail
    /// startup. The create-ticket handler only fails to notify (at call time) if this is blank.
    ///
    /// Lives in Application (unlike <c>EmailOptions</c>, which stays in Infrastructure) because
    /// CreateSupportTicketCommandHandler — an Application-layer handler — needs
    /// <see cref="NotificationEmail"/> to address the email it sends; Application has no
    /// project reference to Infrastructure.
    /// </summary>
    public class SupportOptions
    {
        public string NotificationEmail { get; set; } = string.Empty;
    }
}
