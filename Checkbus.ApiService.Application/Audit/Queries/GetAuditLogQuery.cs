using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Audit.Queries
{
    // CU-16 (odd/tasks/auditoria-sistema.md) — all filters optional, same "absent or matches"
    // in-memory shape as GetTripsReportQuery. OrganizationId only has effect for a
    // CheckbusSoporte caller (cross-tenant scope); an Administrador's own tenant scoping always
    // comes from the validated claim, never from this request value — see Handler.
    public class GetAuditLogQuery : IRequest<IReadOnlyList<AuditLogDto>>
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public AuditAction? Action { get; set; }
        public Guid? ActorUserId { get; set; }
        public Guid? OrganizationId { get; set; }
    }
}
