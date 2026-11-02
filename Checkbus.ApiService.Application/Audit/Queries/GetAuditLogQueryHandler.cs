using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Audit;
using Checkbus.ApiService.Domain.Exceptions.Audit;
using MediatR;

namespace Checkbus.ApiService.Application.Audit.Queries
{
    public class GetAuditLogQueryHandler : IRequestHandler<GetAuditLogQuery, IReadOnlyList<AuditLogDto>>
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ICurrentUserService _currentUser;

        public GetAuditLogQueryHandler(
            IAuditLogRepository auditLogRepository,
            IOrganizationRepository organizationRepository,
            ICurrentUserService currentUser)
        {
            _auditLogRepository = auditLogRepository;
            _organizationRepository = organizationRepository;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<AuditLogDto>> Handle(GetAuditLogQuery request, CancellationToken cancellationToken)
        {
            var organizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            IReadOnlyList<AuditLogEntry> entries;
            bool crossTenant;

            if (_currentUser.Role == nameof(Role.CheckbusSoporte))
            {
                // Role gate (Administrador,CheckbusSoporte) is enforced at the controller; this
                // handler additionally verifies the caller's own OrganizationId resolves to the
                // sentinel Checkbus Organization - defense in depth, never trust the Role claim
                // alone for this cross-tenant visibility (same pattern as
                // GetAllSupportTicketsQueryHandler).
                var slug = await _organizationRepository.GetSlugByIdAsync(organizationId, cancellationToken);
                if (slug != CheckbusInternalOrganization.Slug)
                {
                    throw new AuditLogAccessDeniedException();
                }

                entries = request.OrganizationId.HasValue
                    ? await _auditLogRepository.GetAllByOrganizationAsync(request.OrganizationId.Value, cancellationToken)
                    : await _auditLogRepository.GetAllAsync(cancellationToken);
                crossTenant = true;
            }
            else if (_currentUser.Role == nameof(Role.Administrador))
            {
                // Scope is always the caller's own organization - any OrganizationId filter in
                // the request is ignored for this role (CU-16 main flow).
                entries = await _auditLogRepository.GetAllByOrganizationAsync(organizationId, cancellationToken);
                crossTenant = false;
            }
            else
            {
                // Should never be reached past the controller's [Authorize(Roles = ...)] -
                // defense in depth, same criterion as the rest of the codebase.
                throw new AuditLogAccessDeniedException();
            }

            var filtered = entries
                .Where(e => request.DateFrom is null || e.CreatedAt >= request.DateFrom.Value)
                .Where(e => request.DateTo is null || e.CreatedAt <= request.DateTo.Value)
                .Where(e => request.Action is null || e.Action == request.Action)
                .Where(e => request.ActorUserId is null || e.ActorUserId == request.ActorUserId)
                .OrderByDescending(e => e.CreatedAt)
                .ToList();

            // Resolve each distinct OrganizationId's display Name once, instead of calling the
            // repository once per row - mirrors GetAllSupportTicketsQueryHandler.MapToDto. Only
            // needed when the scope is cross-tenant (CheckbusSoporte); an Administrador's rows
            // are all their own single organization, so OrganizationName stays null.
            var organizationNames = new Dictionary<Guid, string>();
            if (crossTenant)
            {
                foreach (var distinctOrganizationId in filtered
                    .Where(e => e.OrganizationId.HasValue)
                    .Select(e => e.OrganizationId!.Value)
                    .Distinct())
                {
                    var organizationName = await _organizationRepository.GetNameByIdAsync(distinctOrganizationId, cancellationToken);
                    organizationNames[distinctOrganizationId] = organizationName ?? "(desconocida)";
                }
            }

            return filtered
                .Select(entry => MapToDto(entry, crossTenant, organizationNames))
                .ToList();
        }

        private static AuditLogDto MapToDto(AuditLogEntry entry, bool crossTenant, Dictionary<Guid, string> organizationNames)
        {
            string? organizationName = null;
            if (crossTenant && entry.OrganizationId.HasValue)
            {
                organizationNames.TryGetValue(entry.OrganizationId.Value, out organizationName);
            }

            return new AuditLogDto
            {
                Id = entry.Id,
                CreatedAt = entry.CreatedAt,
                ActorEmail = entry.ActorEmail,
                Action = entry.Action,
                EntityType = entry.EntityType,
                EntityId = entry.EntityId,
                Description = entry.Description,
                OrganizationName = organizationName
            };
        }
    }
}
