using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{

    public class ResolveTripSlugQuery : IRequest<Guid?>
    {
        public required string Slug { get; set; }
    }
}
