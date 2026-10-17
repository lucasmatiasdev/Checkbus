using Checkbus.ApiService.Application.Tenancy.Commands;
using Checkbus.ApiService.Domain.Enums;
using FluentValidation.TestHelper;

namespace Checkbus.Tests.Tenancy;

public class RegisterOrganizationCommandValidatorTests
{
    private readonly RegisterOrganizationCommandValidator _validator = new();

    private static RegisterOrganizationCommand ValidCommand() => new()
    {
        OrganizationName = "Checkbus Demo",
        CUIT = "20-12345678-9",
        AdminName = "Jose",
        AdminSurname = "Diaz",
        AdminDocumentType = DocumentType.DNI,
        AdminDocumentNumber = "40123456"
    };

    [Fact]
    public void ValidCommand_PassesShapeCheck()
    {
        var result = _validator.TestValidate(ValidCommand());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void OrganizationName_Empty_IsRejected()
    {
        var command = ValidCommand();
        command.OrganizationName = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.OrganizationName);
    }

    [Fact]
    public void OrganizationName_TooLong_IsRejected()
    {
        var command = ValidCommand();
        command.OrganizationName = new string('a', 201);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.OrganizationName);
    }

    [Fact]
    public void OrganizationName_SanitizesToEmptySlug_IsRejected()
    {
        var command = ValidCommand();
        command.OrganizationName = "!!!";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.OrganizationName);
    }

    [Theory]
    [InlineData("20-12345678-9")]
    [InlineData("30-98765432-1")]
    public void CUIT_ValidFormat_IsAccepted(string cuit)
    {
        var command = ValidCommand();
        command.CUIT = cuit;

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.CUIT);
    }

    [Fact]
    public void CUIT_Empty_IsRejected()
    {
        var command = ValidCommand();
        command.CUIT = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CUIT);
    }

    [Theory]
    [InlineData("20123456789")]
    [InlineData("20-123456789")]
    [InlineData("201-2345678-9")]
    [InlineData("20-12345678-99")]
    [InlineData("AB-12345678-9")]
    public void CUIT_InvalidFormat_IsRejected(string cuit)
    {
        var command = ValidCommand();
        command.CUIT = cuit;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.CUIT);
    }

    [Fact]
    public void AdminName_Empty_IsRejected()
    {
        var command = ValidCommand();
        command.AdminName = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.AdminName);
    }

    [Fact]
    public void AdminSurname_Empty_IsRejected()
    {
        var command = ValidCommand();
        command.AdminSurname = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.AdminSurname);
    }

    [Fact]
    public void AdminDocumentNumber_Empty_IsRejected()
    {
        var command = ValidCommand();
        command.AdminDocumentNumber = "";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.AdminDocumentNumber);
    }

    [Fact]
    public void AdminDocumentNumber_DniWithFewerThanEightDigits_IsRejected()
    {
        var command = ValidCommand();
        command.AdminDocumentType = DocumentType.DNI;
        command.AdminDocumentNumber = "4012345";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.AdminDocumentNumber);
    }

    [Fact]
    public void AdminDocumentNumber_SevenChars_PasaporteType_IsAccepted()
    {
        var command = ValidCommand();
        command.AdminDocumentType = DocumentType.Pasaporte;
        command.AdminDocumentNumber = "4012345";

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.AdminDocumentNumber);
    }

    [Fact]
    public void AdminDocumentType_InvalidEnumValue_IsRejected()
    {
        var command = ValidCommand();
        command.AdminDocumentType = (DocumentType)999;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.AdminDocumentType);
    }
}
