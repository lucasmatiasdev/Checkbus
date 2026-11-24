using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{

    public class GetTripsReportQuery : IRequest<TripsReportDto>
    {
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        public TripStatus? Status { get; set; }
        public Guid? DriverId { get; set; }
        public Guid? VehicleId { get; set; }
    }
}
