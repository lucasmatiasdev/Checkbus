using FluentValidation;

namespace Checkbus.ApiService.Application.Events.Commands
{
    public sealed class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
    {
        public CreateEventCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.Address).NotEmpty().WithMessage("Address is required.");
            RuleFor(x => x.PlaceId).NotEmpty().WithMessage("PlaceId is required.");
            RuleFor(x => x.Type).IsInEnum();

            RuleFor(x => x.Date)
                .NotEqual(default(DateTime))
                .WithMessage("Date is required.");
        }
    }
}
