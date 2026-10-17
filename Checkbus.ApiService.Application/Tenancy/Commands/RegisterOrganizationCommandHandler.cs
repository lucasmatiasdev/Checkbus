using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Email;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Users;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Authentication;
using Checkbus.ApiService.Domain.Exceptions.Tenancy;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Checkbus.ApiService.Application.Tenancy.Commands
{
    public class RegisterOrganizationCommandHandler : IRequestHandler<RegisterOrganizationCommand, RegisterOrganizationCommandResult>
    {
        // Unsuffixed + numeric suffixes 1..99, same budget UserEmailGenerator.Generate uses for
        // email local-part disambiguation — this is the equivalent loop for Organization.Slug,
        // which SanitizeSlug alone does not disambiguate.
        private const int MaxSlugDisambiguationAttempts = 100;

        private readonly IOrganizationRepository _organizationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly ILogger<RegisterOrganizationCommandHandler> _logger;

        public RegisterOrganizationCommandHandler(
            IOrganizationRepository organizationRepository,
            IUserRepository userRepository,
            IPasswordHasher passwordHasher,
            IEmailService emailService,
            ILogger<RegisterOrganizationCommandHandler> logger)
        {
            _organizationRepository = organizationRepository;
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<RegisterOrganizationCommandResult> Handle(RegisterOrganizationCommand request, CancellationToken cancellationToken)
        {
            if (await _organizationRepository.CuitExistsAsync(request.CUIT, cancellationToken))
            {
                throw new CuitAlreadyRegisteredException();
            }

            var slug = await ResolveUniqueSlugAsync(request.OrganizationName, cancellationToken);

            var now = DateTime.UtcNow;
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                CUIT = request.CUIT,
                Name = request.OrganizationName,
                Slug = slug,
                LogoUrl = "", // No logo upload in this iteration — out of scope.
                IsActive = true
            };

            await _organizationRepository.AddAsync(organization, cancellationToken);

            // Same email-generation sequence RegisterUserCommandHandler already uses: fetch
            // already-taken emails for the candidate local-part prefix, then let
            // UserEmailGenerator.Generate disambiguate.
            var domain = UserEmailGenerator.SanitizeSlug(slug) + ".com";
            var basePart = UserEmailGenerator.NormalizePart(request.AdminName) + "." + UserEmailGenerator.NormalizePart(request.AdminSurname);
            if (basePart.Length > UserEmailGenerator.MaxBaseLocalPartLength)
            {
                basePart = basePart[..UserEmailGenerator.MaxBaseLocalPartLength];
            }

            var taken = new HashSet<string>(
                await _userRepository.FindEmailsByLocalPartPrefixAsync(basePart, domain, cancellationToken),
                StringComparer.OrdinalIgnoreCase);

            var generated = UserEmailGenerator.Generate(request.AdminName, request.AdminSurname, slug, taken);
            if (!generated.Succeeded)
            {
                if (generated.Error == UserEmailGenerationError.DisambiguationExhausted)
                {
                    throw new EmailGenerationExhaustedException();
                }

                // EmptyLocalPart is unreachable through the HTTP pipeline —
                // RegisterOrganizationCommandValidator rejects it with 400 before the handler
                // ever runs (same defense-in-depth posture as RegisterUserCommandHandler's
                // equivalent branch). EmptyDomain is unreachable too: slug is always non-empty
                // by construction (ResolveUniqueSlugAsync already validated OrganizationName).
                throw new ValidationException(
                    [new ValidationFailure(nameof(request.AdminName), "AdminName and AdminSurname must together contain at least one letter or digit usable in an email address.")]);
            }

            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Name = request.AdminName,
                Surname = request.AdminSurname,
                Email = generated.Email!,
                PasswordHash = string.Empty,
                DocumentType = request.AdminDocumentType,
                DocumentNumber = request.AdminDocumentNumber,
                Role = Role.Administrador,
                OrganizationId = organization.Id,
                IsActive = true,
                MustChangePassword = true,
                CreatedAt = now,
                UpdatedAt = now
            };
            adminUser.PasswordHash = _passwordHasher.Hash(adminUser, request.AdminDocumentNumber);

            await _userRepository.AddAsync(adminUser, cancellationToken);

            // Same defensive posture as RegisterUserCommandHandler (CU-06): a flaky SMTP relay
            // must never fail a registration that's already persisted.
            try
            {
                await _emailService.SendAsync(BuildCredentialsEmail(adminUser, request.AdminDocumentNumber), cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to send credentials email for new admin UserId {UserId} of Organization {OrganizationId}",
                    adminUser.Id,
                    organization.Id);
            }

            return new RegisterOrganizationCommandResult
            {
                OrganizationId = organization.Id,
                OrganizationName = organization.Name,
                Slug = organization.Slug,
                AdminUserId = adminUser.Id,
                AdminEmail = adminUser.Email,
                AdminName = adminUser.Name,
                AdminSurname = adminUser.Surname
            };
        }

        private async Task<string> ResolveUniqueSlugAsync(string organizationName, CancellationToken cancellationToken)
        {
            var baseSlug = UserEmailGenerator.SanitizeSlug(organizationName);
            if (baseSlug.Length == 0)
            {
                // Unreachable through the HTTP pipeline — RegisterOrganizationCommandValidator
                // already rejects this with 400. Defense-in-depth only, same posture as the
                // email-generation branch above.
                throw new ValidationException(
                    [new ValidationFailure(nameof(RegisterOrganizationCommand.OrganizationName), "OrganizationName must contain at least one letter or digit usable in a URL slug.")]);
            }

            for (var suffix = 0; suffix < MaxSlugDisambiguationAttempts; suffix++)
            {
                var candidate = suffix == 0 ? baseSlug : $"{baseSlug}-{suffix}";
                if (!await _organizationRepository.SlugExistsAsync(candidate, cancellationToken))
                {
                    return candidate;
                }
            }

            // Practically unreachable (100 organizations with the exact same sanitized name) —
            // same "disambiguation exhausted" shape as UserEmailGenerator's own failure mode,
            // just without a dedicated exception type since there is nothing more specific to
            // communicate to the caller than "try a different name."
            throw new InvalidOperationException($"Could not generate a unique slug for organization name '{organizationName}' after {MaxSlugDisambiguationAttempts} attempts.");
        }

        private static EmailMessage BuildCredentialsEmail(User user, string initialPassword)
        {
            var subject = "Tus credenciales de acceso a Checkbus";
            var body = $"""
                <h2>¡Bienvenido/a a Checkbus!</h2>
                <p>Hola {user.Name} {user.Surname},</p>
                <p>Se creó la cuenta de administrador de tu organización con las siguientes credenciales:</p>
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
