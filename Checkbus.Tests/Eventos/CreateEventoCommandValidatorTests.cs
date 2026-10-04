using Checkbus.ApiService.Application.Eventos.Commands;
using Checkbus.ApiService.Domain.Enums;
using FluentValidation.TestHelper;

namespace Checkbus.Tests.Eventos;

public class CreateEventoCommandValidatorTests
{
    private readonly CreateEventoCommandValidator _validator = new();

    private static CreateEventoCommand ValidCommand() => new()
    {
        Nombre = "Final Copa Argentina",
        Tipo = EventoTipo.Partido,
        Fecha = new DateTime(2026, 5, 1, 20, 0, 0, DateTimeKind.Utc),
        UbicacionNombre = "Estadio Mario Alberto Kempes",
        Direccion = "Av. Cardeñosa, Córdoba",
        PlaceId = "place-kempes-1",
        Latitud = -31.3333,
        Longitud = -64.2333
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
        command.Nombre = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Nombre);
    }

    [Fact]
    public void Direccion_Empty_IsRejected()
    {
        var command = ValidCommand();
        command.Direccion = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Direccion);
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
        command.Tipo = (EventoTipo)999;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Tipo);
    }

    [Fact]
    public void Fecha_Default_IsRejected()
    {
        var command = ValidCommand();
        command.Fecha = default;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Fecha);
    }
}
