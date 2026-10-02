using FluentValidation;

namespace Checkbus.ApiService.Application.DriverRequirements.Commands
{
    public sealed class UploadDriverRequirementDocumentCommandValidator : AbstractValidator<UploadDriverRequirementDocumentCommand>
    {
        private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "application/pdf"];
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        public UploadDriverRequirementDocumentCommandValidator()
        {
            RuleFor(x => x.ContentType)
                .Must(contentType => AllowedContentTypes.Contains(contentType))
                .WithMessage($"Content type must be one of: {string.Join(", ", AllowedContentTypes)}.");

            RuleFor(x => x.FileSizeBytes)
                .InclusiveBetween(1, MaxFileSizeBytes)
                .WithMessage("File size must be between 1 byte and 5 MB.");

            RuleFor(x => x.ExpirationDate)
                .GreaterThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Expiration date cannot be in the past.");
        }
    }
}
