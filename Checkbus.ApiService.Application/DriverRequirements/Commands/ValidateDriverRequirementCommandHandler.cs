using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.DriverRequirements;
using MediatR;

namespace Checkbus.ApiService.Application.DriverRequirements.Commands
{
    public class ValidateDriverRequirementCommandHandler : IRequestHandler<ValidateDriverRequirementCommand>
    {
        private readonly IDriverRequirementRepository _driverRequirementRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUserService _currentUser;

        public ValidateDriverRequirementCommandHandler(
            IDriverRequirementRepository driverRequirementRepository,
            IUserRepository userRepository,
            ICurrentUserService currentUser)
        {
            _driverRequirementRepository = driverRequirementRepository;
            _userRepository = userRepository;
            _currentUser = currentUser;
        }

        public async Task Handle(ValidateDriverRequirementCommand request, CancellationToken cancellationToken)
        {
            // Validate is Administrador-only, not self-service at all — not even the
            // Chofer who owns the row may approve/reject their own document.
            if (_currentUser.Role != nameof(Role.Administrador))
            {
                throw new DriverRequirementAccessDeniedException();
            }

            var callerOrganizationId = _currentUser.OrganizationId
                ?? throw new InvalidOperationException("Authenticated caller has no OrganizationId claim.");

            var target = await _userRepository.FindByIdAsync(request.TargetUserId, cancellationToken);
            if (target is null || target.OrganizationId != callerOrganizationId)
            {
                // IDOR-safe: cross-tenant existence is never leaked to the caller.
                throw new DriverRequirementNotFoundException();
            }

            var requirements = await _driverRequirementRepository.GetByUserIdAsync(request.TargetUserId, cancellationToken);
            var requirement = requirements.FirstOrDefault(r => r.Type == request.Type)
                ?? throw new InvalidOperationException(
                    $"DriverRequirement row for user {request.TargetUserId} and type {request.Type} is missing. " +
                    "This row must have been auto-created at Chofer registration time.");

            // No requirement that DocumentPresent be true first — an admin can mark
            // NoApto on an empty row too (e.g. "nothing was ever submitted").
            requirement.Status = request.Approved ? DriverRequirementStatus.Apto : DriverRequirementStatus.NoApto;
            requirement.UpdatedAt = DateTime.UtcNow;

            await _driverRequirementRepository.UpdateAsync(requirement, cancellationToken);
        }
    }
}
