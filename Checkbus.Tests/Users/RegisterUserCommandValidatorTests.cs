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
    public void DocumentNumber_SevenChars_IsAccepted()
    {
        // No MinimumLength on this field: the Q1 decision moved the short-password
        // floor to login, not to the DNI used here as the initial password.
        var command = ValidCommand();
        command.DocumentNumber = "4012345";

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
}
