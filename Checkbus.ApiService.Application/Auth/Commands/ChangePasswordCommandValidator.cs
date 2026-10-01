using FluentValidation;

namespace Checkbus.ApiService.Application.Auth.Commands
{
    public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
    {
        public ChangePasswordCommandValidator()
        {
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required.")
                .MinimumLength(8)
                .MaximumLength(128);
            // MinimumLength(8) is KEPT here — unlike the login validator, this
            // endpoint governs a user-chosen password, not a DNI-derived one.
        }
    }
}
