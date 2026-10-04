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
    /// renaming/mapping.
    /// </summary>
    public record DirectionsResult(TimeSpan EstimatedDuration, decimal DistanceKm);
}
