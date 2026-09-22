using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Domain.Exceptions.Authentication
{
    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string message = "User not found.") : base(message) { }
    }
}
