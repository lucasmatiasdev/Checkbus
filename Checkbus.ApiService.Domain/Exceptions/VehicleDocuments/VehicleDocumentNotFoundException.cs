using System;

namespace Checkbus.ApiService.Domain.Exceptions.VehicleDocuments
{

    public class VehicleDocumentNotFoundException : Exception
    {
        public VehicleDocumentNotFoundException(string message = "Vehicle document not found.") : base(message) { }
    }
}
