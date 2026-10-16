using Checkbus.ApiService.Application.Interfaces.Repositories;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    public class ResolveTripSlugQueryHandler : IRequestHandler<ResolveTripSlugQuery, Guid?>
    {
        private readonly ITripRepository _tripRepository;

        public ResolveTripSlugQueryHandler(ITripRepository tripRepository)
        {
            _tripRepository = tripRepository;
        }

        public Task<Guid?> Handle(ResolveTripSlugQuery request, CancellationToken cancellationToken)
        {
            return _tripRepository.GetIdBySlugAsync(request.Slug, cancellationToken);
        }
    }
}
