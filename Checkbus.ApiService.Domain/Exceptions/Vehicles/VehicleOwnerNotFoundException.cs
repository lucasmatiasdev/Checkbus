namespace Checkbus.ApiService.Domain.Exceptions.Vehicles
{

    public class VehicleOwnerNotFoundException : Exception
    {
        public VehicleOwnerNotFoundException(string message = "Vehicle owner user not found.") : base(message) { }
    }
}
