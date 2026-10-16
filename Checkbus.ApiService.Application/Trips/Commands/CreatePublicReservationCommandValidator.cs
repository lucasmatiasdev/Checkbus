using Checkbus.ApiService.Domain.Enums;
using FluentValidation;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // Structural rules only, same split CreateTripCommandValidator/PublishTripCommandValidator
    // use: everything synchronous lives here, everything that needs a DB lookup (Trip
    // not-found/not-published, a SeatId belonging to another vehicle, a seat already taken)
    // is thrown as a domain exception from CreatePublicReservationCommandHandler instead.
    // Duplicate-SeatId-within-the-same-batch IS a structural/synchronous rule (it never needs
    // the DB — the submitted list is self-contradictory on its own), so it belongs here.
    public sealed class CreatePublicReservationCommandValidator : AbstractValidator<CreatePublicReservationCommand>
    {
        public CreatePublicReservationCommandValidator()
        {
            RuleFor(x => x.TripId).NotEmpty().WithMessage("TripId is required.");

            RuleFor(x => x.Passengers)
                .NotEmpty()
                .WithMessage("At least one seat/passenger entry is required.");

            RuleFor(x => x.Passengers)
                .Must(passengers => passengers is null
                    || passengers.Select(p => p.SeatId).Distinct().Count() == passengers.Count)
                .WithMessage("The same seat cannot be reserved twice in the same request.");

            RuleForEach(x => x.Passengers).ChildRules(passenger =>
            {
                passenger.RuleFor(p => p.SeatId).NotEmpty().WithMessage("SeatId is required.");
                passenger.RuleFor(p => p.Name).NotEmpty().WithMessage("Name is required.");
                passenger.RuleFor(p => p.Surname).NotEmpty().WithMessage("Surname is required.");
                passenger.RuleFor(p => p.DocumentType).IsInEnum();
                passenger.RuleFor(p => p.DocumentNumber).NotEmpty().WithMessage("DocumentNumber is required.");
                // Same CU-06 rule, condicional a DocumentType.DNI — Pasaporte keeps only the
                // NotEmpty check above.
                passenger.RuleFor(p => p.DocumentNumber)
                    .Matches("^[0-9]{8}$").WithMessage("DNI must be exactly 8 digits.")
                    .When(p => p.DocumentType == DocumentType.DNI);
                passenger.RuleFor(p => p.Email)
                    .NotEmpty().WithMessage("Email is required.")
                    .EmailAddress().WithMessage("Email must be a valid email address.");
                passenger.RuleFor(p => p.Phone)
                    .NotEmpty().WithMessage("Phone is required.")
                    .Matches("^[0-9]{8,15}$").WithMessage("Phone must contain only digits (8 to 15).");
            });
        }
    }
}
