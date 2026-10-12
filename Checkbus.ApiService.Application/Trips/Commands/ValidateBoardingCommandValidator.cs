using FluentValidation;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    public sealed class ValidateBoardingCommandValidator : AbstractValidator<ValidateBoardingCommand>
    {
        public ValidateBoardingCommandValidator()
        {
            RuleFor(x => x.ReservationId).NotEmpty().WithMessage("ReservationId is required.");
            RuleFor(x => x.Method).IsInEnum().WithMessage("Method must be a valid ValidationMethod value.");
        }
    }
}
