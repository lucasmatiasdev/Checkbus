using Checkbus.ApiService.Application.Users;
using Checkbus.ApiService.Domain.Enums;
using FluentValidation;

namespace Checkbus.ApiService.Application.Tenancy.Commands
{
    public sealed class RegisterOrganizationCommandValidator : AbstractValidator<RegisterOrganizationCommand>
    {
        public RegisterOrganizationCommandValidator()
        {
            RuleFor(x => x.OrganizationName)
                .NotEmpty().WithMessage("OrganizationName is required.")
                .MaximumLength(200)
                .Must(n => UserEmailGenerator.SanitizeSlug(n).Length > 0)
                    .WithMessage("OrganizationName must contain at least one letter or digit usable in a URL slug.");

            // Format only: two digits, hyphen, eight digits, hyphen, one digit (e.g.
            // 20-12345678-9) — matches the format CheckbusDbSeeder already hardcodes for seed
            // CUITs. Deliberately NOT a real Argentine check-digit validation; out of scope.
            RuleFor(x => x.CUIT)
                .NotEmpty().WithMessage("CUIT is required.")
                .Matches(@"^\d{2}-\d{8}-\d$").WithMessage("CUIT must match the format NN-NNNNNNNN-N.");

            // Same name/surname/document-number rules RegisterUserCommandValidator already
            // applies to the equivalent fields.
            RuleFor(x => x.AdminName)
                .NotEmpty().WithMessage("AdminName is required.")
                .MaximumLength(100)
                .Must(n => UserEmailGenerator.NormalizePart(n).Length > 0)
                    .WithMessage("AdminName must contain at least one letter or digit usable in an email address.");

            RuleFor(x => x.AdminSurname)
                .NotEmpty().WithMessage("AdminSurname is required.")
                .MaximumLength(100)
                .Must(s => UserEmailGenerator.NormalizePart(s).Length > 0)
                    .WithMessage("AdminSurname must contain at least one letter or digit usable in an email address.");

            RuleFor(x => x.AdminDocumentNumber)
                .NotEmpty().WithMessage("AdminDocumentNumber is required.")
                .MaximumLength(20)
                .Matches("^[A-Za-z0-9]+$").WithMessage("AdminDocumentNumber must be alphanumeric.");
            // Same exact-8-digit rule RegisterUserCommandValidator applies to DocumentType.DNI —
            // the initial password here is AdminDocumentNumber too, same convention (CU-06).
            RuleFor(x => x.AdminDocumentNumber)
                .Matches("^[0-9]{8}$").WithMessage("DNI must be exactly 8 digits.")
                .When(x => x.AdminDocumentType == DocumentType.DNI);

            RuleFor(x => x.AdminDocumentType).IsInEnum();
        }
    }
}
