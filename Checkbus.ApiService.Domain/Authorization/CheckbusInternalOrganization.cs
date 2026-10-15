namespace Checkbus.ApiService.Domain.Authorization
{
    /// <summary>
    /// Identifies the sentinel Organization row that represents Checkbus itself (not a
    /// client/tenant) — the Role.CheckbusSoporte role is only ever meaningful for a caller
    /// whose OrganizationId resolves to this Organization's Slug. User.OrganizationId stays a
    /// non-nullable required FK (no schema change) by modeling Checkbus's own staff as regular
    /// Users of this one special Organization, same pattern as any client Organization.
    /// Authorization checks for CheckbusSoporte-gated endpoints MUST verify this Slug, not just
    /// the Role claim — a caller's Role alone is not proof of which Organization they belong to.
    /// </summary>
    public static class CheckbusInternalOrganization
    {
        public const string Slug = "checkbus-interno";
    }
}
