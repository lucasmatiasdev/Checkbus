using Checkbus.ApiService.Domain.Entities.Authentication;

namespace Checkbus.ApiService.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<User?> FindByEmailAsync(string email);
    }
}
