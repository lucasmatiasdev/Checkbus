using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.DriverRequirements.Queries
{
    public class GetDriverRequirementDocumentQuery : IRequest<DriverRequirementFileDto>
    {
        public required Guid TargetUserId { get; set; }
        public required DriverRequirementType Type { get; set; }
    }
}
