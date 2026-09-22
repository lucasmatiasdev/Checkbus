using Checkbus.ApiService.Application.Common;
using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Exceptions.Authentication;

namespace Checkbus.ApiService.Application.Auth.Commands
{
    public class LoginCommandHandler : ICommandHandler<LoginCommand, LoginCommandResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtGenerator _jwtGenerator;

        public LoginCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, IJwtGenerator jwtGenerator)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtGenerator = jwtGenerator;
        }

        public async Task<LoginCommandResult> HandleAsync(LoginCommand command)
        {
            var user = await _userRepository.FindByEmailAsync(command.Email);
            if (user is null)
                throw new UserNotFoundException();

            if (!user.IsActive)
                throw new UserInactiveException();

            if (!_passwordHasher.Verify(user.PasswordHash, command.Password))
                throw new InvalidCredentialsException();

            var token = _jwtGenerator.GenerateToken(user);

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
