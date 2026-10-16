using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Public, unauthenticated slug->TripId lookup for the friendly share link (CU-03). The
    // existing api/public/trips/{id:guid}/... actions are untouched — this only resolves the
    // Guid a caller then uses against those same actions.
    public class ResolveTripSlugQuery : IRequest<Guid?>
    {
        public required string Slug { get; set; }
    }
}
