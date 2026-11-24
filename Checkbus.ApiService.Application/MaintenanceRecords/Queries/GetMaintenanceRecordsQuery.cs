using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.MaintenanceRecords.Queries
{
    public class GetMaintenanceRecordsQuery : IRequest<IReadOnlyList<MaintenanceRecordDto>>
    {

        public Guid? VehicleId { get; set; }
        public MaintenanceStatus? Status { get; set; }
        public MaintenanceType? Type { get; set; }
    }
}
