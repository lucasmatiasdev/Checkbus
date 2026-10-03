using Checkbus.ApiService.Domain.Enums;
using FluentValidation;

namespace Checkbus.ApiService.Application.MaintenanceRecords.Commands
{
    public sealed class UpdateMaintenanceRecordCommandValidator : AbstractValidator<UpdateMaintenanceRecordCommand>
    {
        public UpdateMaintenanceRecordCommandValidator()
        {
            RuleFor(x => x.Status).IsInEnum();

            // EnProceso implies the service visit has actually started — conditional
            // validation style mirrors CreateVehicleCommandValidator's OwnerType branches.
            When(x => x.Status == MaintenanceStatus.EnProceso, () =>
            {
                RuleFor(x => x.StartDate)
                    .NotNull()
                    .WithMessage("StartDate is required when Status is EnProceso.");
            });

            When(x => x.StartDate.HasValue && x.EndDate.HasValue, () =>
            {
                RuleFor(x => x.EndDate)
                    .GreaterThanOrEqualTo(x => x.StartDate!.Value)
                    .WithMessage("EndDate must be on or after StartDate.");
            });
        }
    }
}
