using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.MaintenanceRecords.Commands
{
    public class UpdateMaintenanceRecordCommand : IRequest
    {
        public required Guid Id { get; set; }
        public required MaintenanceStatus Status { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? MechanicNotes { get; set; }
        public decimal? Cost { get; set; }
    }
}
