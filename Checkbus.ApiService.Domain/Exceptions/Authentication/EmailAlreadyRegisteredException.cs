using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Domain.Exceptions.Authentication
{
    public class EmailAlreadyRegisteredException : Exception
    {
        public EmailAlreadyRegisteredException(string message = "Email is already registered.") : base(message) { }
    }
}
