using Checkbus.BEL.Auth;
using Checkbus.DAL.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OrganizationEntity = Checkbus.BEL.Organization.Organization;

namespace Checkbus.BLL.Organization;

/// <summary>
/// Default <see cref="ICompanyRegistrationService"/>. Runs both inserts
/// (organization, then its admin user once the organization has an assigned
/// Id) inside one explicit database transaction so a failure — most notably a
/// duplicate CUIT or a duplicate admin e-mail — leaves neither row behind.
/// </summary>
public class CompanyRegistrationService : ICompanyRegistrationService
{
    private readonly IDbContextFactory<CheckbusDbContext> _contextFactory;
    private readonly IPasswordHasher<User> _passwordHasher;

    public CompanyRegistrationService(IDbContextFactory<CheckbusDbContext> contextFactory, IPasswordHasher<User> passwordHasher)
    {
        _contextFactory = contextFactory;
        _passwordHasher = passwordHasher;
    }

    public async Task<CompanyRegistrationResult> RegisterCompanyAsync(
        string organizationName,
        string cuit,
        string adminFullName,
        string adminEmail,
        string adminPassword)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var organization = new OrganizationEntity { Name = organizationName, Cuit = cuit };
            context.Organizations.Add(organization);
            await context.SaveChangesAsync();

            var admin = new User
            {
                OrganizationId = organization.Id,
                Email = adminEmail,
                FullName = adminFullName,
                PasswordHash = string.Empty,
                RoleId = RoleNames.AdministradorId,
            };
            admin.PasswordHash = _passwordHasher.HashPassword(admin, adminPassword);

            context.Users.Add(admin);
            await context.SaveChangesAsync();

            await transaction.CommitAsync();

            return new CompanyRegistrationResult(organization.Id, admin.Id, RoleNames.AdministradorId, RoleNames.Administrador);
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException(
                "Company registration failed: the CUIT or the admin e-mail is already in use.", ex);
        }
    }
}
