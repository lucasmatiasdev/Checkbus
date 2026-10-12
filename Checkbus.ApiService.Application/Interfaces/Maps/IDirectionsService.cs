namespace Checkbus.ApiService.Application.Interfaces.Maps
{
    /// <summary>
    /// Server-side port for computing route duration/distance from an ordered list of
    /// coordinates (origin, intermediate stops, destination). The authoritative call made once
    /// at <c>Trip</c> creation — never recalculated on read, never trusted from the client.
    /// </summary>
    public interface IDirectionsService
    {
        /// <summary>
        /// Computes the route duration/distance for the given ordered points.
        /// </summary>
        /// <param name="points">
        /// Ordered (Latitude, Longitude) points: origin first, then any intermediate stops, then
        /// the destination last. Must contain at least 2 points.
        /// </param>
        Task<DirectionsResult> GetDirectionsAsync(
            IReadOnlyList<(double Latitude, double Longitude)> points,
            CancellationToken cancellationToken);
    }

    /// <summary>
    /// Result of a Directions lookup. Field names intentionally mirror <c>Route</c>'s own fields
    /// (<c>EstimatedDuration</c>, <c>DistanceKm</c>) so callers can persist it directly with no
    /// renaming/mapping. <c>LegDurations</c> is ordered one entry per leg (i.e. <c>points.Count
    /// - 1</c> entries), <c>LegDurations[i]</c> being the travel time from <c>points[i]</c> to
    /// <c>points[i+1]</c> — reliable 1:1 with <c>Stop.Order</c> since no <c>optimizeWaypoints</c>
    /// is requested, so Google never reorders the given points. Used by
    /// <c>CreateTripCommandHandler</c> to compute a per-stop placeholder <c>DepartureTime</c>.
    /// </summary>
    public record DirectionsResult(
        TimeSpan EstimatedDuration,
        decimal DistanceKm,
        IReadOnlyList<TimeSpan> LegDurations);
}
