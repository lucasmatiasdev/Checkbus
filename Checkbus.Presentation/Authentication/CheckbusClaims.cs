namespace Checkbus.Presentation.Authentication
{
    /// <summary>
    /// Checkbus-specific claim type constants issued by <see cref="LoginPrincipalFactory"/> and
    /// consumed by <see cref="CircuitCurrentTenant"/> (see design D1/D3).
    /// </summary>
    public static class CheckbusClaims
    {
        public const string OrganizationId = "checkbus:organization_id";
        public const string MustChangePassword = "checkbus:must_change_password";
    }
}
