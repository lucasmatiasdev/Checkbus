namespace Checkbus.Web.Features.PublicReservations.Services;

/// <summary>
/// Closed outcome of a <c>GET /api/public/trips/{id}/event-image</c> call
/// (odd/tasks/detalle-reserva-publica.md T4). Mirrors <see cref="PublicTripDetailOutcome"/>'s
/// shape — no Forbidden case, this endpoint is anonymous. Unlike every other outcome on this
/// surface, <see cref="Success"/> carries raw bytes rather than a deserialized JSON DTO: this
/// endpoint returns a binary image stream, not JSON.
/// </summary>
public abstract record PublicTripEventImageOutcome
{
    private PublicTripEventImageOutcome()
    {
    }

    /// <summary>200 OK — the image bytes were fetched successfully.</summary>
    public sealed record Success(byte[] Content, string ContentType) : PublicTripEventImageOutcome;

    /// <summary>
    /// 404 Not Found — either the Trip/Event doesn't resolve, the Trip isn't Publicado, or the
    /// Event has no image. All indistinguishable by design (IDOR-safe, same rationale as every
    /// other public endpoint here).
    /// </summary>
    public sealed record NotFound : PublicTripEventImageOutcome;

    /// <summary>
    /// Any transport-level failure (connection error, client-side timeout, unexpected status
    /// code, etc). Message is short and user-presentable, never a raw exception detail.
    /// </summary>
    public sealed record TransportError(string Message) : PublicTripEventImageOutcome;
}
