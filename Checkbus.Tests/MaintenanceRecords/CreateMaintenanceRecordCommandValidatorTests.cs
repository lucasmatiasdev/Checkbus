using Checkbus.ApiService.Application.MaintenanceRecords.Commands;
using Checkbus.ApiService.Domain.Enums;
using FluentValidation.TestHelper;

namespace Checkbus.Tests.MaintenanceRecords;

public class CreateMaintenanceRecordCommandValidatorTests
{
    private readonly CreateMaintenanceRecordCommandValidator _validator = new();

    private static CreateMaintenanceRecordCommand ValidCommand() => new()
    {
        VehicleId = Guid.NewGuid(),
        Type = MaintenanceType.Preventivo,
        ScheduledDate = new DateOnly(2026, 1, 1),
        Description = "Cambio de aceite"
    };

    [Fact]
    public void ValidCommand_PassesShapeCheck()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
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
    public void ScheduledDate_Default_IsRejected()
    {
        var command = ValidCommand();
        command.ScheduledDate = default;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ScheduledDate);
    }

    [Fact]
    public void Type_InvalidEnumValue_IsRejected()
    {
        var command = ValidCommand();
        command.Type = (MaintenanceType)999;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Type);
    }
}
