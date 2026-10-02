using System;
using System.Collections.Generic;
using System.Text;

namespace Checkbus.ApiService.Domain.Exceptions.DriverRequirements
{
    // Deliberately generic: thrown both when the target user truly does not exist and
    // when it exists in another organization, so the response never leaks cross-tenant
    // existence (IDOR-safe — both cases look identical to the caller).
    public class DriverRequirementNotFoundException : Exception
    {
        public DriverRequirementNotFoundException(string message = "Driver requirement not found.") : base(message) { }
    }
}
