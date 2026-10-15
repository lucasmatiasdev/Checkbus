using Checkbus.ApiService.Application.SupportTickets.Commands;
using Checkbus.ApiService.Domain.Enums;
using FluentValidation.TestHelper;

namespace Checkbus.Tests.SupportTickets;

public class CreateSupportTicketCommandValidatorTests
{
    private readonly CreateSupportTicketCommandValidator _validator = new();

    private static CreateSupportTicketCommand ValidCommand() => new()
    {
        Category = SupportTicketCategory.ProblemaTecnico,
        Subject = "No puedo ver mis reservas",
        Description = "El listado de reservas no carga desde ayer.",
        Priority = SupportTicketPriority.Alta
    };

    [Fact]
    public void ValidCommand_PassesShapeCheck()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Category_InvalidEnumValue_IsRejected()
    {
        var command = ValidCommand();
        command.Category = (SupportTicketCategory)999;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Category);
    }

    [Fact]
    public void Priority_InvalidEnumValue_IsRejected()
    {
        var command = ValidCommand();
        command.Priority = (SupportTicketPriority)999;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Priority);
    }

    [Fact]
    public void Subject_Empty_IsRejected()
    {
        var command = ValidCommand();
        command.Subject = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Subject);
    }

    [Fact]
    public void Subject_ExceedsMaxLength_IsRejected()
    {
        var command = ValidCommand();
        command.Subject = new string('a', 201);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Subject);
    }

    [Fact]
    public void Description_Empty_IsRejected()
    {
        var command = ValidCommand();
        command.Description = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void AttachmentStream_SetWithoutFileName_IsRejected()
    {
        var command = ValidCommand();
        command.AttachmentStream = new MemoryStream();
        command.AttachmentContentType = "image/png";
        command.AttachmentFileName = null;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.AttachmentFileName);
    }

    [Fact]
    public void AttachmentStream_SetWithoutContentType_IsRejected()
    {
        var command = ValidCommand();
        command.AttachmentStream = new MemoryStream();
        command.AttachmentFileName = "captura.png";
        command.AttachmentContentType = null;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.AttachmentContentType);
    }

    [Fact]
    public void AttachmentStream_SetWithFileNameAndContentType_PassesShapeCheck()
    {
        var command = ValidCommand();
        command.AttachmentStream = new MemoryStream();
        command.AttachmentFileName = "captura.png";
        command.AttachmentContentType = "image/png";

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
