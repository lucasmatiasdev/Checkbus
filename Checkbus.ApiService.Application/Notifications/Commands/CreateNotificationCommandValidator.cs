using FluentValidation;

namespace Checkbus.ApiService.Application.Notifications.Commands
{
    public sealed class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
    {
        public CreateNotificationCommandValidator()
        {
            RuleFor(x => x.TargetType).IsInEnum();
            RuleFor(x => x.TargetId).NotEmpty();
            RuleFor(x => x.Reason).IsInEnum();

            // Unlike CancelTripCommandValidator's Description rule, Message is unconditionally
            // required — a notification without a message is useless no matter the Reason.
            RuleFor(x => x.Message).NotEmpty();
        }
    }
}
