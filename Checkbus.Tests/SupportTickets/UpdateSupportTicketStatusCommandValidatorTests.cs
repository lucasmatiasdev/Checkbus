using Checkbus.ApiService.Application.SupportTickets.Commands;
using Checkbus.ApiService.Domain.Enums;
using FluentValidation.TestHelper;

namespace Checkbus.Tests.SupportTickets;

public class UpdateSupportTicketStatusCommandValidatorTests
{
    private readonly UpdateSupportTicketStatusCommandValidator _validator = new();

    private static UpdateSupportTicketStatusCommand ValidCommand() => new()
    {
        TicketId = Guid.NewGuid(),
        NewStatus = SupportTicketStatus.Resuelto
    };

    [Fact]
    public void ValidCommand_PassesShapeCheck()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NewStatus_InvalidEnumValue_IsRejected()
    {
        var command = ValidCommand();
        command.NewStatus = (SupportTicketStatus)999;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.NewStatus);
    }
}
