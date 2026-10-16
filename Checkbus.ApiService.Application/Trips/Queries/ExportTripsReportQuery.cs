using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Trips.Queries
{
    // Same optional filters as GetTripsReportQuery (forwarded to it verbatim via mediator —
    // see Handler) plus the requested output format.
    public class ExportTripsReportQuery : IRequest<ExportedReportDto>
    {
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        public TripStatus? Status { get; set; }
        public Guid? DriverId { get; set; }
        public Guid? VehicleId { get; set; }
        public required ReportExportFormat Format { get; set; }
    }
}
