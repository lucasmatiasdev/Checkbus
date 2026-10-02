using MediatR;

namespace Checkbus.ApiService.Application.DriverRequirements.Queries
{
    public class GetDriverRequirementsQuery : IRequest<IReadOnlyList<DriverRequirementDto>>
    {
        public required Guid TargetUserId { get; set; }
    }
}
