namespace Checkbus.ApiService.Domain.Exceptions.Tenancy
{
    public class CuitAlreadyRegisteredException : Exception
    {
        public CuitAlreadyRegisteredException(string message = "An organization with this CUIT is already registered.") : base(message) { }
    }
}
