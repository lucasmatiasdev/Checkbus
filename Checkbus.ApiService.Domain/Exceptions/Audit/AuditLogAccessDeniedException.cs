using System;

namespace Checkbus.ApiService.Domain.Exceptions.Audit
{

    public class AuditLogAccessDeniedException : Exception
    {
        public AuditLogAccessDeniedException(string message = "Only Administrador or Checkbus internal staff can perform this action.") : base(message) { }
    }
}
