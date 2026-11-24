using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Domain.Exceptions.DriverRequirements
{

    public class DriverRequirementNotFoundException : Exception
    {
        public DriverRequirementNotFoundException(string message = "Driver requirement not found.") : base(message) { }
    }
}
