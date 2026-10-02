using Checkbus.Web.Contracts;

namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/Users</c> call, covering every realistic server response so
/// callers (the Usuarios list Razor page) can pattern-match on the result without inspecting HTTP
/// status codes directly. Unlike <see cref="UserRegistrationOutcome"/>, this endpoint has no
/// validation/conflict case — it is a plain read, so only success, an auth failure, and a
/// transport failure are realistic. The private constructor confines the hierarchy to the nested
/// cases declared here; a consumer can exhaustively switch over <see cref="Success"/>,
/// <see cref="Forbidden"/>, and <see cref="TransportError"/>.
/// </summary>
public abstract record UsersListOutcome
{
    private UsersListOutcome()
    {
    }

    /// <summary>200 OK — the organization's users were fetched successfully.</summary>
    public sealed record Success(IReadOnlyList<UserListItemResponse> Users) : UsersListOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : UsersListOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : UsersListOutcome;
}
