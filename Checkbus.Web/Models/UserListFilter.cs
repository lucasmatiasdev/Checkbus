using Checkbus.Web.Contracts;

namespace Checkbus.Web.Models;

/// <summary>
/// Pure, plain-code filter predicate for the Usuarios list page (<c>Usuarios.razor</c>). Pushed
/// out of the Razor component so it is unit-testable without bUnit (this repo has none) — the
/// page itself calls <see cref="Apply"/> on every role/search-text change to recompute the
/// <c>MudDataGrid</c>'s <c>Items</c> client-side; there is no server round-trip.
/// </summary>
public static class UserListFilter
{
    /// <summary>
    /// Filters <paramref name="users"/> by role (exact match, <see langword="null"/> means "all
    /// roles") and by a case-insensitive, trimmed substring match against either
    /// <see cref="UserListItemResponse.Name"/> or <see cref="UserListItemResponse.Surname"/>
    /// (null/empty/whitespace <paramref name="searchText"/> means "no text filtering"). Both
    /// filters combine with AND when both are set.
    /// </summary>
    public static IReadOnlyList<UserListItemResponse> Apply(
        IReadOnlyList<UserListItemResponse> users, WebRole? role, string? searchText)
    {
        IEnumerable<UserListItemResponse> result = users;

        if (role is not null)
        {
            result = result.Where(user => user.Role == role.Value);
        }

        var trimmedSearch = searchText?.Trim();
        if (!string.IsNullOrEmpty(trimmedSearch))
        {
            result = result.Where(user =>
                user.Name.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase) ||
                user.Surname.Contains(trimmedSearch, StringComparison.OrdinalIgnoreCase));
        }

        return result.ToArray();
    }
}
