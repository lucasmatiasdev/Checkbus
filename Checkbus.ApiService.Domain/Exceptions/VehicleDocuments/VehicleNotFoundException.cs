using System;

namespace Checkbus.ApiService.Domain.Exceptions.VehicleDocuments
{

    public class VehicleNotFoundException : Exception
    {
        public VehicleNotFoundException(string message = "Vehicle not found.") : base(message) { }
    }
}
