using Checkbus.ApiService.Domain.Enums;
using MediatR;

namespace Checkbus.ApiService.Application.DriverRequirements.Commands
{
    public class ValidateDriverRequirementCommand : IRequest
    {
        public required Guid TargetUserId { get; set; }
        public required DriverRequirementType Type { get; set; }
        public required bool Approved { get; set; }
    }
}
