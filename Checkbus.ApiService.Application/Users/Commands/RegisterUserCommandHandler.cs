using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Documents;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Authentication;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Users.Commands
{
    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterUserCommandResult>
    {
        private readonly IUserRepository _userRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IDriverRequirementRepository _driverRequirementRepository;
        private readonly ICurrentUserService _currentUser;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly ILogger<RegisterUserCommandHandler> _logger;

        public RegisterUserCommandHandler(
            IUserRepository userRepository,
            IOrganizationRepository organizationRepository,
            IDriverRequirementRepository driverRequirementRepository,
            ICurrentUserService currentUser,
            IPasswordHasher passwordHasher,
            IEmailService emailService,
            ILogger<RegisterUserCommandHandler> logger)
        {
            _userRepository = userRepository;
            _organizationRepository = organizationRepository;
            _driverRequirementRepository = driverRequirementRepository;
            _currentUser = currentUser;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<RegisterUserCommandResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {

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

            if (user.Role == Role.Chofer)
            {
                var requirementNow = DateTime.UtcNow;
                var requirements = Enum.GetValues<DriverRequirementType>()
                    .Select(type => new DriverRequirement
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Type = type,
                        Status = DriverRequirementStatus.Pendiente,
                        DocumentPresent = false,
                        CreatedAt = requirementNow,
                        UpdatedAt = requirementNow
                    });

                await _driverRequirementRepository.AddRangeAsync(requirements, cancellationToken);
            }

            try
            {
                await _emailService.SendAsync(BuildCredentialsEmail(user, request.DocumentNumber), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send credentials email for UserId {UserId}", user.Id);
            }

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

        private static EmailMessage BuildCredentialsEmail(User user, string initialPassword)
        {
            var subject = "Tus credenciales de acceso a Checkbus";
            var body = $"""
                <h2>¡Bienvenido/a a Checkbus!</h2>
                <p>Hola {user.Name} {user.Surname},</p>
                <p>Se creó tu cuenta con las siguientes credenciales:</p>
                <ul>
                    <li>Usuario: {user.Email}</li>
                    <li>Contraseña inicial: {initialPassword}</li>
                </ul>
                <p>Por seguridad, vas a tener que cambiarla la primera vez que inicies sesión.</p>
                """;

            return new EmailMessage(user.Email, subject, body);
        }
    }
}
