using System;

namespace Checkbus.ApiService.Domain.Exceptions.Maintenance
{
    public class VehicleDiagnosticNotFoundException : Exception
    {
        public VehicleDiagnosticNotFoundException(string message = "Vehicle diagnostic not found.") : base(message) { }
    }
}
