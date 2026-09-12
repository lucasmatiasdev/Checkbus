namespace Checkbus.Application.UseCases.Authentication.Register
{
    public sealed record RegisterRequest(int OrganizationId, int ProfileId, string FullName, string DocumentNumber);
}
