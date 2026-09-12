using Checkbus.Application.Abstractions;
using Checkbus.Application.UseCases.Authentication.Login;
using Checkbus.Domain.Entities.Authentication;
using Checkbus.Domain.Entities.Authentication.Authorization;
using Checkbus.Domain.Entities.Tenancy;
using Checkbus.UnitTests.TestDoubles;
using NSubstitute;

namespace Checkbus.UnitTests.Application.UseCases.Authentication.Login
{
    public class LoginUseCaseTests
    {
        private const string ValidPassword = "correct-password";
        private const string StoredHash = "stored-hash";
        private const int DefaultProfileId = 7;

        private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
        private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
        private readonly TestTimeProvider _timeProvider = new(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));

        private LoginUseCase CreateSut() => new(_userRepository, _passwordHasher, _timeProvider);

        private static User CreateUser(
            string email = "user@example.com",
            string passwordHash = StoredHash,
            bool isActive = true,
            DateTimeOffset? lockedUntil = null,
            int failedLoginAttempts = 0,
            int organizationId = 1,
            bool mustChangePassword = false)
        {
            return new User
            {
                Id = 42,
                FullName = "Test User",
                Email = email,
                PasswordHash = passwordHash,
                Organization = new Organization { Id = organizationId, Name = "Org", CUIT = "20-12345678-9" },
                OrganizationId = organizationId,
                Profile = new Profile { Id = DefaultProfileId, Name = "Default", OrganizationId = organizationId },
                ProfileId = DefaultProfileId,
                DocumentNumber = "12345678",
                IsActive = isActive,
                LockedUntil = lockedUntil,
                FailedLoginAttempts = failedLoginAttempts,
                MustChangePassword = mustChangePassword
            };
        }

        [Fact]
        public async Task ExecuteAsync_UnknownEmail_ReturnsInvalidCredentialsAndDoesNotPersist()
        {
            _userRepository.FindByEmailAsync("unknown@example.com", Arg.Any<CancellationToken>())
                .Returns((User?)null);

            var sut = CreateSut();

            var result = await sut.ExecuteAsync(new LoginRequest("unknown@example.com", "any-password"));

            Assert.False(result.Success);
            Assert.Equal(LoginFailure.InvalidCredentials, result.Failure);
            await _userRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsync_AccountLockedInFuture_ReturnsAccountLockedWithoutCounterChange()
        {
            var lockedUntil = _timeProvider.GetUtcNow().AddMinutes(10);
            var user = CreateUser(lockedUntil: lockedUntil, failedLoginAttempts: 3);
            _userRepository.FindByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

            var sut = CreateSut();

            var result = await sut.ExecuteAsync(new LoginRequest(user.Email, ValidPassword));

            Assert.False(result.Success);
            Assert.Equal(LoginFailure.AccountLocked, result.Failure);
            Assert.Equal(3, user.FailedLoginAttempts);
            _passwordHasher.DidNotReceive().Verify(Arg.Any<string>(), Arg.Any<string>());
            await _userRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsync_InactiveAccount_ReturnsAccountInactive()
        {
            var user = CreateUser(isActive: false);
            _userRepository.FindByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

            var sut = CreateSut();

            var result = await sut.ExecuteAsync(new LoginRequest(user.Email, ValidPassword));

            Assert.False(result.Success);
            Assert.Equal(LoginFailure.AccountInactive, result.Failure);
            await _userRepository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsync_LockedAndInactiveAccount_ReturnsAccountLockedFirst()
        {
            var lockedUntil = _timeProvider.GetUtcNow().AddMinutes(5);
            var user = CreateUser(isActive: false, lockedUntil: lockedUntil);
            _userRepository.FindByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);

            var sut = CreateSut();

            var result = await sut.ExecuteAsync(new LoginRequest(user.Email, ValidPassword));

            Assert.False(result.Success);
            Assert.Equal(LoginFailure.AccountLocked, result.Failure);
        }

        [Fact]
        public async Task ExecuteAsync_ValidCredentials_ReturnsSuccessAndResetsCounter()
        {
            var user = CreateUser(failedLoginAttempts: 2);
            _userRepository.FindByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
            _passwordHasher.Verify(StoredHash, ValidPassword).Returns(true);

            var sut = CreateSut();

            var result = await sut.ExecuteAsync(new LoginRequest(user.Email, ValidPassword));

            Assert.True(result.Success);
            Assert.Equal(user.Id, result.UserId);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.Organization.Id, result.OrganizationId);
            Assert.Equal(0, user.FailedLoginAttempts);
            Assert.Null(user.LockedUntil);
            await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsync_ValidCredentialsWithMustChangePassword_ReturnsMustChangePasswordTrue()
        {
            var user = CreateUser(mustChangePassword: true);
            _userRepository.FindByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
            _passwordHasher.Verify(StoredHash, ValidPassword).Returns(true);

            var sut = CreateSut();

            var result = await sut.ExecuteAsync(new LoginRequest(user.Email, ValidPassword));

            Assert.True(result.Success);
            Assert.True(result.MustChangePassword);
        }

        [Fact]
        public async Task ExecuteAsync_WrongPasswordBelowThreshold_IncrementsCounterAndReturnsInvalidCredentials()
        {
            var user = CreateUser(failedLoginAttempts: 2);
            _userRepository.FindByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
            _passwordHasher.Verify(StoredHash, "wrong-password").Returns(false);

            var sut = CreateSut();

            var result = await sut.ExecuteAsync(new LoginRequest(user.Email, "wrong-password"));

            Assert.False(result.Success);
            Assert.Equal(LoginFailure.InvalidCredentials, result.Failure);
            Assert.Equal(3, user.FailedLoginAttempts);
            Assert.Null(user.LockedUntil);
            await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsync_FifthConsecutiveFailure_LocksAccountFor15Minutes()
        {
            var user = CreateUser(failedLoginAttempts: 4);
            _userRepository.FindByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
            _passwordHasher.Verify(StoredHash, "wrong-password").Returns(false);

            var sut = CreateSut();

            var result = await sut.ExecuteAsync(new LoginRequest(user.Email, "wrong-password"));

            Assert.False(result.Success);
            Assert.Equal(LoginFailure.InvalidCredentials, result.Failure);
            Assert.Equal(5, user.FailedLoginAttempts);
            Assert.Equal(_timeProvider.GetUtcNow().AddMinutes(15), user.LockedUntil);
            await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ExecuteAsync_LockExpired_ResetsCounterInMemoryAndProceedsToSuccess()
        {
            var pastLock = _timeProvider.GetUtcNow().AddMinutes(-1);
            var user = CreateUser(failedLoginAttempts: 5, lockedUntil: pastLock);
            _userRepository.FindByEmailAsync(user.Email, Arg.Any<CancellationToken>()).Returns(user);
            _passwordHasher.Verify(StoredHash, ValidPassword).Returns(true);

            var sut = CreateSut();

            var result = await sut.ExecuteAsync(new LoginRequest(user.Email, ValidPassword));

            Assert.True(result.Success);
            Assert.Equal(0, user.FailedLoginAttempts);
            Assert.Null(user.LockedUntil);
            await _userRepository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        }
    }
}
