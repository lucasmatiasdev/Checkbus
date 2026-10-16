using Checkbus.ApiService.Application.Trips.Commands;
using Checkbus.ApiService.Domain.Enums;
using FluentValidation.TestHelper;

namespace Checkbus.Tests.Trips;

public class CreatePublicReservationCommandValidatorTests
{
    private readonly CreatePublicReservationCommandValidator _validator = new();

    private static PassengerSeatRequest ValidPassenger() => new()
    {
        SeatId = Guid.NewGuid(),
        Name = "Juan",
        Surname = "Perez",
        DocumentType = DocumentType.DNI,
        DocumentNumber = "40123456",
        Email = "juan.perez@example.com",
        Phone = "1122334455"
    };

    private static CreatePublicReservationCommand ValidCommand() => new()
    {
        TripId = Guid.NewGuid(),
        Passengers = [ValidPassenger()]
    };

    [Fact]
    public void ValidCommand_PassesShapeCheck()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DocumentNumber_DniWithFewerThanEightDigits_IsRejected()
    {
        var passenger = ValidPassenger();
        passenger.DocumentType = DocumentType.DNI;
        passenger.DocumentNumber = "4012345";
        var command = ValidCommand();
        command.Passengers = [passenger];

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Passengers[0].DocumentNumber");
    }

    [Fact]
    public void DocumentNumber_DniWithExactlyEightDigits_IsAccepted()
    {
        var passenger = ValidPassenger();
        passenger.DocumentType = DocumentType.DNI;
        passenger.DocumentNumber = "40123456";
        var command = ValidCommand();
        command.Passengers = [passenger];

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor("Passengers[0].DocumentNumber");
    }

    [Fact]
    public void DocumentNumber_PasaporteWithFewerThanEightDigits_IsAccepted()
    {
        // The exact-8-digit rule only applies to DocumentType.DNI (CU-06/CU-14) — Pasaporte
        // only gets the NotEmpty check.
        var passenger = ValidPassenger();
        passenger.DocumentType = DocumentType.Pasaporte;
        passenger.DocumentNumber = "AB12345";
        var command = ValidCommand();
        command.Passengers = [passenger];

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor("Passengers[0].DocumentNumber");
    }

    [Fact]
    public void Phone_NonNumeric_IsRejected()
    {
        var passenger = ValidPassenger();
        passenger.Phone = "11-2233-4455";
        var command = ValidCommand();
        command.Passengers = [passenger];

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Passengers[0].Phone");
    }

    [Fact]
    public void Phone_TooShort_IsRejected()
    {
        var passenger = ValidPassenger();
        passenger.Phone = "1234567";
        var command = ValidCommand();
        command.Passengers = [passenger];

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Passengers[0].Phone");
    }

    [Fact]
    public void Phone_ValidDigitsOnly_IsAccepted()
    {
        var passenger = ValidPassenger();
        passenger.Phone = "1122334455";
        var command = ValidCommand();
        command.Passengers = [passenger];

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor("Passengers[0].Phone");
    }
}
