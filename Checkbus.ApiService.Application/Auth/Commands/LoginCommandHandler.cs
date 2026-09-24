using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Exceptions.Authentication;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Auth.Commands
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginCommandResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtGenerator _jwtGenerator;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IJwtGenerator jwtGenerator,
            ILogger<LoginCommandHandler> logger)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtGenerator = jwtGenerator;
            _logger = logger;
        }

        public async Task<LoginCommandResult> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);
            if (user is null)
            {
                _logger.LogWarning("LoginFailed with FailureReason {FailureReason}", "UserNotFound");
                throw new UserNotFoundException();
            }

            if (!user.IsActive)
            {
                _logger.LogWarning(
                    "LoginFailed for UserId {UserId} in OrganizationId {OrganizationId} with FailureReason {FailureReason}",
                    user.Id, user.OrganizationId, "UserInactive");
                throw new UserInactiveException();
            }

            if (!_passwordHasher.Verify(user.PasswordHash, request.Password))
            {
                _logger.LogWarning(
                    "LoginFailed for UserId {UserId} in OrganizationId {OrganizationId} with FailureReason {FailureReason}",
                    user.Id, user.OrganizationId, "InvalidCredentials");
                throw new InvalidCredentialsException();
            }

            var token = _jwtGenerator.GenerateToken(user);

            _logger.LogInformation(
                "LoginSucceeded for UserId {UserId} in OrganizationId {OrganizationId} with Role {Role}",
                user.Id, user.OrganizationId, user.Role.Name);

            return new LoginCommandResult
            {
                Token = token,
                UserId = user.Id,
                OrganizationId = user.OrganizationId,
                Role = user.Role.Name,
                Username = user.Username
            };
        }
    }
}
