using System.Globalization;
using System.Text;

namespace Checkbus.Application.UseCases.Authentication.Register
{
    /// <summary>
    /// Pure, IO-free slug generator for the system-generated internal email identifier (D2/D3).
    /// Strips diacritics via Unicode decomposition, treats any non-alphanumeric character as a
    /// token break, joins the resulting tokens with <paramref name="separator"/>, and truncates
    /// the joined result to <paramref name="maxLength"/>.
    /// </summary>
    public static class EmailIdentifierGenerator
    {
        public static string Slug(string value, string separator, int maxLength)
        {
            var tokens = Tokenize(value);
            var joined = string.Join(separator, tokens);

            return joined.Length > maxLength ? joined[..maxLength] : joined;
        }

        private static IEnumerable<string> Tokenize(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                sb.Append(char.IsAsciiLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
            }

            return sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
