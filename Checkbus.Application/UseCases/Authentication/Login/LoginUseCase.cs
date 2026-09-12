using Checkbus.Application.Abstractions;

namespace Checkbus.Application.UseCases.Authentication.Login
{
    public sealed class LoginUseCase
    {
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly TimeProvider _timeProvider;

        public LoginUseCase(IUserRepository userRepository, IPasswordHasher passwordHasher, TimeProvider timeProvider)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _timeProvider = timeProvider;
        }

        public async Task<LoginResult> ExecuteAsync(LoginRequest request, CancellationToken ct = default)
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var now = _timeProvider.GetUtcNow();

            var user = await _userRepository.FindByEmailAsync(normalizedEmail, ct);
            if (user is null)
            {
                return LoginResult.Failed(LoginFailure.InvalidCredentials);
            }

            if (user.LockedUntil > now)
            {
                return LoginResult.Failed(LoginFailure.AccountLocked);
            }

            if (!user.IsActive)
            {
                return LoginResult.Failed(LoginFailure.AccountInactive);
            }

            if (user.LockedUntil is not null)
            {
                user.FailedLoginAttempts = 0;
                user.LockedUntil = null;
            }

            if (!_passwordHasher.Verify(user.PasswordHash, request.Password))
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                {
                    user.LockedUntil = now.Add(LockoutDuration);
                }

                user.UpdatedAt = now.UtcDateTime;
                await _userRepository.SaveChangesAsync(ct);

                return LoginResult.Failed(LoginFailure.InvalidCredentials);
            }

            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            user.UpdatedAt = now.UtcDateTime;
            await _userRepository.SaveChangesAsync(ct);

            return LoginResult.Succeeded(user.Id, user.Email, user.OrganizationId, user.MustChangePassword);
        }
    }
}
