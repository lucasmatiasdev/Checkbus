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

            RuleFor(x => x.Message).NotEmpty();
        }
    }
}
