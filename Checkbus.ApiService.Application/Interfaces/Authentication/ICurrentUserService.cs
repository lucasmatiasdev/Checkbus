using System;

namespace Checkbus.ApiService.Application.Interfaces.Authentication
{
    public interface ICurrentUserService
    {
        bool IsAuthenticated { get; }
        Guid? UserId { get; }
        Guid? OrganizationId { get; }
        string? Role { get; }
        string? Username { get; }
    }
}
