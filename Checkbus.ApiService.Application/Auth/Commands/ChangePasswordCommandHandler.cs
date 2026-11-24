using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Exceptions.Authentication;
using MediatR;

namespace Checkbus.ApiService.Application.Auth.Commands
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
    {
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IPasswordHasher _passwordHasher;

        public ChangePasswordCommandHandler(
            IUserRepository userRepository,
            ICurrentUserService currentUser,
            IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _currentUser = currentUser;
            _passwordHasher = passwordHasher;
        }

        public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {

            var userId = _currentUser.UserId;
            var user = userId is null
                ? null
                : await _userRepository.FindByIdAsync(userId.Value, cancellationToken);

            if (user is null)
            {
                throw new UserNotFoundException();
            }

            user.PasswordHash = _passwordHasher.Hash(user, request.NewPassword);
            user.MustChangePassword = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);
        }
    }
}
