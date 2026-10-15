using FluentValidation;

namespace Checkbus.ApiService.Application.SupportTickets.Commands
{
    public sealed class UpdateSupportTicketStatusCommandValidator : AbstractValidator<UpdateSupportTicketStatusCommand>
    {
        public UpdateSupportTicketStatusCommandValidator()
        {
            // No transition restriction - any status to any status is allowed, confirmed scope
            // decision (odd/tasks/panel-soporte-checkbus.md). Only guards against an undefined
            // enum value.
            RuleFor(x => x.NewStatus).IsInEnum();
        }
    }
}
