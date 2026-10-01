using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Exceptions.Authentication;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Checkbus.ApiService.Application.Users.Commands
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserCommandResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IPasswordHasher _passwordHasher;

        public RegisterUserCommandHandler(
            IUserRepository userRepository,
            IOrganizationRepository organizationRepository,
            ICurrentUserService currentUser,
            IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _organizationRepository = organizationRepository;
            _currentUser = currentUser;
            _passwordHasher = passwordHasher;
        }

        public async Task<RegisterUserCommandResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            // Tenant comes exclusively from the validated claim — the command has no
            // OrganizationId field, so no request body value can ever influence it.
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            if (await _userRepository.DocumentNumberExistsInOrganizationAsync(request.DocumentNumber, organizationId, cancellationToken))
            {
                throw new DocumentNumberAlreadyRegisteredException();
            }

            var slug = await _organizationRepository.GetSlugByIdAsync(organizationId, cancellationToken)
                ?? throw new InvalidOperationException($"Organization {organizationId} from the caller's token does not exist.");

            var domain = UserEmailGenerator.SanitizeSlug(slug) + ".com";
            var basePart = UserEmailGenerator.NormalizePart(request.Name) + "." + UserEmailGenerator.NormalizePart(request.Surname);
            if (basePart.Length > UserEmailGenerator.MaxBaseLocalPartLength)
            {
                basePart = basePart[..UserEmailGenerator.MaxBaseLocalPartLength];
            }

            var taken = new HashSet<string>(
                await _userRepository.FindEmailsByLocalPartPrefixAsync(basePart, domain, cancellationToken),
                StringComparer.OrdinalIgnoreCase);

            var generated = UserEmailGenerator.Generate(request.Name, request.Surname, slug, taken);
            if (!generated.Succeeded)
            {
                if (generated.Error == UserEmailGenerationError.DisambiguationExhausted)
                {
                    throw new EmailGenerationExhaustedException();
                }

                // EmptyLocalPart / EmptyDomain are unreachable through the HTTP pipeline —
                // RegisterUserCommandValidator rejects both with 400 before the handler
                // ever runs. This branch is defense-in-depth only (D-3).
                throw new ValidationException(
                    [new ValidationFailure(nameof(request.Name), "Name and Surname must together contain at least one letter or digit usable in an email address.")]);
            }

            var now = DateTime.UtcNow;
            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Surname = request.Surname,
                Email = generated.Email!,
                PasswordHash = string.Empty,
                DocumentType = request.DocumentType,
                DocumentNumber = request.DocumentNumber,
                Role = request.Role,
                OrganizationId = organizationId,
                IsActive = true,
                MustChangePassword = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            user.PasswordHash = _passwordHasher.Hash(user, request.DocumentNumber);

            await _userRepository.AddAsync(user, cancellationToken);

            return new RegisterUserCommandResult
            {
                UserId = user.Id,
                Email = user.Email,
                Name = user.Name,
                Surname = user.Surname,
                Role = user.Role.ToString(),
                OrganizationId = user.OrganizationId,
                MustChangePassword = user.MustChangePassword
            };
        }
    }
}
