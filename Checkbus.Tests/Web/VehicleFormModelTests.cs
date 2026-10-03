using System.ComponentModel.DataAnnotations;
using Checkbus.Web.Contracts;
using Checkbus.Web.Models;

namespace Checkbus.Tests.Web;

/// <summary>
/// Validates <see cref="VehicleFormModel"/> using plain <see cref="Validator"/> DataAnnotations
/// checks (no FluentValidation, no bUnit) — this project keeps Checkbus.Web validation logic in
/// plain testable classes per this repo's convention. Scrutinizes <see cref="VehicleFormModel.ToRequest"/>
/// hardest: it must null out whichever owner sub-fields don't apply to the selected
/// <see cref="WebVehicleOwnerType"/>, since the server validator rejects a request carrying owner
/// fields that don't match its <c>OwnerType</c>.
/// </summary>
public class VehicleFormModelTests
{
    private static VehicleFormModel CreateValidModel() => new()
    {
        Brand = "Mercedes-Benz",
        Model = "Sprinter",
        Year = 2020,
        Patent = "AB123CD",
        Capacity = 20,
        Mileage = 1000,
        Status = WebVehicleStatus.Activo,
        OwnerType = WebVehicleOwnerType.Organizacion
    };

    private static IList<ValidationResult> Validate(VehicleFormModel model)
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
    public void Validate_EmptyBrand_FailsValidation()
    {
        var model = CreateValidModel();
        model.Brand = string.Empty;

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(VehicleFormModel.Brand)));
    }

    [Fact]
    public void Validate_EmptyModel_FailsValidation()
    {
        var model = CreateValidModel();
        model.Model = string.Empty;

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(VehicleFormModel.Model)));
    }

    [Fact]
    public void Validate_EmptyPatent_FailsValidation()
    {
        var model = CreateValidModel();
        model.Patent = string.Empty;

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(VehicleFormModel.Patent)));
    }

    [Fact]
    public void Validate_YearBelowMinimum_FailsValidation()
    {
        var model = CreateValidModel();
        model.Year = 1979;

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(VehicleFormModel.Year)));
    }

    [Fact]
    public void Validate_NegativeCapacity_FailsValidation()
    {
        var model = CreateValidModel();
        model.Capacity = -1;

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(VehicleFormModel.Capacity)));
    }

    [Fact]
    public void Validate_NegativeMileage_FailsValidation()
    {
        var model = CreateValidModel();
        model.Mileage = -1;

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(VehicleFormModel.Mileage)));
    }

    [Fact]
    public void ToRequest_OwnerTypeOrganizacion_NullsAllOwnerFields()
    {
        var model = CreateValidModel();
        model.OwnerType = WebVehicleOwnerType.Organizacion;
        model.OwnerUserId = Guid.NewGuid();
        model.OwnerName = "Leftover name";
        model.OwnerDocumentNumber = "Leftover doc";

        var request = model.ToRequest();

        Assert.Equal(WebVehicleOwnerType.Organizacion, request.OwnerType);
        Assert.Null(request.OwnerUserId);
        Assert.Null(request.OwnerName);
        Assert.Null(request.OwnerDocumentNumber);
    }

    [Fact]
    public void ToRequest_OwnerTypeChofer_KeepsOwnerUserIdAndNullsOtherOwnerFields()
    {
        var model = CreateValidModel();
        model.OwnerType = WebVehicleOwnerType.Chofer;
        var choferId = Guid.NewGuid();
        model.OwnerUserId = choferId;
        model.OwnerName = "Leftover name";
        model.OwnerDocumentNumber = "Leftover doc";

        var request = model.ToRequest();

        Assert.Equal(WebVehicleOwnerType.Chofer, request.OwnerType);
        Assert.Equal(choferId, request.OwnerUserId);
        Assert.Null(request.OwnerName);
        Assert.Null(request.OwnerDocumentNumber);
    }

    [Fact]
    public void ToRequest_OwnerTypeOtro_KeepsNameAndDocumentNumberAndNullsOwnerUserId()
    {
        var model = CreateValidModel();
        model.OwnerType = WebVehicleOwnerType.Otro;
        model.OwnerUserId = Guid.NewGuid();
        model.OwnerName = "Juan Perez";
        model.OwnerDocumentNumber = "30123456";

        var request = model.ToRequest();

        Assert.Equal(WebVehicleOwnerType.Otro, request.OwnerType);
        Assert.Null(request.OwnerUserId);
        Assert.Equal("Juan Perez", request.OwnerName);
        Assert.Equal("30123456", request.OwnerDocumentNumber);
    }

    [Fact]
    public void ToRequest_ValidModel_MapsAllBaseFieldsToRequest()
    {
        var model = CreateValidModel();

        var request = model.ToRequest();

        Assert.Equal(model.Brand, request.Brand);
        Assert.Equal(model.Model, request.Model);
        Assert.Equal(model.Year, request.Year);
        Assert.Equal(model.Patent, request.Patent);
        Assert.Equal(model.Capacity, request.Capacity);
        Assert.Equal(model.Mileage, request.Mileage);
        Assert.Equal(model.Status, request.Status);
    }
}
