using System;

namespace Checkbus.ApiService.Domain.Exceptions.Maintenance
{
    public class MaintenanceRecordNotFoundException : Exception
    {
        public MaintenanceRecordNotFoundException(string message = "Maintenance record not found.") : base(message) { }
    }
}
