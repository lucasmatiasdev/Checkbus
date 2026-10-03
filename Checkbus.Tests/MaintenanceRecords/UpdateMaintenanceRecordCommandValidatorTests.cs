using Checkbus.ApiService.Application.MaintenanceRecords.Commands;
using Checkbus.ApiService.Domain.Enums;
using FluentValidation.TestHelper;

namespace Checkbus.Tests.MaintenanceRecords;

public class UpdateMaintenanceRecordCommandValidatorTests
{
    private readonly UpdateMaintenanceRecordCommandValidator _validator = new();

    private static UpdateMaintenanceRecordCommand ValidCommand() => new()
    {
        Id = Guid.NewGuid(),
        Status = MaintenanceStatus.Programado
    };

    [Fact]
    public void ValidCommand_PassesShapeCheck()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Status_InvalidEnumValue_IsRejected()
    {
        var command = ValidCommand();
        command.Status = (MaintenanceStatus)999;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Status);
    }

    [Fact]
    public void Status_EnProceso_WithoutStartDate_IsRejected()
    {
        var command = ValidCommand();
        command.Status = MaintenanceStatus.EnProceso;
        command.StartDate = null;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.StartDate);
    }

    [Fact]
    public void Status_EnProceso_WithStartDate_IsAccepted()
    {
        var command = ValidCommand();
        command.Status = MaintenanceStatus.EnProceso;
        command.StartDate = new DateOnly(2026, 1, 1);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.StartDate);
    }

    [Fact]
    public void EndDate_BeforeStartDate_IsRejected()
    {
        var command = ValidCommand();
        command.StartDate = new DateOnly(2026, 1, 10);
        command.EndDate = new DateOnly(2026, 1, 5);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.EndDate);
    }

    [Fact]
    public void EndDate_EqualToStartDate_IsAccepted()
    {
        var command = ValidCommand();
        command.StartDate = new DateOnly(2026, 1, 10);
        command.EndDate = new DateOnly(2026, 1, 10);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.EndDate);
    }

    [Fact]
    public void EndDate_AfterStartDate_IsAccepted()
    {
        var command = ValidCommand();
        command.StartDate = new DateOnly(2026, 1, 10);
        command.EndDate = new DateOnly(2026, 1, 20);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.EndDate);
    }
}
