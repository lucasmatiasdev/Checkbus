using Checkbus.Web.Features.Organizations.Contracts;

namespace Checkbus.Web.Features.Organizations.Services;

/// <summary>
/// Closed outcome of a <c>POST /api/Organizations</c> call, covering every realistic server
/// response so callers (the CheckbusSoporte organization-registration Razor page) can
/// pattern-match on the result without inspecting HTTP status codes directly. The private
/// constructor confines the hierarchy to the nested cases declared here; a consumer can
/// exhaustively switch over <see cref="Success"/>, <see cref="ValidationFailed"/>,
/// <see cref="Conflict"/>, <see cref="Forbidden"/>, and <see cref="TransportError"/>. Mirrors
/// <c>UserRegistrationOutcome</c> exactly — same five cases apply here too: <see cref="Conflict"/>
/// covers a duplicate CUIT (or a colliding slug, though that's disambiguated server-side instead
/// of rejected) the same way it covers a duplicate DocumentNumber for user registration.
/// </summary>
public abstract record OrganizationRegistrationOutcome
{
    private OrganizationRegistrationOutcome()
    {
    }

    /// <summary>201 Created — the organization and its first admin were registered successfully.</summary>
    public sealed record Success(RegisterOrganizationResult Result) : OrganizationRegistrationOutcome;

    /// <summary>400 Bad Request — FluentValidation failures keyed by property name.</summary>
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : OrganizationRegistrationOutcome;

    /// <summary>409 Conflict — e.g. a duplicate CUIT. Message is the problem's Detail.</summary>
    public sealed record Conflict(string Message) : OrganizationRegistrationOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : OrganizationRegistrationOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : OrganizationRegistrationOutcome;
}
