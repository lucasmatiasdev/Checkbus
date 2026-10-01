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
            // No MinimumLength: per the resolved Q1 decision, the short-password floor
            // moved to login, not to the DNI used here as the initial password.

            RuleFor(x => x.DocumentType).IsInEnum();
            RuleFor(x => x.Role).IsInEnum();
        }
    }
}
