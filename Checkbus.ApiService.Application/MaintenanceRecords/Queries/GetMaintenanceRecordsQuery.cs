using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.MaintenanceRecords.Queries
{
    public class GetMaintenanceRecordsQuery : IRequest<IReadOnlyList<MaintenanceRecordDto>>
    {
        // All optional — an absent filter just means "no restriction on this field" within
        // the caller's own organization vehicles (tenant scoping always comes from the
        // validated claim, never from a request value — see Handler).
        public Guid? VehicleId { get; set; }
        public MaintenanceStatus? Status { get; set; }
        public MaintenanceType? Type { get; set; }
    }
}
