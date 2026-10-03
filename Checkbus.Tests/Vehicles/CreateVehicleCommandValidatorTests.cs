using Checkbus.ApiService.Application.Vehicles.Commands;
using Checkbus.ApiService.Domain.Enums;
using FluentValidation.TestHelper;

namespace Checkbus.Tests.Vehicles;

public class CreateVehicleCommandValidatorTests
{
    private readonly CreateVehicleCommandValidator _validator = new();

    private static CreateVehicleCommand ValidOrganizacionCommand() => new()
    {
        Brand = "Mercedes-Benz",
        Model = "O500",
        Year = 2020,
        Patent = "AB123CD",
        Capacity = 45,
        Mileage = 1000,
        Status = VehicleStatus.Activo,
        OwnerType = VehicleOwnerType.Organizacion
    };

    [Fact]
    public void ValidOrganizacionCommand_PassesShapeCheck()
    {
        var result = _validator.TestValidate(ValidOrganizacionCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Brand_Empty_IsRejected(string brand)
    {
        var command = ValidOrganizacionCommand();
        command.Brand = brand;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Brand);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Model_Empty_IsRejected(string model)
    {
        var command = ValidOrganizacionCommand();
        command.Model = model;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Model);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Patent_Empty_IsRejected(string patent)
    {
        var command = ValidOrganizacionCommand();
        command.Patent = patent;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Patent);
    }

    [Fact]
    public void Year_BelowMinimum_IsRejected()
    {
        var command = ValidOrganizacionCommand();
        command.Year = 1979;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Year);
    }

    [Fact]
    public void Year_MoreThanOneYearInTheFuture_IsRejected()
    {
        var command = ValidOrganizacionCommand();
        command.Year = DateTime.UtcNow.Year + 2;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Year);
    }

    [Fact]
    public void Year_NextYear_IsAccepted()
    {
        var command = ValidOrganizacionCommand();
        command.Year = DateTime.UtcNow.Year + 1;

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Year);
    }

    [Fact]
    public void Capacity_Negative_IsRejected()
    {
        var command = ValidOrganizacionCommand();
        command.Capacity = -1;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Capacity);
    }

    [Fact]
    public void Capacity_Zero_IsAccepted()
    {
        var command = ValidOrganizacionCommand();
        command.Capacity = 0;

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Capacity);
    }

    [Fact]
    public void Mileage_Negative_IsRejected()
    {
        var command = ValidOrganizacionCommand();
        command.Mileage = -1;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Mileage);
    }

    [Fact]
    public void Mileage_Zero_IsAccepted()
    {
        var command = ValidOrganizacionCommand();
        command.Mileage = 0;

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Mileage);
    }

    // --- OwnerType = Organizacion: no owner fields accepted ---

    [Fact]
    public void Organizacion_WithOwnerUserId_IsRejected()
    {
        var command = ValidOrganizacionCommand();
        command.OwnerUserId = Guid.NewGuid();

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.OwnerUserId);
    }

    [Fact]
    public void Organizacion_WithOwnerName_IsRejected()
    {
        var command = ValidOrganizacionCommand();
        command.OwnerName = "Juan Perez";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.OwnerName);
    }

    [Fact]
    public void Organizacion_WithOwnerDocumentNumber_IsRejected()
    {
        var command = ValidOrganizacionCommand();
        command.OwnerDocumentNumber = "30111222";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.OwnerDocumentNumber);
    }

    // --- OwnerType = Chofer: OwnerUserId required, Name/DNI must be unset ---

    [Fact]
    public void Chofer_MissingOwnerUserId_IsRejected()
    {
        var command = ValidOrganizacionCommand();
        command.OwnerType = VehicleOwnerType.Chofer;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.OwnerUserId);
    }

    [Fact]
    public void Chofer_WithOwnerUserIdOnly_PassesShapeCheck()
    {
        var command = ValidOrganizacionCommand();
        command.OwnerType = VehicleOwnerType.Chofer;
        command.OwnerUserId = Guid.NewGuid();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Chofer_WithOwnerName_IsRejected()
    {
        var command = ValidOrganizacionCommand();
        command.OwnerType = VehicleOwnerType.Chofer;
        command.OwnerUserId = Guid.NewGuid();
        command.OwnerName = "Juan Perez";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.OwnerName);
    }

    [Fact]
    public void Chofer_WithOwnerDocumentNumber_IsRejected()
    {
        var command = ValidOrganizacionCommand();
        command.OwnerType = VehicleOwnerType.Chofer;
        command.OwnerUserId = Guid.NewGuid();
        command.OwnerDocumentNumber = "30111222";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.OwnerDocumentNumber);
    }

    // --- OwnerType = Otro: Name + DNI required, OwnerUserId must be unset ---

    [Fact]
    public void Otro_MissingOwnerNameAndDocumentNumber_IsRejected()
    {
        var command = ValidOrganizacionCommand();
        command.OwnerType = VehicleOwnerType.Otro;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.OwnerName);
        result.ShouldHaveValidationErrorFor(c => c.OwnerDocumentNumber);
    }

    [Fact]
    public void Otro_WithNameAndDocumentNumberOnly_PassesShapeCheck()
    {
        var command = ValidOrganizacionCommand();
        command.OwnerType = VehicleOwnerType.Otro;
        command.OwnerName = "Juan Perez";
        command.OwnerDocumentNumber = "30111222";

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Otro_WithOwnerUserId_IsRejected()
    {
        var command = ValidOrganizacionCommand();
        command.OwnerType = VehicleOwnerType.Otro;
        command.OwnerName = "Juan Perez";
        command.OwnerDocumentNumber = "30111222";
        command.OwnerUserId = Guid.NewGuid();

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.OwnerUserId);
    }
}
