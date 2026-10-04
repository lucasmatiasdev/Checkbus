using Checkbus.ApiService.Application.Viajes.Commands;

namespace Checkbus.Tests.Viajes;

public class CreateViajeCommandValidatorTests
{
    private readonly CreateViajeCommandValidator _validator = new();

    private static CreateViajeCommand CreateValidCommand() => new()
    {
        VehicleId = Guid.NewGuid(),
        ChoferId = Guid.NewGuid(),
        EventoId = Guid.NewGuid(),
        FechaSalida = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
        FechaLlegada = new DateTime(2026, 5, 1, 18, 0, 0, DateTimeKind.Utc),
        Precio = 15000m,
        Stops =
        [
            new CreateViajeStopCommand { Nombre = "Origen", Direccion = "Dir 1", PlaceId = "place-1", Latitud = 1, Longitud = 1 },
            new CreateViajeStopCommand { Nombre = "Destino", Direccion = "Dir 2", PlaceId = "place-2", Latitud = 2, Longitud = 2 }
        ]
    };

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.Validate(CreateValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyVehicleId_HasError()
    {
        var command = CreateValidCommand();
        command.VehicleId = Guid.Empty;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateViajeCommand.VehicleId));
    }

    [Fact]
    public void Validate_EmptyChoferId_HasError()
    {
        var command = CreateValidCommand();
        command.ChoferId = Guid.Empty;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateViajeCommand.ChoferId));
    }

    [Fact]
    public void Validate_EmptyEventoId_HasError()
    {
        var command = CreateValidCommand();
        command.EventoId = Guid.Empty;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateViajeCommand.EventoId));
    }

    [Fact]
    public void Validate_FechaLlegadaNotAfterFechaSalida_HasError()
    {
        var command = CreateValidCommand();
        command.FechaLlegada = command.FechaSalida;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateViajeCommand.FechaLlegada));
    }

    [Fact]
    public void Validate_FechaLlegadaBeforeFechaSalida_HasError()
    {
        var command = CreateValidCommand();
        command.FechaLlegada = command.FechaSalida.AddHours(-1);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateViajeCommand.FechaLlegada));
    }

    [Fact]
    public void Validate_PrecioZero_HasError()
    {
        var command = CreateValidCommand();
        command.Precio = 0;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateViajeCommand.Precio));
    }

    [Fact]
    public void Validate_NegativePrecio_HasError()
    {
        var command = CreateValidCommand();
        command.Precio = -1;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateViajeCommand.Precio));
    }

    [Fact]
    public void Validate_FewerThanTwoStops_HasError()
    {
        var command = CreateValidCommand();
        command.Stops = [command.Stops[0]];

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateViajeCommand.Stops));
    }

    [Fact]
    public void Validate_NoStops_HasError()
    {
        var command = CreateValidCommand();
        command.Stops = [];

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateViajeCommand.Stops));
    }
}
