using System.ComponentModel.DataAnnotations;
using Checkbus.Web.Contracts;
using Checkbus.Web.Models;

namespace Checkbus.Tests.Web;

/// <summary>
/// Validates <see cref="RegisterUserFormModel"/> using plain <see cref="Validator"/>
/// DataAnnotations checks (no FluentValidation, no bUnit) — this project keeps Checkbus.Web
/// validation logic in plain testable classes per this repo's convention.
/// </summary>
public class RegisterUserFormModelTests
{
    private static RegisterUserFormModel CreateValidModel() => new()
    {
        Name = "Jose",
        Surname = "Diaz",
        DocumentType = WebDocumentType.DNI,
        DocumentNumber = "12345678",
        Role = WebRole.Chofer
    };

    private static IList<ValidationResult> Validate(RegisterUserFormModel model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Validate_FullyValidModel_ReturnsNoResults()
    {
        var model = CreateValidModel();

        var results = Validate(model);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_EmptyName_FailsValidation()
    {
        var model = CreateValidModel();
        model.Name = string.Empty;

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterUserFormModel.Name)));
    }

    [Fact]
    public void Validate_NameExceeds100Characters_FailsValidation()
    {
        var model = CreateValidModel();
        model.Name = new string('a', 101);

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterUserFormModel.Name)));
    }

    [Fact]
    public void Validate_EmptyDocumentNumber_FailsValidation()
    {
        var model = CreateValidModel();
        model.DocumentNumber = string.Empty;

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterUserFormModel.DocumentNumber)));
    }

    [Theory]
    [InlineData("1234-5678")]
    [InlineData("1234 5678")]
    public void Validate_DocumentNumberWithNonAlphanumericCharacters_FailsValidation(string documentNumber)
    {
        var model = CreateValidModel();
        model.DocumentNumber = documentNumber;

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterUserFormModel.DocumentNumber)));
    }

    [Fact]
    public void Validate_DocumentNumberExceeds20Characters_FailsValidation()
    {
        var model = CreateValidModel();
        model.DocumentNumber = new string('1', 21);

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterUserFormModel.DocumentNumber)));
    }

    [Fact]
    public void Validate_NullRole_FailsValidation()
    {
        var model = CreateValidModel();
        model.Role = null;

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterUserFormModel.Role)));
    }

    [Fact]
    public void AssignableRoles_ContainsExactlyNonAdministradorRoles()
    {
        Assert.Equal(3, RegisterUserFormModel.AssignableRoles.Count);
        Assert.Contains(WebRole.Chofer, RegisterUserFormModel.AssignableRoles);
        Assert.Contains(WebRole.Planificador, RegisterUserFormModel.AssignableRoles);
        Assert.Contains(WebRole.Mecanico, RegisterUserFormModel.AssignableRoles);
        Assert.DoesNotContain(WebRole.Administrador, RegisterUserFormModel.AssignableRoles);
    }

    [Fact]
    public void ToRequest_ValidModel_MapsAllFieldsToRequest()
    {
        var model = new RegisterUserFormModel
        {
            Name = "Maria",
            Surname = "Gonzalez",
            DocumentType = WebDocumentType.Pasaporte,
            DocumentNumber = "AB123456",
            Role = WebRole.Planificador
        };

        var request = model.ToRequest();

        Assert.Equal(model.Name, request.Name);
        Assert.Equal(model.Surname, request.Surname);
        Assert.Equal(model.DocumentType, request.DocumentType);
        Assert.Equal(model.DocumentNumber, request.DocumentNumber);
        Assert.Equal(model.Role, request.Role);
    }
}
