namespace Checkbus.ApiService.Domain.Exceptions.VehicleDocuments
{
    public class VehicleDocumentNumberAlreadyRegisteredException : Exception
    {
        public VehicleDocumentNumberAlreadyRegisteredException(
            string message = "A document with this number is already registered for another vehicle.") : base(message) { }
    }
}
