using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Domain.Exceptions.Authentication
{
    public class OrganizationInactiveException : Exception
    {
        public OrganizationInactiveException(string message = "Organization is inactive.") : base(message) { }
    }
}
