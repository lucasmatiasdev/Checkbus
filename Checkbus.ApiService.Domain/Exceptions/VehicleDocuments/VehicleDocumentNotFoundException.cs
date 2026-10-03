using System;

namespace Checkbus.ApiService.Domain.Exceptions.VehicleDocuments
{
    // Distinct from VehicleNotFoundException: thrown when the vehicle itself exists and is
    // owned by the caller's organization, but the requested document Type has never been
    // submitted/created for it (e.g. validating a conditional type before its first upload).
    public class VehicleDocumentNotFoundException : Exception
    {
        public VehicleDocumentNotFoundException(string message = "Vehicle document not found.") : base(message) { }
    }
}
