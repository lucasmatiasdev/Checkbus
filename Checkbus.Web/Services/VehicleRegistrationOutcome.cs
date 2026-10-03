using Checkbus.Web.Contracts;

namespace Checkbus.Web.Services;

/// <summary>
/// Closed outcome of a <c>POST /api/Vehicles</c> call, covering every realistic server response so
/// callers (the admin "Nuevo Vehículo" Razor page) can pattern-match on the result without
/// inspecting HTTP status codes directly. Mirrors <see cref="UserRegistrationOutcome"/>'s exact
/// shape — vehicle registration has the same create-with-possible-conflict shape as user
/// registration (a duplicate <c>Patent</c> conflicts the same way a duplicate <c>DocumentNumber</c>
/// does for users). The private constructor confines the hierarchy to the nested cases declared
/// here; a consumer can exhaustively switch over <see cref="Success"/>,
/// <see cref="ValidationFailed"/>, <see cref="Conflict"/>, <see cref="Forbidden"/>, and
/// <see cref="TransportError"/>.
/// </summary>
public abstract record VehicleRegistrationOutcome
{
    private VehicleRegistrationOutcome()
    {
    }

    /// <summary>201 Created — the vehicle was registered successfully.</summary>
    public sealed record Success(VehicleRegistrationResult Result) : VehicleRegistrationOutcome;

    /// <summary>400 Bad Request — FluentValidation failures keyed by property name.</summary>
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : VehicleRegistrationOutcome;

    /// <summary>409 Conflict — a duplicate <c>Patent</c>. Message is the problem's Detail.</summary>
    public sealed record Conflict(string Message) : VehicleRegistrationOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : VehicleRegistrationOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : VehicleRegistrationOutcome;
}
