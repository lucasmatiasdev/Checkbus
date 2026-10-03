using Checkbus.ApiService.Domain.Enums;
using FluentValidation;

namespace Checkbus.ApiService.Application.Vehicles.Commands
{
    public sealed class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
    {
        private const int MinYear = 1980;

        public CreateVehicleCommandValidator()
        {
            RuleFor(x => x.Brand).NotEmpty().WithMessage("Brand is required.");
            RuleFor(x => x.Model).NotEmpty().WithMessage("Model is required.");
            RuleFor(x => x.Patent).NotEmpty().WithMessage("Patent is required.");

            RuleFor(x => x.Year)
                .InclusiveBetween(MinYear, DateTime.UtcNow.Year + 1)
                .WithMessage($"Year must be between {MinYear} and next year.");

            RuleFor(x => x.Capacity).GreaterThanOrEqualTo(0).WithMessage("Capacity cannot be negative.");
            RuleFor(x => x.Mileage).GreaterThanOrEqualTo(0).WithMessage("Mileage cannot be negative.");

            RuleFor(x => x.Status).IsInEnum();
            RuleFor(x => x.OwnerType).IsInEnum();

            // OwnerType=Organizacion: no owner fields accepted — ownership stays unset,
            // never implied from the operational OrganizationId link (see
            // notes/Politica_documentacion_vehiculos.docx, constraint 1).
            When(x => x.OwnerType == VehicleOwnerType.Organizacion, () =>
            {
                RuleFor(x => x.OwnerUserId)
                    .Must(id => id is null)
                    .WithMessage("OwnerUserId must not be set when OwnerType is Organizacion.");
                RuleFor(x => x.OwnerName)
                    .Must(string.IsNullOrEmpty)
                    .WithMessage("OwnerName must not be set when OwnerType is Organizacion.");
                RuleFor(x => x.OwnerDocumentNumber)
                    .Must(string.IsNullOrEmpty)
                    .WithMessage("OwnerDocumentNumber must not be set when OwnerType is Organizacion.");
            });

            // OwnerType=Chofer: OwnerUserId required (resolved against an existing
            // Role.Chofer user in the caller's organization by the handler);
            // OwnerName/OwnerDocumentNumber must stay unset.
            When(x => x.OwnerType == VehicleOwnerType.Chofer, () =>
            {
                RuleFor(x => x.OwnerUserId)
                    .NotNull()
                    .WithMessage("OwnerUserId is required when OwnerType is Chofer.");
                RuleFor(x => x.OwnerName)
                    .Must(string.IsNullOrEmpty)
                    .WithMessage("OwnerName must not be set when OwnerType is Chofer.");
                RuleFor(x => x.OwnerDocumentNumber)
                    .Must(string.IsNullOrEmpty)
                    .WithMessage("OwnerDocumentNumber must not be set when OwnerType is Chofer.");
            });

            // OwnerType=Otro: OwnerName + OwnerDocumentNumber both required;
            // OwnerUserId must stay unset.
            When(x => x.OwnerType == VehicleOwnerType.Otro, () =>
            {
                RuleFor(x => x.OwnerName)
                    .NotEmpty()
                    .WithMessage("OwnerName is required when OwnerType is Otro.");
                RuleFor(x => x.OwnerDocumentNumber)
                    .NotEmpty()
                    .WithMessage("OwnerDocumentNumber is required when OwnerType is Otro.");
                RuleFor(x => x.OwnerUserId)
                    .Must(id => id is null)
                    .WithMessage("OwnerUserId must not be set when OwnerType is Otro.");
            });
        }
    }
}
