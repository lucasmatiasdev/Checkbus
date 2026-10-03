using FluentValidation;

namespace Checkbus.ApiService.Application.VehicleDiagnostics.Commands
{
    public sealed class CreateVehicleDiagnosticCommandValidator : AbstractValidator<CreateVehicleDiagnosticCommand>
    {
        public CreateVehicleDiagnosticCommandValidator()
        {
            RuleFor(x => x.MaintenanceRecordId)
                .NotEqual(Guid.Empty)
                .WithMessage("MaintenanceRecordId is required.");

            RuleFor(x => x.DiagnosedAt)
                .NotEqual(default(DateOnly))
                .WithMessage("DiagnosedAt is required.");

            RuleFor(x => x.Components)
                .NotEmpty()
                .WithMessage("At least one component diagnostic entry is required.");

            RuleForEach(x => x.Components).ChildRules(component =>
            {
                component.RuleFor(c => c.Component).IsInEnum();
                component.RuleFor(c => c.Condition).IsInEnum();
            });
        }
    }
}
