using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Domain.Exceptions.DriverRequirements
{
    public class DriverRequirementAccessDeniedException : Exception
    {
        public DriverRequirementAccessDeniedException(string message = "You are not allowed to perform this action on this driver requirement.") : base(message) { }
    }
}
