using FluentValidation;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    // Structural rule only. The actual eligibility check (Trip.Status must currently be
    // Programado) needs a DB lookup and doesn't fit FluentValidation's synchronous-rule
    // convention used elsewhere in this codebase — it is thrown as a domain exception from
    // PublishTripCommandHandler instead, same split as CreateTripCommandValidator.
    public sealed class PublishTripCommandValidator : AbstractValidator<PublishTripCommand>
    {
        public PublishTripCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");
        }
    }
}
