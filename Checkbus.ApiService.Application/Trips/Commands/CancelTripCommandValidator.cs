using Checkbus.ApiService.Domain.Enums;
using FluentValidation;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    public sealed class CancelTripCommandValidator : AbstractValidator<CancelTripCommand>
    {
        public CancelTripCommandValidator()
        {
            RuleFor(x => x.Reason).IsInEnum();

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required when Reason is Otro.")
                .When(x => x.Reason == TripCancellationReason.Otro);
        }
    }
}
