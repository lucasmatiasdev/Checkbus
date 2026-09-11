using Checkbus.Application.Abstractions;
using Checkbus.Domain.Entities.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Checkbus.Infrastructure.Security
{
    public class IdentityPasswordHasher : IPasswordHasher
    {
        private readonly PasswordHasher<User> _inner = new();

        public string Hash(string password) => _inner.HashPassword(null!, password);

        public bool Verify(string passwordHash, string providedPassword) =>
            _inner.VerifyHashedPassword(null!, passwordHash, providedPassword) != PasswordVerificationResult.Failed;
    }
}
