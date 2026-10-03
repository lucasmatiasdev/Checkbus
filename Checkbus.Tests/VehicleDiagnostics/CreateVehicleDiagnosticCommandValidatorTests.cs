using Checkbus.ApiService.Application.VehicleDiagnostics.Commands;
using Checkbus.ApiService.Domain.Enums;
using FluentValidation.TestHelper;

namespace Checkbus.Tests.VehicleDiagnostics;

public class CreateVehicleDiagnosticCommandValidatorTests
{
    private readonly CreateVehicleDiagnosticCommandValidator _validator = new();

    private static CreateVehicleDiagnosticCommand ValidCommand() => new()
    {
        MaintenanceRecordId = Guid.NewGuid(),
        DiagnosedAt = new DateOnly(2026, 1, 2),
        Notes = "Revision general",
        Components =
        [
            new ComponentDiagnosticInput(VehicleComponent.Motor, ComponentCondition.Bueno, null)
        ]
    };

    [Fact]
    public void ValidCommand_PassesShapeCheck()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void MaintenanceRecordId_Empty_IsRejected()
    {
        var command = ValidCommand();
        command.MaintenanceRecordId = Guid.Empty;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.MaintenanceRecordId);
    }

    [Fact]
    public void DiagnosedAt_Default_IsRejected()
    {
        var command = ValidCommand();
        command.DiagnosedAt = default;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.DiagnosedAt);
    }

    [Fact]
    public void Components_Empty_IsRejected()
    {
        var command = ValidCommand();
        command.Components = [];

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Components);
    }

    [Fact]
    public void Components_InvalidComponentEnumValue_IsRejected()
    {
        var command = ValidCommand();
        command.Components = [new ComponentDiagnosticInput((VehicleComponent)999, ComponentCondition.Bueno, null)];

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Components[0].Component");
    }

    [Fact]
    public void Components_InvalidConditionEnumValue_IsRejected()
    {
        var command = ValidCommand();
        command.Components = [new ComponentDiagnosticInput(VehicleComponent.Motor, (ComponentCondition)999, null)];

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Components[0].Condition");
    }
}
