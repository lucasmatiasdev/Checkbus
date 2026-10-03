using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.VehicleDocuments.Queries;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.VehicleDocuments;

namespace Checkbus.Tests.VehicleDocuments;

public class GetVehicleDocumentQueryHandlerTests
{
    private sealed class FakeVehicleRepository(Vehicle? vehicle) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(vehicle);

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentQueryHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentQueryHandler.");

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentQueryHandler.");
    }

    private sealed class FakeVehicleDocumentRepository : IVehicleDocumentRepository
    {
        public IReadOnlyList<VehicleDocument> Documents { get; set; } = [];

        public Task<IReadOnlyList<VehicleDocument>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
            => Task.FromResult(Documents);

        public Task AddRangeAsync(IEnumerable<VehicleDocument> documents, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentQueryHandler.");

        public Task AddAsync(VehicleDocument document, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentQueryHandler.");

        public Task UpdateAsync(VehicleDocument document, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentQueryHandler.");

        public Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentQueryHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Administrador";
        public string? Email => "admin@checkbus-demo.com";
    }

    private static Vehicle CreateVehicle(Guid organizationId) => new()
    {
        Id = Guid.NewGuid(),
        Brand = "Mercedes-Benz",
        Model = "O500",
        Year = 2020,
        Patent = "AB123CD",
        Capacity = 45,
        Mileage = 1000,
        Status = VehicleStatus.Activo,
        OrganizationId = organizationId,
        OwnerType = VehicleOwnerType.Organizacion
    };

    private static VehicleDocument CreateDocument(Guid vehicleId, VehicleDocumentType type) => new()
    {
        Id = Guid.NewGuid(),
        VehicleId = vehicleId,
        Type = type,
        Status = VehicleDocumentStatus.Apto,
        DocumentPresent = true,
        FileKey = "vehicle-documents/some/key.pdf",
        FileContentType = "application/pdf",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_ExistingRow_ReturnsFileKeyAndContentType()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var document = CreateDocument(vehicle.Id, VehicleDocumentType.Seguro);
        var repository = new FakeVehicleDocumentRepository { Documents = [document] };
        var handler = new GetVehicleDocumentQueryHandler(
            new FakeVehicleRepository(vehicle), repository, new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(
            new GetVehicleDocumentQuery { VehicleId = vehicle.Id, Type = VehicleDocumentType.Seguro },
            TestContext.Current.CancellationToken);

        Assert.Equal(document.FileKey, result.FileKey);
        Assert.Equal(document.FileContentType, result.FileContentType);
    }

    [Fact]
    public async Task Handle_CrossOrganizationVehicle_ThrowsVehicleNotFoundException()
    {
        var vehicle = CreateVehicle(Guid.NewGuid()); // different organization than the caller
        var repository = new FakeVehicleDocumentRepository();
        var handler = new GetVehicleDocumentQueryHandler(
            new FakeVehicleRepository(vehicle), repository, new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<VehicleNotFoundException>(() => handler.Handle(
            new GetVehicleDocumentQuery { VehicleId = vehicle.Id, Type = VehicleDocumentType.Seguro },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_MissingVehicle_ThrowsVehicleNotFoundException()
    {
        var repository = new FakeVehicleDocumentRepository();
        var handler = new GetVehicleDocumentQueryHandler(
            new FakeVehicleRepository(vehicle: null), repository, new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<VehicleNotFoundException>(() => handler.Handle(
            new GetVehicleDocumentQuery { VehicleId = Guid.NewGuid(), Type = VehicleDocumentType.Seguro },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_TypeNeverUploaded_ThrowsVehicleDocumentNotFoundException()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var repository = new FakeVehicleDocumentRepository(); // no rows at all
        var handler = new GetVehicleDocumentQueryHandler(
            new FakeVehicleRepository(vehicle), repository, new FakeCurrentUserService(organizationId));

        await Assert.ThrowsAsync<VehicleDocumentNotFoundException>(() => handler.Handle(
            new GetVehicleDocumentQuery { VehicleId = vehicle.Id, Type = VehicleDocumentType.TituloPropiedad },
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var repository = new FakeVehicleDocumentRepository();
        var handler = new GetVehicleDocumentQueryHandler(
            new FakeVehicleRepository(vehicle: null), repository, new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new GetVehicleDocumentQuery { VehicleId = Guid.NewGuid(), Type = VehicleDocumentType.Seguro },
            TestContext.Current.CancellationToken));
    }
}
