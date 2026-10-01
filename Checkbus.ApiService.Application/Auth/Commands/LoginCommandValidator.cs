using FluentValidation;

namespace Checkbus.ApiService.Application.Auth.Commands
{
    public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .MaximumLength(254)
                .EmailAddress();

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.")
                .MaximumLength(128);
            // No MinimumLength: a newly-registered user's initial password is their
            // DocumentNumber, which may legitimately be as short as 7 characters
            // (Argentine DNIs). The 8-character floor moved to ChangePasswordCommandValidator,
            // which governs user-chosen passwords.
        }
    }
}
