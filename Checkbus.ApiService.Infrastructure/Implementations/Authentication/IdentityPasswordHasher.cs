using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Infrastructure.Implementations.Authentication
{
    public class IdentityPasswordHasher : IPasswordHasher
    {
        PasswordHasher<User> _hasher = new PasswordHasher<User>();
        public string Hash(User user, string password)
        {
            return _hasher.HashPassword(user, password);
        }

        public bool Verify(string hash, string password)
        {
            try
            {
                var result = _hasher.VerifyHashedPassword(null!, hash, password);
                return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
