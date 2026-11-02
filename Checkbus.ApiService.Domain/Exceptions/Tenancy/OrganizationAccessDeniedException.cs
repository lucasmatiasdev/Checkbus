using System;

namespace Checkbus.ApiService.Domain.Exceptions.Tenancy
{
    // CU-16 (odd/tasks/auditoria-sistema.md T4) — defense in depth for the two cross-tenant reads
    // the audit screen needs (list organizations, list users of an arbitrary organization):
    // [Authorize(Roles = "CheckbusSoporte")] on the controller is not proof the caller actually
    // belongs to the sentinel Checkbus Organization, same reasoning as
    // SupportTicketAccessDeniedException/AuditLogAccessDeniedException.
    public class OrganizationAccessDeniedException : Exception
    {
        public OrganizationAccessDeniedException(string message = "Only Checkbus internal staff can perform this action.") : base(message) { }
    }
}
