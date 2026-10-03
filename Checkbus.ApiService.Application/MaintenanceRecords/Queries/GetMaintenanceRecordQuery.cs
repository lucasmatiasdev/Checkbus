using MediatR;

namespace Checkbus.ApiService.Application.MaintenanceRecords.Queries
{
    public class GetMaintenanceRecordQuery : IRequest<MaintenanceRecordDto>
    {
        public required Guid Id { get; set; }
    }
}
