namespace Checkbus.ApiService.Domain.Exceptions.Vehicles
{
    public class PatentAlreadyRegisteredException : Exception
    {
        public PatentAlreadyRegisteredException(string message = "A vehicle with this patent is already registered.") : base(message) { }
    }
}
