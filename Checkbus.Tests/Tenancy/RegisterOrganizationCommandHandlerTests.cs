using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Tenancy.Commands;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Tenancy;
using Microsoft.Extensions.Logging.Abstractions;

namespace Checkbus.Tests.Tenancy;

public class RegisterOrganizationCommandHandlerTests
{
    private sealed class FakeEmailService : IEmailService
    {
        public List<EmailMessage> SentMessages { get; } = [];
        public bool ThrowOnSend { get; set; }

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            if (ThrowOnSend)
            {
                throw new InvalidOperationException("Simulated SMTP failure.");
            }

            SentMessages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> AddedUsers { get; } = [];
        public IReadOnlyList<string> TakenEmails { get; set; } = [];

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => Task.FromResult<User?>(null);

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            AddedUsers.Add(user);
            return Task.CompletedTask;
        }

        public Task<bool> DocumentNumberExistsInOrganizationAsync(
            string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by RegisterOrganizationCommandHandler.");

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(
            string localPartPrefix, string domain, CancellationToken cancellationToken)
            => Task.FromResult(TakenEmails);

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by RegisterOrganizationCommandHandler.");

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by RegisterOrganizationCommandHandler.");

        public Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by RegisterOrganizationCommandHandler.");
    }

    private sealed class FakeOrganizationRepository : IOrganizationRepository
    {
        public List<Organization> AddedOrganizations { get; } = [];
        public HashSet<string> ExistingCuits { get; set; } = [];
        public HashSet<string> ExistingSlugs { get; set; } = [];

        public Task AddAsync(Organization organization, CancellationToken cancellationToken)
        {
            AddedOrganizations.Add(organization);
            return Task.CompletedTask;
        }

        public Task<string?> GetSlugByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by RegisterOrganizationCommandHandler.");

        public Task<string?> GetNameByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by RegisterOrganizationCommandHandler.");

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by RegisterOrganizationCommandHandler.");

        public Task<bool> CuitExistsAsync(string cuit, CancellationToken cancellationToken)
            => Task.FromResult(ExistingCuits.Contains(cuit));

        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
            => Task.FromResult(ExistingSlugs.Contains(slug));
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string? ReceivedPlaintext { get; private set; }

        public string Hash(User user, string password)
        {
            ReceivedPlaintext = password;
            return "hashed:" + password;
        }

        public bool Verify(string hash, string password) => hash == "hashed:" + password;
    }

    private static RegisterOrganizationCommand CreateCommand() => new()
    {
        OrganizationName = "Checkbus Demo",
        CUIT = "20-12345678-9",
        AdminName = "Jose",
        AdminSurname = "Diaz",
        AdminDocumentType = DocumentType.DNI,
        AdminDocumentNumber = "40123456"
    };

    private static RegisterOrganizationCommandHandler CreateHandler(
        FakeOrganizationRepository? organizationRepository = null,
        FakeUserRepository? userRepository = null,
        FakePasswordHasher? passwordHasher = null,
        FakeEmailService? emailService = null) => new(
            organizationRepository ?? new FakeOrganizationRepository(),
            userRepository ?? new FakeUserRepository(),
            passwordHasher ?? new FakePasswordHasher(),
            emailService ?? new FakeEmailService(),
            NullLogger<RegisterOrganizationCommandHandler>.Instance);

    [Fact]
    public async Task Handle_DuplicateCuit_ThrowsCuitAlreadyRegisteredException_AndCreatesNothing()
    {
        var organizationRepository = new FakeOrganizationRepository
        {
            ExistingCuits = ["20-12345678-9"]
        };
        var userRepository = new FakeUserRepository();
        var handler = CreateHandler(organizationRepository, userRepository);

        await Assert.ThrowsAsync<CuitAlreadyRegisteredException>(
            () => handler.Handle(CreateCommand(), TestContext.Current.CancellationToken));

        Assert.Empty(organizationRepository.AddedOrganizations);
        Assert.Empty(userRepository.AddedUsers);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesOrganizationWithSanitizedSlugAndIsActive()
    {
        var organizationRepository = new FakeOrganizationRepository();
        var handler = CreateHandler(organizationRepository);

        var result = await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        var created = Assert.Single(organizationRepository.AddedOrganizations);
        Assert.Equal("checkbusdemo", created.Slug);
        Assert.Equal("checkbusdemo", result.Slug);
        Assert.Equal("20-12345678-9", created.CUIT);
        Assert.Equal("Checkbus Demo", created.Name);
        Assert.Equal("", created.LogoUrl);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task Handle_OrganizationNameCollidesWithExistingSlug_AppliesNumericSuffix()
    {
        var organizationRepository = new FakeOrganizationRepository
        {
            ExistingSlugs = ["checkbusdemo"]
        };
        var handler = CreateHandler(organizationRepository);

        var result = await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.Equal("checkbusdemo-1", result.Slug);
        Assert.Equal("checkbusdemo-1", Assert.Single(organizationRepository.AddedOrganizations).Slug);
    }

    [Fact]
    public async Task Handle_OrganizationNameCollidesTwice_AppliesLowestFreeNumericSuffix()
    {
        var organizationRepository = new FakeOrganizationRepository
        {
            ExistingSlugs = ["checkbusdemo", "checkbusdemo-1"]
        };
        var handler = CreateHandler(organizationRepository);

        var result = await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.Equal("checkbusdemo-2", result.Slug);
    }

    [Fact]
    public async Task Handle_ValidRequest_GeneratesAdminEmailFromNameSurnameAndNewSlug()
    {
        var userRepository = new FakeUserRepository();
        var handler = CreateHandler(userRepository: userRepository);

        var result = await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.Equal("jose.diaz@checkbusdemo.com", result.AdminEmail);
        Assert.Equal("jose.diaz@checkbusdemo.com", Assert.Single(userRepository.AddedUsers).Email);
    }

    [Fact]
    public async Task Handle_AdminEmailCollidesWithTakenEmails_AppliesDisambiguationSuffix()
    {
        // Reuses UserEmailGenerator's own disambiguation mechanics (already verified by
        // UserEmailGeneratorTests / RegisterUserCommandHandlerTests) — this only proves the
        // handler calls it correctly with the already-taken-emails lookup.
        var userRepository = new FakeUserRepository
        {
            TakenEmails = ["jose.diaz@checkbusdemo.com"]
        };
        var handler = CreateHandler(userRepository: userRepository);

        var result = await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.Equal("jose.diaz1@checkbusdemo.com", result.AdminEmail);
    }

    [Fact]
    public async Task Handle_ValidRequest_HashesAdminDocumentNumberAsInitialPassword()
    {
        var passwordHasher = new FakePasswordHasher();
        var handler = CreateHandler(passwordHasher: passwordHasher);

        await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.Equal("40123456", passwordHasher.ReceivedPlaintext);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesAdminWithAdministradorRole_RegardlessOfAnyOtherInput()
    {
        var userRepository = new FakeUserRepository();
        var handler = CreateHandler(userRepository: userRepository);

        await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        var created = Assert.Single(userRepository.AddedUsers);
        Assert.Equal(Role.Administrador, created.Role);
    }

    [Fact]
    public async Task Handle_ValidRequest_NewAdminBelongsToNewOrganization()
    {
        var organizationRepository = new FakeOrganizationRepository();
        var userRepository = new FakeUserRepository();
        var handler = CreateHandler(organizationRepository, userRepository);

        await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        var organization = Assert.Single(organizationRepository.AddedOrganizations);
        var user = Assert.Single(userRepository.AddedUsers);
        Assert.Equal(organization.Id, user.OrganizationId);
    }

    [Fact]
    public async Task Handle_ValidRequest_SendsCredentialsEmailWithGeneratedAddressAndInitialPassword()
    {
        var emailService = new FakeEmailService();
        var handler = CreateHandler(emailService: emailService);
        var command = CreateCommand();

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        var message = Assert.Single(emailService.SentMessages);
        Assert.Equal(result.AdminEmail, message.To);
        Assert.Contains(result.AdminEmail, message.HtmlBody);
        Assert.Contains(command.AdminDocumentNumber, message.HtmlBody);
    }

    [Fact]
    public async Task Handle_CredentialsEmailSendFails_RegistrationStillSucceeds_AndDoesNotRevertCreatedEntities()
    {
        var emailService = new FakeEmailService { ThrowOnSend = true };
        var organizationRepository = new FakeOrganizationRepository();
        var userRepository = new FakeUserRepository();
        var handler = CreateHandler(organizationRepository, userRepository, emailService: emailService);

        var result = await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        Assert.NotEqual(Guid.Empty, result.OrganizationId);
        Assert.NotEqual(Guid.Empty, result.AdminUserId);
        Assert.Single(organizationRepository.AddedOrganizations);
        Assert.Single(userRepository.AddedUsers);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsResultMatchingPersistedEntities()
    {
        var organizationRepository = new FakeOrganizationRepository();
        var userRepository = new FakeUserRepository();
        var handler = CreateHandler(organizationRepository, userRepository);

        var result = await handler.Handle(CreateCommand(), TestContext.Current.CancellationToken);

        var organization = Assert.Single(organizationRepository.AddedOrganizations);
        var user = Assert.Single(userRepository.AddedUsers);
        Assert.Equal(organization.Id, result.OrganizationId);
        Assert.Equal(organization.Name, result.OrganizationName);
        Assert.Equal(organization.Slug, result.Slug);
        Assert.Equal(user.Id, result.AdminUserId);
        Assert.Equal(user.Email, result.AdminEmail);
        Assert.Equal(user.Name, result.AdminName);
        Assert.Equal(user.Surname, result.AdminSurname);
    }
}
