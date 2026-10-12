namespace Checkbus.ApiService.Infrastructure.Implementations.Email
{
    /// <summary>
    /// Email/SMTP configuration. Unlike <c>JwtOptions</c>/<c>FileStorageOptions</c>, this is
    /// NOT throw-if-missing: the real Brevo SMTP credentials have not been provisioned yet, so
    /// binding must tolerate a missing "Email" config section or blank values. The real SMTP
    /// send only fails (at call time, not at startup) once something actually invokes
    /// <c>IEmailService</c> with no usable credentials.
    /// </summary>
    public class EmailOptions
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public bool UseStartTls { get; set; } = true;
    }
}
