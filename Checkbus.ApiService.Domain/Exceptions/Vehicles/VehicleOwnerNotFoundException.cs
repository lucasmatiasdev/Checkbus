namespace Checkbus.ApiService.Domain.Exceptions.Vehicles
{
    // Deliberately generic: thrown whether OwnerUserId does not exist at all, exists but is
    // not Role.Chofer, or belongs to a different organization, so the response never leaks
    // cross-tenant existence (IDOR-safe — all three cases look identical to the caller),
    // mirroring DriverRequirementNotFoundException's precedent.
    public class VehicleOwnerNotFoundException : Exception
    {
        public VehicleOwnerNotFoundException(string message = "Vehicle owner user not found.") : base(message) { }
    }
}
