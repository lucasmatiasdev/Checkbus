using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.Audit.Queries
{

    public class GetAuditLogQuery : IRequest<IReadOnlyList<AuditLogDto>>
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public AuditAction? Action { get; set; }
        public Guid? ActorUserId { get; set; }
        public Guid? OrganizationId { get; set; }
    }
}
