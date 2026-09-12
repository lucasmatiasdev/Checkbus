using Checkbus.Application.Abstractions;
using Checkbus.Domain.Entities.Authentication;

namespace Checkbus.Application.UseCases.Authentication.Register
{
    public sealed class RegisterUseCase
    {
        private const int MaxFullNameLength = 200;
        private const int MaxDocumentNumberLength = 20;
        private const int LocalPartMaxLength = 60;
        private const int DomainLabelMaxLength = 63;
        private const int MaxEmailCollisionRetries = 3;

        private readonly IOrganizationRepository _organizationRepository;
        private readonly IProfileRepository _profileRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPasswordGenerator _passwordGenerator;
        private readonly IPasswordHasher _passwordHasher;
        private readonly TimeProvider _timeProvider;

        public RegisterUseCase(
            IOrganizationRepository organizationRepository,
            IProfileRepository profileRepository,
            IUserRepository userRepository,
            IPasswordGenerator passwordGenerator,
            IPasswordHasher passwordHasher,
            TimeProvider timeProvider)
        {
            _organizationRepository = organizationRepository;
            _profileRepository = profileRepository;
            _userRepository = userRepository;
            _passwordGenerator = passwordGenerator;
            _passwordHasher = passwordHasher;
            _timeProvider = timeProvider;
        }

        public async Task<RegisterResult> ExecuteAsync(RegisterRequest request, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Length > MaxFullNameLength)
            {
                return RegisterResult.Failed(RegisterFailure.InvalidFullName);
            }

            if (string.IsNullOrWhiteSpace(request.DocumentNumber) || request.DocumentNumber.Length > MaxDocumentNumberLength)
            {
                return RegisterResult.Failed(RegisterFailure.InvalidDocumentNumber);
            }

            var organization = await _organizationRepository.FindByIdAsync(request.OrganizationId, ct);
            if (organization is null)
            {
                return RegisterResult.Failed(RegisterFailure.OrganizationNotFound);
            }

            var profile = await _profileRepository.FindByIdAsync(request.ProfileId, ct);
            if (profile is null)
            {
                return RegisterResult.Failed(RegisterFailure.ProfileNotFound);
            }

            if (profile.OrganizationId is not null && profile.OrganizationId != request.OrganizationId)
            {
                // Deliberately indistinguishable from "profile does not exist" (design D5/spec cross-tenant scenario).
                return RegisterResult.Failed(RegisterFailure.ProfileNotFound);
            }

            var localBase = EmailIdentifierGenerator.Slug(request.FullName, ".", LocalPartMaxLength);
            if (localBase.Length == 0)
            {
                // A full name made only of punctuation/whitespace-equivalents slugs to nothing.
                return RegisterResult.Failed(RegisterFailure.InvalidFullName);
            }

            var domainSlug = EmailIdentifierGenerator.Slug(organization.Name, string.Empty, DomainLabelMaxLength);
            var domainLabel = domainSlug.Length == 0 ? $"org{organization.Id}" : domainSlug;
            var domain = $"{domainLabel}.com";

            var takenEmails = await _userRepository.FindEmailsByPrefixAsync(organization.Id, localBase, ct);
            var taken = new HashSet<string>(takenEmails, StringComparer.OrdinalIgnoreCase);

            var suffix = 0;
            while (taken.Contains(CandidateEmail(localBase, suffix, domain)))
            {
                suffix++;
            }

            var password = _passwordGenerator.Generate();
            var passwordHash = _passwordHasher.Hash(password);
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            var user = new User
            {
                FullName = request.FullName,
                Email = CandidateEmail(localBase, suffix, domain),
                PasswordHash = passwordHash,
                Organization = organization,
                OrganizationId = organization.Id,
                Profile = profile,
                ProfileId = profile.Id,
                DocumentNumber = request.DocumentNumber,
                IsActive = true,
                MustChangePassword = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            for (var attempt = 0; attempt <= MaxEmailCollisionRetries; attempt++)
            {
                var outcome = await _userRepository.AddAsync(user, ct);
                if (outcome == UserInsertOutcome.Added)
                {
                    return RegisterResult.Succeeded(user.Id, user.Email, password);
                }

                // D1: increment the suffix locally on the next attempt — no re-query of FindEmailsByPrefixAsync.
                suffix++;
                user.Email = CandidateEmail(localBase, suffix, domain);
            }

            return RegisterResult.Failed(RegisterFailure.EmailAlreadyRegistered);
        }

        private static string CandidateEmail(string localBase, int suffix, string domain) =>
            suffix == 0 ? $"{localBase}@{domain}" : $"{localBase}{suffix}@{domain}";
    }
}
