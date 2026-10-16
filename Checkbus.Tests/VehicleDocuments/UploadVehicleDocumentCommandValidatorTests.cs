using Checkbus.ApiService.Application.VehicleDocuments.Commands;
using Checkbus.ApiService.Domain.Enums;
using FluentValidation.TestHelper;

namespace Checkbus.Tests.VehicleDocuments;

public class UploadVehicleDocumentCommandValidatorTests
{
    private readonly UploadVehicleDocumentCommandValidator _validator = new();

    private static UploadVehicleDocumentCommand ValidCommand() => new()
    {
        VehicleId = Guid.NewGuid(),
        Type = VehicleDocumentType.Seguro,
        FileStream = new MemoryStream([1, 2, 3]),
        ContentType = "application/pdf",
        FileSizeBytes = 1024,
        OriginalFileName = "seguro.pdf",
        DocumentNumber = "POL-12345",
        ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30)
    };

    [Fact]
    public void ValidCommand_PassesShapeCheck()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("application/pdf")]
    public void ContentType_Allowed_IsAccepted(string contentType)
    {
        var command = ValidCommand();
        command.ContentType = contentType;

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.ContentType);
    }

    [Theory]
    [InlineData("application/zip")]
    [InlineData("text/plain")]
    [InlineData("image/gif")]
    [InlineData("")]
    public void ContentType_NotAllowed_IsRejected(string contentType)
    {
        var command = ValidCommand();
        command.ContentType = contentType;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ContentType);
    }

    [Fact]
    public void FileSizeBytes_Zero_IsRejected()
    {
        var command = ValidCommand();
        command.FileSizeBytes = 0;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.FileSizeBytes);
    }

    [Fact]
    public void FileSizeBytes_ExceedsTenMegabytes_IsRejected()
    {
        var command = ValidCommand();
        command.FileSizeBytes = 10 * 1024 * 1024 + 1;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.FileSizeBytes);
    }

    [Fact]
    public void FileSizeBytes_ExactlyTenMegabytes_IsAccepted()
    {
        var command = ValidCommand();
        command.FileSizeBytes = 10 * 1024 * 1024;

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.FileSizeBytes);
    }

    [Fact]
    public void FileSizeBytes_One_IsAccepted()
    {
        var command = ValidCommand();
        command.FileSizeBytes = 1;

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.FileSizeBytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void DocumentNumber_EmptyOrWhitespace_IsRejected(string documentNumber)
    {
        var command = ValidCommand();
        command.DocumentNumber = documentNumber;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.DocumentNumber);
    }

    [Fact]
    public void DocumentNumber_ExceedsFiftyCharacters_IsRejected()
    {
        var command = ValidCommand();
        command.DocumentNumber = new string('9', 51);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.DocumentNumber);
    }

    [Fact]
    public void ExpirationDate_InThePast_IsRejected()
    {
        var command = ValidCommand();
        command.ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ExpirationDate);
    }

    [Fact]
    public void ExpirationDate_Today_IsAccepted()
    {
        var command = ValidCommand();
        command.ExpirationDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.ExpirationDate);
    }
}
