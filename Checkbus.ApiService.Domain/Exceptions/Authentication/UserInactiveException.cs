using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Domain.Exceptions.Authentication
{
    public class UserInactiveException : Exception
    {
        public UserInactiveException(string message = "User account is inactive.") : base(message) { }
    }
}
