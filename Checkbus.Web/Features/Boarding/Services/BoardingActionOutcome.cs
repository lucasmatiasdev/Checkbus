namespace Checkbus.Web.Features.Boarding.Services;

/// <summary>
/// Closed outcome shared by <c>POST /api/boarding/validate</c> and
/// <c>POST /api/boarding/{tripId}/close</c> — both return <c>204 No Content</c> on success and
/// carry no response body worth modeling differently.
/// </summary>
public abstract record BoardingActionOutcome
{
    private BoardingActionOutcome()
    {
    }

    /// <summary>204 No Content — the action was applied successfully.</summary>
    public sealed record Success : BoardingActionOutcome;

    /// <summary>401 Unauthorized or 403 Forbidden — no reliable body to parse for either.</summary>
    public sealed record Forbidden : BoardingActionOutcome;

    /// <summary>
    /// 404 Not Found — the reservation/code doesn't resolve, or the Trip doesn't belong to the
    /// caller (IDOR-safe: both cases are indistinguishable to the caller).
    /// </summary>
    public sealed record NotFound : BoardingActionOutcome;

    /// <summary>
    /// 409 Conflict — the BoardingRecord was already resolved (already Abordado or Ausente).
    /// </summary>
    public sealed record Conflict(string Message) : BoardingActionOutcome;

    /// <summary>400 Bad Request — FluentValidation rejected the request.</summary>
    public sealed record ValidationFailed(IReadOnlyDictionary<string, string[]> Errors) : BoardingActionOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : BoardingActionOutcome;
}
