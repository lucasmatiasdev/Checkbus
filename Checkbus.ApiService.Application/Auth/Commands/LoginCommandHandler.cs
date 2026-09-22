using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Exceptions.Authentication;
using MediatR;

namespace Checkbus.ApiService.Application.Auth.Commands
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginCommandResult>
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

        public async Task<LoginCommandResult> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.FindByEmailAsync(request.Email, cancellationToken);
            if (user is null)
                throw new UserNotFoundException();

            if (!user.IsActive)
                throw new UserInactiveException();

            if (!_passwordHasher.Verify(user.PasswordHash, request.Password))
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
