using FluentValidation;

namespace Checkbus.ApiService.Application.Events.Commands
{
    public sealed class UploadEventImageCommandValidator : AbstractValidator<UploadEventImageCommand>
    {
        private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB, same cap as UploadVehicleDocumentCommandValidator

        public UploadEventImageCommandValidator()
        {
            RuleFor(x => x.ContentType)
                .Must(contentType => AllowedContentTypes.Contains(contentType))
                .WithMessage($"Content type must be one of: {string.Join(", ", AllowedContentTypes)}.");

            RuleFor(x => x.FileSizeBytes)
                .InclusiveBetween(1, MaxFileSizeBytes)
                .WithMessage("File size must be between 1 byte and 5 MB.");
        }
    }
}
