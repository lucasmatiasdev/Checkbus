using FluentValidation;

namespace Checkbus.ApiService.Application.SupportTickets.Commands
{
    public sealed class CreateSupportTicketCommandValidator : AbstractValidator<CreateSupportTicketCommand>
    {
        public CreateSupportTicketCommandValidator()
        {
            RuleFor(x => x.Category).IsInEnum();

            RuleFor(x => x.Subject)
                .NotEmpty().WithMessage("Subject is required.")
                .MinimumLength(5).WithMessage("Subject must be at least 5 characters long.")
                .MaximumLength(200).WithMessage("Subject must not exceed 200 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MinimumLength(20).WithMessage("Description must be at least 20 characters long.");

            RuleFor(x => x.Priority).IsInEnum();

            // A stream with no name/content-type (or vice versa) is not a usable attachment -
            // same "all-or-nothing" shape UploadVehicleDocumentCommand's required trio enforces,
            // but conditional here since AttachmentStream itself is optional.
            RuleFor(x => x.AttachmentFileName)
                .NotEmpty().WithMessage("AttachmentFileName is required when an attachment is provided.")
                .When(x => x.AttachmentStream is not null);

            RuleFor(x => x.AttachmentContentType)
                .NotEmpty().WithMessage("AttachmentContentType is required when an attachment is provided.")
                .When(x => x.AttachmentStream is not null);
        }
    }
}
