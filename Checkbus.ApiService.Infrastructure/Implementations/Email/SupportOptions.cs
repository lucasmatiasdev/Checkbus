namespace Checkbus.ApiService.Infrastructure.Implementations.Email
{
    /// <summary>
    /// Support ticket notification configuration. Same permissive binding as
    /// <c>EmailOptions</c>: NOT throw-if-missing, because the real notification address may not
    /// be provisioned yet — a missing "Support" config section or blank value must not fail
    /// startup. The create-ticket handler only fails to notify (at call time) if this is blank.
    /// </summary>
    public class SupportOptions
    {
        public string NotificationEmail { get; set; } = string.Empty;
    }
}
