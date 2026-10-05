using Checkbus.ApiService.Application.Events.Commands;
using Checkbus.ApiService.Domain.Enums;
using FluentValidation.TestHelper;

namespace Checkbus.Tests.Events;

public class CreateEventCommandValidatorTests
{
    private readonly CreateEventCommandValidator _validator = new();

    private static CreateEventCommand ValidCommand() => new()
    {
        Name = "Final Copa Argentina",
        Type = EventType.Partido,
        Date = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
        LocationName = "Estadio Mario Alberto Kempes",
        Address = "Av. Cardeñosa, Córdoba",
        PlaceId = "place-kempes-1",
        Latitude = -31.3333,
        Longitude = -64.2333
    };

    [Fact]
    public void ValidCommand_PassesShapeCheck()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Nombre_Empty_IsRejected()
    {
        var command = ValidCommand();
        command.Name = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Direccion_Empty_IsRejected()
    {
        var command = ValidCommand();
        command.Address = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Address);
    }

    [Fact]
    public void PlaceId_Empty_IsRejected()
    {
        var command = ValidCommand();
        command.PlaceId = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.PlaceId);
    }

    [Fact]
    public void Tipo_InvalidEnumValue_IsRejected()
    {
        var command = ValidCommand();
        command.Type = (EventType)999;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Type);
    }

    [Fact]
    public void Fecha_Default_IsRejected()
    {
        var command = ValidCommand();
        command.Date = default;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Date);
    }
}
