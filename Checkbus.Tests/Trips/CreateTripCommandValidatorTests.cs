using Checkbus.ApiService.Application.Trips.Commands;

namespace Checkbus.Tests.Trips;

public class CreateTripCommandValidatorTests
{
    private readonly CreateTripCommandValidator _validator = new();

    private static CreateTripCommand CreateValidCommand() => new()
    {
        VehicleId = Guid.NewGuid(),
        DriverId = Guid.NewGuid(),
        EventId = Guid.NewGuid(),
        DepartureDate = DateTime.UtcNow.AddDays(30),
        ArrivalDate = DateTime.UtcNow.AddDays(30).AddHours(8),
        Price = 15000m,
        Stops =
        [
            new CreateTripStopCommand { Name = "Origen", Address = "Dir 1", PlaceId = "place-1", Latitude = 1, Longitude = 1 },
            new CreateTripStopCommand { Name = "Destino", Address = "Dir 2", PlaceId = "place-2", Latitude = 2, Longitude = 2 }
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTripCommand.VehicleId));
    }

    [Fact]
    public void Validate_EmptyDriverId_HasError()
    {
        var command = CreateValidCommand();
        command.DriverId = Guid.Empty;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTripCommand.DriverId));
    }

    [Fact]
    public void Validate_EmptyEventId_HasError()
    {
        var command = CreateValidCommand();
        command.EventId = Guid.Empty;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTripCommand.EventId));
    }

    [Fact]
    public void Validate_DepartureDateInThePast_HasError()
    {
        var command = CreateValidCommand();
        command.DepartureDate = DateTime.UtcNow.AddDays(-1);
        command.ArrivalDate = DateTime.UtcNow.AddHours(-1);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTripCommand.DepartureDate));
    }

    [Fact]
    public void Validate_ArrivalDateNotAfterDepartureDate_HasError()
    {
        var command = CreateValidCommand();
        command.ArrivalDate = command.DepartureDate;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTripCommand.ArrivalDate));
    }

    [Fact]
    public void Validate_ArrivalDateBeforeDepartureDate_HasError()
    {
        var command = CreateValidCommand();
        command.ArrivalDate = command.DepartureDate.AddHours(-1);

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTripCommand.ArrivalDate));
    }

    [Fact]
    public void Validate_PriceZero_HasError()
    {
        var command = CreateValidCommand();
        command.Price = 0;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTripCommand.Price));
    }

    [Fact]
    public void Validate_NegativePrice_HasError()
    {
        var command = CreateValidCommand();
        command.Price = -1;

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTripCommand.Price));
    }

    [Fact]
    public void Validate_FewerThanTwoStops_HasError()
    {
        var command = CreateValidCommand();
        command.Stops = [command.Stops[0]];

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTripCommand.Stops));
    }

    [Fact]
    public void Validate_NoStops_HasError()
    {
        var command = CreateValidCommand();
        command.Stops = [];

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTripCommand.Stops));
    }
}
