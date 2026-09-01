namespace Checkbus.BLL.Organization;

/// <summary>
/// Result of a successful company registration (CU-02): the new tenant and
/// its initial administrator user, ready to be signed in directly.
/// </summary>
public record CompanyRegistrationResult(int OrganizationId, int AdminUserId, int AdminRoleId, string AdminRoleName);

/// <summary>
/// CU-02 — company registration ("alta de empresa"): creates the
/// <see cref="Checkbus.BEL.Organization.Organization"/> tenant together with
/// its initial <c>Administrador</c> user in a single transaction, so a
/// failure on either side leaves no partial tenant behind.
/// </summary>
public interface ICompanyRegistrationService
{
    /// <exception cref="InvalidOperationException">
    /// Thrown when the CUIT or the admin e-mail is already in use.
    /// </exception>
    Task<CompanyRegistrationResult> RegisterCompanyAsync(
        string organizationName,
        string cuit,
        string adminFullName,
        string adminEmail,
        string adminPassword);
}
