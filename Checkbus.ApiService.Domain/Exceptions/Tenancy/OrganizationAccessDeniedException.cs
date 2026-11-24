using System;

namespace Checkbus.ApiService.Domain.Exceptions.Tenancy
{

    public class OrganizationAccessDeniedException : Exception
    {
        public OrganizationAccessDeniedException(string message = "Only Checkbus internal staff can perform this action.") : base(message) { }
    }
}
