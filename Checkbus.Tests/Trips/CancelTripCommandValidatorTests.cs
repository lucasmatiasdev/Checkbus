using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Domain.Enums;
using FluentValidation.TestHelper;

namespace Checkbus.Tests.Trips;

public class CancelTripCommandValidatorTests
{
    private readonly CancelTripCommandValidator _validator = new();

    private static CancelTripCommand ValidCommand() => new()
    {
        Id = Guid.NewGuid(),
        Reason = TripCancellationReason.ProblemaChofer
    };

    [Fact]
    public void ValidCommand_PassesShapeCheck()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Reason_InvalidEnumValue_IsRejected()
    {
        var command = ValidCommand();
        command.Reason = (TripCancellationReason)999;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Reason);
    }

    [Fact]
    public void ReasonOtro_WithoutDescription_IsRejected()
    {
        var command = ValidCommand();
        command.Reason = TripCancellationReason.Otro;
        command.Description = null;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void ReasonOtro_WithDescription_PassesShapeCheck()
    {
        var command = ValidCommand();
        command.Reason = TripCancellationReason.Otro;
        command.Description = "El evento que motivaba el viaje fue reprogramado.";

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void ReasonNotOtro_WithoutDescription_PassesShapeCheck()
    {
        var command = ValidCommand();
        command.Reason = TripCancellationReason.DesperfectoVehiculo;
        command.Description = null;

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Description);
    }
}
