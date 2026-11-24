using FluentValidation;

namespace Checkbus.ApiService.Application.SupportTickets.Commands
{
    public sealed class UpdateSupportTicketStatusCommandValidator : AbstractValidator<UpdateSupportTicketStatusCommand>
    {
        public UpdateSupportTicketStatusCommandValidator()
        {

            RuleFor(x => x.NewStatus).IsInEnum();
        }
    }
}
