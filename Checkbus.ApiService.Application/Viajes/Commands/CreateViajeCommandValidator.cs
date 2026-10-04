using FluentValidation;

namespace Checkbus.ApiService.Application.Viajes.Commands
{
    // Structural rules only. The habilitación checks (vehicle/chofer status, documentation,
    // date-range overlap) need DB lookups and don't fit FluentValidation's synchronous-rule
    // convention used elsewhere in this codebase — they are thrown as domain exceptions from
    // CreateViajeCommandHandler instead (odd/tasks/rutas-publicacion.md Scope).
    public sealed class CreateViajeCommandValidator : AbstractValidator<CreateViajeCommand>
    {
        public CreateViajeCommandValidator()
        {
            RuleFor(x => x.VehicleId).NotEmpty().WithMessage("VehicleId is required.");
            RuleFor(x => x.ChoferId).NotEmpty().WithMessage("ChoferId is required.");
            RuleFor(x => x.EventoId).NotEmpty().WithMessage("EventoId is required.");

            RuleFor(x => x.FechaLlegada)
                .GreaterThan(x => x.FechaSalida)
                .WithMessage("FechaLlegada must be after FechaSalida.");

            RuleFor(x => x.Precio).GreaterThan(0).WithMessage("Precio must be greater than 0.");

            // At least an origin and a destination.
            RuleFor(x => x.Stops)
                .Must(stops => stops is { Count: >= 2 })
                .WithMessage("At least 2 stops (origin and destination) are required.");
        }
    }
}
