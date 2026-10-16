using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // All optional — an absent filter means "no restriction on this field" within the
    // caller's own organization's trips (tenant scoping always comes from the validated
    // claim, never from a request value — see Handler). Kept separate from GetTripsQuery
    // so the existing trips-listing screen's query stays untouched.
    public class GetTripsReportQuery : IRequest<TripsReportDto>
    {
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        public TripStatus? Status { get; set; }
        public Guid? DriverId { get; set; }
        public Guid? VehicleId { get; set; }
    }
}
