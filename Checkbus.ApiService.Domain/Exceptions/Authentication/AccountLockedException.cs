using System;

namespace Checkbus.ApiService.Domain.Exceptions.Authentication
{
    public class AccountLockedException : Exception
    {
        public DateTime LockedUntil { get; }

        public AccountLockedException(DateTime lockedUntil, string message = "Account is temporarily locked.")
            : base(message)
        {
            LockedUntil = lockedUntil;
        }
    }
}
