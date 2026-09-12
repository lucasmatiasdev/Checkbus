namespace Checkbus.Application.UseCases.Authentication.Register
{
    public enum RegisterFailure
    {
        InvalidFullName,
        InvalidDocumentNumber,
        OrganizationNotFound,
        ProfileNotFound,
        EmailAlreadyRegistered
    }
}
