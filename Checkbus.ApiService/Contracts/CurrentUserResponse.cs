namespace Checkbus.ApiService.Contracts
{
    public sealed record CurrentUserResponse(Guid? UserId, Guid? OrganizationId, string? Role, string? Email);
}
