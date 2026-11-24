using FluentValidation;

namespace Checkbus.ApiService.Application.Trips.Commands
{

    public sealed class PublishTripCommandValidator : AbstractValidator<PublishTripCommand>
    {
        public PublishTripCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Id is required.");
        }
    }
}
