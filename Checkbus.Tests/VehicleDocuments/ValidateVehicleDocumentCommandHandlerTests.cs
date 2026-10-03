using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.VehicleDocuments.Commands;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.VehicleDocuments;

namespace Checkbus.Tests.VehicleDocuments;

public class ValidateVehicleDocumentCommandHandlerTests
{
    private sealed class FakeVehicleRepository(Vehicle? vehicle) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(vehicle);

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateVehicleDocumentCommandHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateVehicleDocumentCommandHandler.");

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateVehicleDocumentCommandHandler.");
    }

    private sealed class FakeVehicleDocumentRepository : IVehicleDocumentRepository
    {
        public IReadOnlyList<VehicleDocument> Documents { get; set; } = [];
        public List<VehicleDocument> UpdatedDocuments { get; } = [];

        public Task<IReadOnlyList<VehicleDocument>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
            => Task.FromResult(Documents);

        public Task AddRangeAsync(IEnumerable<VehicleDocument> documents, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateVehicleDocumentCommandHandler.");

        public Task AddAsync(VehicleDocument document, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateVehicleDocumentCommandHandler.");

        public Task UpdateAsync(VehicleDocument document, CancellationToken cancellationToken)
        {
            UpdatedDocuments.Add(document);
            return Task.CompletedTask;
        }

        public Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by ValidateVehicleDocumentCommandHandler.");
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
        Status = VehicleDocumentStatus.Pendiente,
        DocumentPresent = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static ValidateVehicleDocumentCommand CreateCommand(Guid vehicleId, VehicleDocumentType type, bool approved) => new()
    {
        VehicleId = vehicleId,
        Type = type,
        Approved = approved
    };

    [Theory]
    [InlineData(true, VehicleDocumentStatus.Apto)]
    [InlineData(false, VehicleDocumentStatus.NoApto)]
    public async Task Handle_ExistingRow_SetsExpectedStatus(bool approved, VehicleDocumentStatus expectedStatus)
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var document = CreateDocument(vehicle.Id, VehicleDocumentType.Seguro);
        var repository = new FakeVehicleDocumentRepository { Documents = [document] };
        var handler = new ValidateVehicleDocumentCommandHandler(
            new FakeVehicleRepository(vehicle), repository, new FakeCurrentUserService(organizationId));

        await handler.Handle(CreateCommand(vehicle.Id, VehicleDocumentType.Seguro, approved), TestContext.Current.CancellationToken);

        var updated = Assert.Single(repository.UpdatedDocuments);
        Assert.Equal(expectedStatus, updated.Status);
    }

    [Fact]
    public async Task Handle_CrossOrganizationVehicle_ThrowsVehicleNotFoundException()
    {
        var vehicle = CreateVehicle(Guid.NewGuid()); // different organization than the caller
        var repository = new FakeVehicleDocumentRepository();
        var handler = new ValidateVehicleDocumentCommandHandler(
            new FakeVehicleRepository(vehicle), repository, new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<VehicleNotFoundException>(() => handler.Handle(
            CreateCommand(vehicle.Id, VehicleDocumentType.Seguro, approved: true), TestContext.Current.CancellationToken));

        Assert.Empty(repository.UpdatedDocuments);
    }

    [Fact]
    public async Task Handle_MissingVehicle_ThrowsVehicleNotFoundException()
    {
        var repository = new FakeVehicleDocumentRepository();
        var handler = new ValidateVehicleDocumentCommandHandler(
            new FakeVehicleRepository(vehicle: null), repository, new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<VehicleNotFoundException>(() => handler.Handle(
            CreateCommand(Guid.NewGuid(), VehicleDocumentType.Seguro, approved: true), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ConditionalTypeNeverUploaded_ThrowsVehicleDocumentNotFoundException()
    {
        // Unlike upload, validate must NOT auto-create: validating a conditional document
        // type that was never submitted is a genuine error, distinct from the vehicle
        // itself being missing/cross-tenant.
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var repository = new FakeVehicleDocumentRepository(); // no rows at all
        var handler = new ValidateVehicleDocumentCommandHandler(
            new FakeVehicleRepository(vehicle), repository, new FakeCurrentUserService(organizationId));

        await Assert.ThrowsAsync<VehicleDocumentNotFoundException>(() => handler.Handle(
            CreateCommand(vehicle.Id, VehicleDocumentType.TituloPropiedad, approved: true), TestContext.Current.CancellationToken));

        Assert.Empty(repository.UpdatedDocuments);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var repository = new FakeVehicleDocumentRepository();
        var handler = new ValidateVehicleDocumentCommandHandler(
            new FakeVehicleRepository(vehicle: null), repository, new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            CreateCommand(Guid.NewGuid(), VehicleDocumentType.Seguro, approved: true), TestContext.Current.CancellationToken));
    }
}
