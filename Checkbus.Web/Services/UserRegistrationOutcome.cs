using Checkbus.Web.Contracts;

namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>POST /api/Users</c> call, covering every realistic server response so
/// callers (the admin registration Razor page) can pattern-match on the result without inspecting
/// HTTP status codes directly. The private constructor confines the hierarchy to the nested cases
/// declared here; a consumer can exhaustively switch over <see cref="Success"/>,
/// <see cref="ValidationFailed"/>, <see cref="Conflict"/>, <see cref="Forbidden"/>, and
/// <see cref="TransportError"/>.
/// </summary>
public abstract record UserRegistrationOutcome
{
    private UserRegistrationOutcome()
    {
    }

    /// <summary>201 Created — the user was registered successfully.</summary>
    public sealed record Success(RegisterUserResult Result) : UserRegistrationOutcome;

    /// <summary>400 Bad Request — FluentValidation failures keyed by property name.</summary>
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : UserRegistrationOutcome;

    /// <summary>409 Conflict — e.g. a duplicate DocumentNumber. Message is the problem's Detail.</summary>
    public sealed record Conflict(string Message) : UserRegistrationOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : UserRegistrationOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : UserRegistrationOutcome;
}
