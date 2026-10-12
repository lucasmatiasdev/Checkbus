using Checkbus.Web.Features.Boarding.Contracts;

namespace Checkbus.Web.Features.Boarding.Models;

/// <summary>
/// Pure, plain-code filter predicate for the boarding screen's search box — same
/// "pushed out of the Razor component so it's unit-testable without bUnit" rationale as
/// <c>UserListFilter</c> (<c>Usuarios.razor</c>'s preloaded-list client-side filter). Extended
/// with a <see cref="BoardingPassengerResponse.PassengerDocumentNumber"/> match: unlike
/// <c>User</c>, the passengers shown here DO carry a document number, so this is a genuinely
/// wider predicate, not a copy-paste of <c>UserListFilter</c>.
/// </summary>
public static class BoardingSearchFilter
{
    /// <summary>
    /// Filters <paramref name="passengers"/> by a case-insensitive, trimmed substring match
    /// against Name, Surname, or DocumentNumber (null/empty/whitespace
    /// <paramref name="searchText"/> means "no filtering" — the full preloaded list is
    /// returned).
    /// </summary>
    public static IReadOnlyList<BoardingPassengerResponse> Apply(
        IReadOnlyList<BoardingPassengerResponse> passengers, string? searchText)
    {
        var trimmedSearch = searchText?.Trim();
        if (string.IsNullOrEmpty(trimmedSearch))
        {
            return passengers;
        }

        return passengers
            .Where(p =>
                p.PassengerName.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase) ||
                p.PassengerSurname.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase) ||
                p.PassengerDocumentNumber.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }
}
