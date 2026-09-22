using Checkbus.ApiService.Application.Auth.Commands;
using FluentValidation.TestHelper;

namespace Checkbus.Tests.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    private static LoginCommand ValidCommand() => new()
    {
        Email = "user@example.com",
        Password = "password123"
    };

    [Fact]
    public void Email_Empty_IsRejected()
    {
        var command = ValidCommand();
        command.Email = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Email_Malformed_IsRejected()
    {
        var command = ValidCommand();
        command.Email = "not-an-email";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Email_TooLong_IsRejected()
    {
        var command = ValidCommand();
        command.Email = new string('a', 250) + "@x.co"; // 255 chars

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Email_Valid_PassesShapeCheck()
    {
        var command = ValidCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Email);
    }

    [Fact]
    public void Password_Empty_IsRejected()
    {
        var command = ValidCommand();
        command.Password = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void Password_TooShort_IsRejected()
    {
        var command = ValidCommand();
        command.Password = "abcdefg"; // 7 chars

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void Password_TooLong_IsRejected()
    {
        var command = ValidCommand();
        command.Password = new string('a', 129);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Password);
    }

    [Fact]
    public void Password_ValidLength_PassesShapeCheck()
    {
        var command = ValidCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Password);
    }
}
