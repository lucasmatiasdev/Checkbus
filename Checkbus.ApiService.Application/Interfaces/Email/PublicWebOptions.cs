namespace Checkbus.ApiService.Application.Interfaces.Email
{
    /// <summary>
    /// The public Web app's externally-reachable base URL, used to build links embedded in
    /// outgoing emails (e.g. the reservation confirmation page) — the API can't infer the
    /// frontend's address on its own. Permissive like <c>EmailOptions</c>: blank in
    /// appsettings.json, a real value supplied per-environment
    /// (appsettings.Development.json/user-secrets), never throw-if-missing.
    /// </summary>
    public class PublicWebOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
    }
}
