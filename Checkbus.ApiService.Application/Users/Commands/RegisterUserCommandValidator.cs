using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Enums;
using FluentValidation;

namespace Checkbus.ApiService.Application.Users.Commands
{
    public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100)
                .Must(n => UserEmailGenerator.NormalizePart(n).Length > 0)
                    .WithMessage("Name must contain at least one letter or digit usable in an email address.");

            RuleFor(x => x.Surname)
                .NotEmpty().WithMessage("Surname is required.")
                .MaximumLength(100)
                .Must(s => UserEmailGenerator.NormalizePart(s).Length > 0)
                    .WithMessage("Surname must contain at least one letter or digit usable in an email address.");

            RuleFor(x => x.DocumentNumber)
                .NotEmpty().WithMessage("Document number is required.")
                .MaximumLength(20)
                .Matches("^[A-Za-z0-9]+$").WithMessage("Document number must be alphanumeric.");
            // No generic MinimumLength: per the resolved Q1 decision, the short-password floor
            // moved to login, not to the DNI used here as the initial password. The exact
            // 8-digit rule below is specific to DocumentType.DNI (CU-06) and doesn't reopen
            // that decision — Pasaporte numbers keep the generic alphanumeric rule above.
            RuleFor(x => x.DocumentNumber)
                .Matches("^[0-9]{8}$").WithMessage("DNI must be exactly 8 digits.")
                .When(x => x.DocumentType == DocumentType.DNI);

            RuleFor(x => x.DocumentType).IsInEnum();
            RuleFor(x => x.Role)
                .IsInEnum()
                .Must(role => role != Role.CheckbusSoporte)
                    .WithMessage("CheckbusSoporte cannot be self-registered.");
        }
    }
}
