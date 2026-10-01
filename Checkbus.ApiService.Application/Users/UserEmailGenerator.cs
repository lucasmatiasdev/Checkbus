using System.Globalization;
using System.Text;

namespace Checkbus.ApiService.Application.Users
{
    /// <summary>
    /// Derives a deterministic corporate login identifier from a person's name and their
    /// organization's slug. PURE: no I/O, no clock, no culture dependence. Collision state is
    /// supplied by the caller as <paramref name="takenEmails"/> (see <see cref="Generate"/>), so
    /// the whole unit is testable with no database.
    /// </summary>
    public static class UserEmailGenerator
    {
        public const int MaxLocalPartLength = 64; // RFC 5321 local-part limit
        public const int MaxBaseLocalPartLength = 62; // reserves 2 chars for a 1..99 suffix
        public const int MaxSlugLength = 63; // max DNS label length
        public const int MaxAddressLength = 254; // matches LoginCommandValidator
        public const int MaxDisambiguationAttempts = 100; // unsuffixed + suffixes 1..99

        /// <summary>
        /// Normalizes a name or surname fragment: strips diacritics via NFD decomposition,
        /// lowercases using <see cref="CultureInfo.InvariantCulture"/> (never the ambient
        /// culture), then keeps only [a-z0-9].
        /// </summary>
        public static string NormalizePart(string value)
        {
            var builder = new StringBuilder(value.Length);

            foreach (var c in value.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                var lower = char.ToLowerInvariant(c);
                if ((lower >= 'a' && lower <= 'z') || (lower >= '0' && lower <= '9'))
                {
                    builder.Append(lower);
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Normalizes an organization slug for use as an email domain label: same stripping and
        /// invariant lowercasing as <see cref="NormalizePart"/>, but preserves '-', collapses
        /// runs of '-', trims leading/trailing '-', and truncates to the max DNS label length.
        /// </summary>
        public static string SanitizeSlug(string slug)
        {
            var builder = new StringBuilder(slug.Length);
            var previousWasHyphen = false;

            foreach (var c in slug.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                var lower = char.ToLowerInvariant(c);
                var isAllowed = (lower >= 'a' && lower <= 'z') || (lower >= '0' && lower <= '9') || lower == '-';
                if (!isAllowed)
                {
                    continue;
                }

                if (lower == '-')
                {
                    if (previousWasHyphen)
                    {
                        continue;
                    }

                    previousWasHyphen = true;
                }
                else
                {
                    previousWasHyphen = false;
                }

                builder.Append(lower);
            }

            var sanitized = builder.ToString().Trim('-');

            return sanitized.Length > MaxSlugLength ? sanitized[..MaxSlugLength] : sanitized;
        }

        /// <summary>
        /// Generates a deterministic <c>{name}.{surname}@{slug}.com</c> address, disambiguating
        /// against <paramref name="takenEmails"/> by appending the lowest free numeric suffix.
        /// The caller is responsible for fetching <paramref name="takenEmails"/> (a narrow
        /// projection over addresses sharing this local-part prefix) — this method performs no
        /// I/O of its own.
        /// </summary>
        public static UserEmailGenerationResult Generate(
            string name,
            string surname,
            string organizationSlug,
            IReadOnlySet<string> takenEmails)
        {
            var normalizedName = NormalizePart(name);
            var normalizedSurname = NormalizePart(surname);
            if (normalizedName.Length == 0 || normalizedSurname.Length == 0)
            {
                return UserEmailGenerationResult.Failure(UserEmailGenerationError.EmptyLocalPart);
            }

            var sanitizedSlug = SanitizeSlug(organizationSlug);
            if (sanitizedSlug.Length == 0)
            {
                return UserEmailGenerationResult.Failure(UserEmailGenerationError.EmptyDomain);
            }

            var basePart = normalizedName + "." + normalizedSurname;
            if (basePart.Length > MaxBaseLocalPartLength)
            {
                basePart = basePart[..MaxBaseLocalPartLength];
            }

            var domain = sanitizedSlug + ".com";

            for (var suffix = 0; suffix < MaxDisambiguationAttempts; suffix++)
            {
                var candidate = suffix == 0
                    ? $"{basePart}@{domain}"
                    : $"{basePart}{suffix.ToString(CultureInfo.InvariantCulture)}@{domain}";

                if (!takenEmails.Contains(candidate))
                {
                    return UserEmailGenerationResult.Success(candidate);
                }
            }

            return UserEmailGenerationResult.Failure(UserEmailGenerationError.DisambiguationExhausted);
        }
    }
}
