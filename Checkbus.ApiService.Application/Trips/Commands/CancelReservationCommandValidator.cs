using FluentValidation;

namespace Checkbus.ApiService.Application.Trips.Commands
{
    public sealed class CancelReservationCommandValidator : AbstractValidator<CancelReservationCommand>
    {
        public CancelReservationCommandValidator()
        {
            RuleFor(x => x.ReservationId).NotEmpty().WithMessage("ReservationId is required.");
        }
    }
}
