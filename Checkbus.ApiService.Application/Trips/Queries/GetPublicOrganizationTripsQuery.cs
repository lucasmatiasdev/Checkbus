using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{

    public class GetPublicOrganizationTripsQuery : IRequest<IReadOnlyList<PublicOrganizationTripDto>>
    {
        public required string OrganizationSlug { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? EventName { get; set; }
    }
}
