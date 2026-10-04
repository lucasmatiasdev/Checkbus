using FluentValidation;

namespace Checkbus.ApiService.Application.Eventos.Commands
{
    public sealed class CreateEventoCommandValidator : AbstractValidator<CreateEventoCommand>
    {
        public CreateEventoCommandValidator()
        {
            RuleFor(x => x.Nombre).NotEmpty().WithMessage("Nombre is required.");
            RuleFor(x => x.Direccion).NotEmpty().WithMessage("Direccion is required.");
            RuleFor(x => x.PlaceId).NotEmpty().WithMessage("PlaceId is required.");
            RuleFor(x => x.Tipo).IsInEnum();

            RuleFor(x => x.Fecha)
                .NotEqual(default(DateTime))
                .WithMessage("Fecha is required.");
        }
    }
}
