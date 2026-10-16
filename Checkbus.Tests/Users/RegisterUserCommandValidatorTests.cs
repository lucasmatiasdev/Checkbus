using Checkbus.ApiService.Application.Users.Commands;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Enums;
using FluentValidation.TestHelper;

namespace Checkbus.Tests.Users;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    private static RegisterUserCommand ValidCommand() => new()
    {
        Name = "Jose",
        Surname = "Diaz",
        DocumentType = DocumentType.DNI,
        DocumentNumber = "40123456",
        Role = Role.Chofer
    };

    [Fact]
    public void ValidCommand_PassesShapeCheck()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Name_Empty_IsRejected()
    {
        var command = ValidCommand();
        command.Name = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Name_TooLong_IsRejected()
    {
        var command = ValidCommand();
        command.Name = new string('a', 101);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Name_NonLatinScript_NormalizesToEmpty_IsRejected()
    {
        var command = ValidCommand();
        command.Name = "李";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Surname_Empty_IsRejected()
    {
        var command = ValidCommand();
        command.Surname = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Surname);
    }

    [Fact]
    public void Surname_TooLong_IsRejected()
    {
        var command = ValidCommand();
        command.Surname = new string('a', 101);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Surname);
    }

    [Fact]
    public void Surname_NonLatinScript_NormalizesToEmpty_IsRejected()
    {
        var command = ValidCommand();
        command.Surname = "李";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Surname);
    }

    [Fact]
    public void DocumentNumber_Empty_IsRejected()
    {
        var command = ValidCommand();
        command.DocumentNumber = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.DocumentNumber);
    }

    [Fact]
    public void DocumentNumber_TooLong_IsRejected()
    {
        var command = ValidCommand();
        command.DocumentNumber = new string('1', 21);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.DocumentNumber);
    }

    [Fact]
    public void DocumentNumber_NonAlphanumeric_IsRejected()
    {
        var command = ValidCommand();
        command.DocumentNumber = "4012-3456";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.DocumentNumber);
    }

    [Fact]
    public void DocumentNumber_SevenChars_PasaporteType_IsAccepted()
    {
        // No generic MinimumLength on this field: the Q1 decision moved the short-password
        // floor to login, not to the document number used here as the initial password.
        // The exact-8-digit rule below only applies to DocumentType.DNI (CU-06).
        var command = ValidCommand();
        command.DocumentType = DocumentType.Pasaporte;
        command.DocumentNumber = "4012345";

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.DocumentNumber);
    }

    [Fact]
    public void DocumentNumber_DniWithFewerThanEightDigits_IsRejected()
    {
        var command = ValidCommand();
        command.DocumentType = DocumentType.DNI;
        command.DocumentNumber = "4012345";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.DocumentNumber);
    }

    [Fact]
    public void DocumentNumber_DniWithNonDigitCharacters_IsRejected()
    {
        var command = ValidCommand();
        command.DocumentType = DocumentType.DNI;
        command.DocumentNumber = "4012345A";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.DocumentNumber);
    }

    [Fact]
    public void DocumentNumber_DniWithExactlyEightDigits_IsAccepted()
    {
        var command = ValidCommand();
        command.DocumentType = DocumentType.DNI;
        command.DocumentNumber = "40123456";

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.DocumentNumber);
    }

    [Fact]
    public void DocumentType_InvalidEnumValue_IsRejected()
    {
        var command = ValidCommand();
        command.DocumentType = (DocumentType)999;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.DocumentType);
    }

    [Fact]
    public void Role_InvalidEnumValue_IsRejected()
    {
        var command = ValidCommand();
        command.Role = (Role)999;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Role);
    }

    [Fact]
    public void Role_CheckbusSoporte_IsRejected()
    {
        // CheckbusSoporte must never be self-registered: it is only ever meaningful for a
        // user of the sentinel Checkbus-internal organization and is created exclusively by
        // seeding (see CheckbusDbSeeder), never through this self-service registration path.
        var command = ValidCommand();
        command.Role = Role.CheckbusSoporte;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Role);
    }
}
