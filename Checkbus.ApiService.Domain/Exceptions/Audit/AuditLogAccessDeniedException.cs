using System;

namespace Checkbus.ApiService.Domain.Exceptions.Audit
{
    // Defense in depth for CU-16 (odd/tasks/auditoria-sistema.md): [Authorize(Roles =
    // "Administrador,CheckbusSoporte")] on the controller is not proof the CheckbusSoporte
    // caller actually belongs to the sentinel Checkbus Organization
    // (CheckbusInternalOrganization.Slug) - the Role claim alone is never trusted for that
    // cross-tenant visibility, same reasoning as SupportTicketAccessDeniedException.
    public class AuditLogAccessDeniedException : Exception
    {
        public AuditLogAccessDeniedException(string message = "Only Administrador or Checkbus internal staff can perform this action.") : base(message) { }
    }
}
