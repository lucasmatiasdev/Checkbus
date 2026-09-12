using Checkbus.Application.Abstractions;
using Checkbus.Application.UseCases.Authentication.Register;
using Checkbus.Domain.Entities.Authentication;
using Checkbus.Domain.Entities.Authentication.Authorization;
using Checkbus.Domain.Entities.Tenancy;
using Checkbus.UnitTests.TestDoubles;
using NSubstitute;

namespace Checkbus.UnitTests.Application.UseCases.Authentication.Register
{
    public class RegisterUseCaseTests
    {
        private const int DefaultOrganizationId = 1;
        private const int DefaultProfileId = 7;
        private const string GeneratedPassword = "generated-temp-password";
        private const string HashedPassword = "hashed-password";

        private readonly IOrganizationRepository _organizationRepository = Substitute.For<IOrganizationRepository>();
        private readonly IProfileRepository _profileRepository = Substitute.For<IProfileRepository>();
        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly IPasswordGenerator _passwordGenerator = Substitute.For<IPasswordGenerator>();
        private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
        private readonly TestTimeProvider _timeProvider = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

        private RegisterUseCase CreateSut() => new(
            _organizationRepository,
            _profileRepository,
            _userRepository,
            _passwordGenerator,
            _passwordHasher,
            _timeProvider);

        private static Organization CreateOrganization(int id = DefaultOrganizationId, string name = "Acme Corp") =>
            new() { Id = id, Name = name, CUIT = "20-12345678-9" };

        private static Profile CreateProfile(int id = DefaultProfileId, Organization? organization = null) =>
            new() { Id = id, Name = "Default", Organization = organization };

        private void SetupDefaultOrganizationAndProfile()
        {
            var organization = CreateOrganization();
            var profile = CreateProfile(organization: organization);
            _organizationRepository.FindByIdAsync(DefaultOrganizationId, Arg.Any<CancellationToken>()).Returns(organization);
            _profileRepository.FindByIdAsync(DefaultProfileId, Arg.Any<CancellationToken>()).Returns(profile);
        }

        private static RegisterRequest CreateRequest(
            int organizationId = DefaultOrganizationId,
            int profileId = DefaultProfileId,
            string fullName = "Juan Perez",
            string documentNumber = "12345678") =>
            new(organizationId, profileId, fullName, documentNumber);

        // --- 2.3: input validation short-circuit ---

        [Fact]
        public async Task ExecuteAsync_EmptyFullName_ReturnsInvalidFullNameAndMakesNoRepositoryCalls()
        {
            var sut = CreateSut();

            var result = await sut.ExecuteAsync(CreateRequest(fullName: ""));

            Assert.False(result.Success);
            Assert.Equal(RegisterFailure.InvalidFullName, result.Failure);
            await _organizationRepository.DidNotReceive().FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
            await _profileRepository.DidNotReceive().FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
            await _userRepository.DidNotReceive().AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsync_FullNameOver200Chars_ReturnsInvalidFullNameAndMakesNoRepositoryCalls()
        {
            var sut = CreateSut();
            var longName = new string('a', 201);

            var result = await sut.ExecuteAsync(CreateRequest(fullName: longName));

            Assert.False(result.Success);
            Assert.Equal(RegisterFailure.InvalidFullName, result.Failure);
            await _organizationRepository.DidNotReceive().FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsync_EmptyDocumentNumber_ReturnsInvalidDocumentNumberAndMakesNoRepositoryCalls()
        {
            var sut = CreateSut();

            var result = await sut.ExecuteAsync(CreateRequest(documentNumber: ""));

            Assert.False(result.Success);
            Assert.Equal(RegisterFailure.InvalidDocumentNumber, result.Failure);
            await _organizationRepository.DidNotReceive().FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsync_DocumentNumberOver20Chars_ReturnsInvalidDocumentNumberAndMakesNoRepositoryCalls()
        {
            var sut = CreateSut();
            var longDocumentNumber = new string('9', 21);

            var result = await sut.ExecuteAsync(CreateRequest(documentNumber: longDocumentNumber));

            Assert.False(result.Success);
            Assert.Equal(RegisterFailure.InvalidDocumentNumber, result.Failure);
            await _organizationRepository.DidNotReceive().FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        // --- 2.5: organization/profile lookup + tenancy check ---

        [Fact]
        public async Task ExecuteAsync_UnknownOrganizationId_ReturnsOrganizationNotFound()
        {
            _organizationRepository.FindByIdAsync(DefaultOrganizationId, Arg.Any<CancellationToken>()).Returns((Organization?)null);
            var sut = CreateSut();

            var result = await sut.ExecuteAsync(CreateRequest());

            Assert.False(result.Success);
            Assert.Equal(RegisterFailure.OrganizationNotFound, result.Failure);
            await _profileRepository.DidNotReceive().FindByIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsync_UnknownProfileId_ReturnsProfileNotFound()
        {
            _organizationRepository.FindByIdAsync(DefaultOrganizationId, Arg.Any<CancellationToken>()).Returns(CreateOrganization());
            _profileRepository.FindByIdAsync(DefaultProfileId, Arg.Any<CancellationToken>()).Returns((Profile?)null);
            var sut = CreateSut();

            var result = await sut.ExecuteAsync(CreateRequest());

            Assert.False(result.Success);
            Assert.Equal(RegisterFailure.ProfileNotFound, result.Failure);
        }

        [Fact]
        public async Task ExecuteAsync_CrossTenantProfileId_ReturnsProfileNotFound()
        {
            var organization = CreateOrganization(id: DefaultOrganizationId);
            var otherOrganization = CreateOrganization(id: 999, name: "Other Org");
            _organizationRepository.FindByIdAsync(DefaultOrganizationId, Arg.Any<CancellationToken>()).Returns(organization);
            _profileRepository.FindByIdAsync(DefaultProfileId, Arg.Any<CancellationToken>()).Returns(CreateProfile(organization: otherOrganization));
            var sut = CreateSut();

            var result = await sut.ExecuteAsync(CreateRequest());

            Assert.False(result.Success);
            Assert.Equal(RegisterFailure.ProfileNotFound, result.Failure);
        }

        // --- 2.7: success path (slug/suffix pick, password generation, hashing) ---

        [Fact]
        public async Task ExecuteAsync_ValidRequestWithGlobalProfileAndNoExistingEmails_ReturnsSuccessWithNoSuffixEmailAndSetsFlags()
        {
            var organization = CreateOrganization(name: "Acme Corp");
            var profile = CreateProfile(organization: null); // global profile — proves tenancy check allows Organization == null
            _organizationRepository.FindByIdAsync(DefaultOrganizationId, Arg.Any<CancellationToken>()).Returns(organization);
            _profileRepository.FindByIdAsync(DefaultProfileId, Arg.Any<CancellationToken>()).Returns(profile);
            _userRepository.FindEmailsByPrefixAsync(DefaultOrganizationId, "juan.perez", Arg.Any<CancellationToken>())
                .Returns(new List<string>());
            _passwordGenerator.Generate().Returns(GeneratedPassword);
            _passwordHasher.Hash(GeneratedPassword).Returns(HashedPassword);
            _userRepository.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(UserInsertOutcome.Added);
            var sut = CreateSut();

            var result = await sut.ExecuteAsync(CreateRequest(fullName: "Juan Perez"));

            Assert.True(result.Success);
            Assert.Equal("juan.perez@acmecorp.com", result.GeneratedEmail);
            Assert.Equal(GeneratedPassword, result.TemporaryPassword);
            _passwordHasher.Received(1).Hash(GeneratedPassword);
            await _userRepository.Received(1).AddAsync(
                Arg.Is<User>(u => u.MustChangePassword && u.IsActive && u.PasswordHash == HashedPassword),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsync_SecondUserWithSameGeneratedName_ReturnsEmailWithSuffix1()
        {
            SetupDefaultOrganizationAndProfile();
            _userRepository.FindEmailsByPrefixAsync(DefaultOrganizationId, "juan.perez", Arg.Any<CancellationToken>())
                .Returns(new List<string> { "juan.perez@acmecorp.com" });
            _passwordGenerator.Generate().Returns(GeneratedPassword);
            _passwordHasher.Hash(GeneratedPassword).Returns(HashedPassword);
            _userRepository.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()).Returns(UserInsertOutcome.Added);
            var sut = CreateSut();

            var result = await sut.ExecuteAsync(CreateRequest(fullName: "Juan Perez"));

            Assert.True(result.Success);
            Assert.Equal("juan.perez1@acmecorp.com", result.GeneratedEmail);
        }

        // --- 2.9: bounded collision retry (D1 — MaxEmailCollisionRetries=3, 4 insert attempts total) ---

        [Fact]
        public async Task ExecuteAsync_SingleDuplicateEmailCollision_RetriesNextSuffixLocallyAndSucceeds()
        {
            SetupDefaultOrganizationAndProfile();
            _userRepository.FindEmailsByPrefixAsync(DefaultOrganizationId, "juan.perez", Arg.Any<CancellationToken>())
                .Returns(new List<string>());
            _passwordGenerator.Generate().Returns(GeneratedPassword);
            _passwordHasher.Hash(GeneratedPassword).Returns(HashedPassword);
            _userRepository.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
                .Returns(UserInsertOutcome.DuplicateEmail, UserInsertOutcome.Added);
            var sut = CreateSut();

            var result = await sut.ExecuteAsync(CreateRequest(fullName: "Juan Perez"));

            Assert.True(result.Success);
            Assert.Equal("juan.perez1@acmecorp.com", result.GeneratedEmail);
            await _userRepository.Received(2).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsync_FourConsecutiveDuplicateEmailCollisions_ReturnsEmailAlreadyRegistered()
        {
            SetupDefaultOrganizationAndProfile();
            _userRepository.FindEmailsByPrefixAsync(DefaultOrganizationId, "juan.perez", Arg.Any<CancellationToken>())
                .Returns(new List<string>());
            _passwordGenerator.Generate().Returns(GeneratedPassword);
            _passwordHasher.Hash(GeneratedPassword).Returns(HashedPassword);
            _userRepository.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>())
                .Returns(UserInsertOutcome.DuplicateEmail);
            var sut = CreateSut();

            var result = await sut.ExecuteAsync(CreateRequest(fullName: "Juan Perez"));

            Assert.False(result.Success);
            Assert.Equal(RegisterFailure.EmailAlreadyRegistered, result.Failure);
            await _userRepository.Received(4).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        }
    }
}
