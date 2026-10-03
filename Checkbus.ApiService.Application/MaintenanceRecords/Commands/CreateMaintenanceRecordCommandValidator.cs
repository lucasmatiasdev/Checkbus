using FluentValidation;

namespace Checkbus.ApiService.Application.MaintenanceRecords.Commands
{
    public sealed class CreateMaintenanceRecordCommandValidator : AbstractValidator<CreateMaintenanceRecordCommand>
    {
        public CreateMaintenanceRecordCommandValidator()
        {
            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");

            RuleFor(x => x.ScheduledDate)
                .NotEqual(default(DateOnly))
                .WithMessage("ScheduledDate is required.");

            RuleFor(x => x.Type).IsInEnum();
        }
    }
}
