using FluentValidation;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // Structural rules only. The eligibility checks (vehicle/driver status, documentation,
    // date-range overlap) need DB lookups and don't fit FluentValidation's synchronous-rule
    // convention used elsewhere in this codebase — they are thrown as domain exceptions from
    // CreateTripCommandHandler instead (odd/tasks/rutas-publicacion.md Scope).
    public sealed class CreateTripCommandValidator : AbstractValidator<CreateTripCommand>
    {
        public CreateTripCommandValidator()
        {
            RuleFor(x => x.VehicleId).NotEmpty().WithMessage("VehicleId is required.");
            RuleFor(x => x.DriverId).NotEmpty().WithMessage("DriverId is required.");
            RuleFor(x => x.EventId).NotEmpty().WithMessage("EventId is required.");

            RuleFor(x => x.ArrivalDate)
                .GreaterThan(x => x.DepartureDate)
                .WithMessage("ArrivalDate must be after DepartureDate.");

            RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than 0.");

            // At least an origin and a destination.
            RuleFor(x => x.Stops)
                .Must(stops => stops is { Count: >= 2 })
                .WithMessage("At least 2 stops (origin and destination) are required.");
        }
    }
}
