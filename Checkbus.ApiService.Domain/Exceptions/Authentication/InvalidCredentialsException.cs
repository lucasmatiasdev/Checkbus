using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Domain.Exceptions.Authentication
{
    public class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException(string message = "Invalid credentials provided.") : base(message){}
    }
}
