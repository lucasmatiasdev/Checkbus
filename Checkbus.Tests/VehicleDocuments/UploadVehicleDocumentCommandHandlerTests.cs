using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.Application.VehicleDocuments.Commands;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.VehicleDocuments;

namespace Checkbus.Tests.VehicleDocuments;

public class UploadVehicleDocumentCommandHandlerTests
{
    private sealed class FakeVehicleRepository(Vehicle? vehicle) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(vehicle);

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UploadVehicleDocumentCommandHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UploadVehicleDocumentCommandHandler.");

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UploadVehicleDocumentCommandHandler.");
    }

    private sealed class FakeVehicleDocumentRepository : IVehicleDocumentRepository
    {
        public IReadOnlyList<VehicleDocument> Documents { get; set; } = [];
        public List<VehicleDocument> AddedDocuments { get; } = [];
        public List<VehicleDocument> UpdatedDocuments { get; } = [];

        public Task<IReadOnlyList<VehicleDocument>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
            => Task.FromResult(Documents);

        public Task AddRangeAsync(IEnumerable<VehicleDocument> documents, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UploadVehicleDocumentCommandHandler.");

        public Task AddAsync(VehicleDocument document, CancellationToken cancellationToken)
        {
            AddedDocuments.Add(document);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(VehicleDocument document, CancellationToken cancellationToken)
        {
            UpdatedDocuments.Add(document);
            return Task.CompletedTask;
        }

        public Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UploadVehicleDocumentCommandHandler.");
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public List<string> SavedKeys { get; } = [];
        public List<string> DeletedKeys { get; } = [];

        public Task<string> SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
        {
            SavedKeys.Add(key);
            return Task.FromResult(key);
        }

        public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UploadVehicleDocumentCommandHandler.");

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UploadVehicleDocumentCommandHandler.");

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            DeletedKeys.Add(key);
            return Task.CompletedTask;
        }
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

    private static VehicleDocument CreateDocument(Guid vehicleId, VehicleDocumentType type, VehicleDocumentStatus status, string? fileKey = null) => new()
    {
        Id = Guid.NewGuid(),
        VehicleId = vehicleId,
        Type = type,
        Status = status,
        DocumentPresent = fileKey is not null,
        FileKey = fileKey,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static UploadVehicleDocumentCommand CreateCommand(Guid vehicleId, VehicleDocumentType type) => new()
    {
        VehicleId = vehicleId,
        Type = type,
        FileStream = new MemoryStream([1, 2, 3]),
        ContentType = "application/pdf",
        FileSizeBytes = 3,
        OriginalFileName = "seguro.pdf",
        ExpirationDate = new DateOnly(2027, 1, 1),
        IssueDate = new DateOnly(2026, 1, 1)
    };

    [Fact]
    public async Task Handle_UniversalType_ExistingRow_UpdatesAndResetsStatusToPendiente()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var document = CreateDocument(vehicle.Id, VehicleDocumentType.Seguro, VehicleDocumentStatus.Apto);
        var vehicleDocumentRepository = new FakeVehicleDocumentRepository { Documents = [document] };
        var fileStorage = new FakeFileStorageService();
        var handler = new UploadVehicleDocumentCommandHandler(
            new FakeVehicleRepository(vehicle), vehicleDocumentRepository, fileStorage, new FakeCurrentUserService(organizationId));

        await handler.Handle(CreateCommand(vehicle.Id, VehicleDocumentType.Seguro), TestContext.Current.CancellationToken);

        Assert.Empty(vehicleDocumentRepository.AddedDocuments);
        var updated = Assert.Single(vehicleDocumentRepository.UpdatedDocuments);
        Assert.Same(document, updated);
        Assert.Equal(VehicleDocumentStatus.Pendiente, updated.Status);
        Assert.True(updated.DocumentPresent);
        Assert.Equal(new DateOnly(2026, 1, 1), updated.IssueDate);
        Assert.Equal(new DateOnly(2027, 1, 1), updated.ExpirationDate);
        Assert.NotNull(updated.FileKey);
        Assert.Equal("application/pdf", updated.FileContentType);
        Assert.Single(fileStorage.SavedKeys);
    }

    [Fact]
    public async Task Handle_ConditionalType_NoExistingRow_CreatesNewRow()
    {
        // This is the key new behavior vs. the driver-documents precedent: conditional
        // types (TituloPropiedad, LeasingInscripto, ContratoAlquiler, HabilitacionEspecifica)
        // have no pre-created row, so the first upload must get-or-create it rather than
        // assuming the row already exists (which would be a data invariant violation for
        // driver requirements, but is the normal, expected path here).
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var vehicleDocumentRepository = new FakeVehicleDocumentRepository(); // no rows at all
        var fileStorage = new FakeFileStorageService();
        var handler = new UploadVehicleDocumentCommandHandler(
            new FakeVehicleRepository(vehicle), vehicleDocumentRepository, fileStorage, new FakeCurrentUserService(organizationId));

        await handler.Handle(CreateCommand(vehicle.Id, VehicleDocumentType.TituloPropiedad), TestContext.Current.CancellationToken);

        Assert.Empty(vehicleDocumentRepository.UpdatedDocuments);
        var created = Assert.Single(vehicleDocumentRepository.AddedDocuments);
        Assert.Equal(vehicle.Id, created.VehicleId);
        Assert.Equal(VehicleDocumentType.TituloPropiedad, created.Type);
        Assert.Equal(VehicleDocumentStatus.Pendiente, created.Status);
        Assert.True(created.DocumentPresent);
        Assert.NotNull(created.FileKey);
        Assert.Single(fileStorage.SavedKeys);
    }

    [Fact]
    public async Task Handle_CrossOrganizationVehicle_ThrowsVehicleNotFoundException()
    {
        var vehicle = CreateVehicle(Guid.NewGuid()); // different organization than the caller
        var vehicleDocumentRepository = new FakeVehicleDocumentRepository();
        var fileStorage = new FakeFileStorageService();
        var handler = new UploadVehicleDocumentCommandHandler(
            new FakeVehicleRepository(vehicle), vehicleDocumentRepository, fileStorage, new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<VehicleNotFoundException>(
            () => handler.Handle(CreateCommand(vehicle.Id, VehicleDocumentType.Seguro), TestContext.Current.CancellationToken));

        Assert.Empty(vehicleDocumentRepository.AddedDocuments);
        Assert.Empty(vehicleDocumentRepository.UpdatedDocuments);
        Assert.Empty(fileStorage.SavedKeys);
    }

    [Fact]
    public async Task Handle_MissingVehicle_ThrowsVehicleNotFoundException()
    {
        var vehicleDocumentRepository = new FakeVehicleDocumentRepository();
        var fileStorage = new FakeFileStorageService();
        var handler = new UploadVehicleDocumentCommandHandler(
            new FakeVehicleRepository(vehicle: null), vehicleDocumentRepository, fileStorage, new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<VehicleNotFoundException>(
            () => handler.Handle(CreateCommand(Guid.NewGuid(), VehicleDocumentType.Seguro), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ReUpload_DeletesOldFileKey()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var document = CreateDocument(
            vehicle.Id, VehicleDocumentType.Seguro, VehicleDocumentStatus.Apto, fileKey: "vehicle-documents/old-key.pdf");
        var vehicleDocumentRepository = new FakeVehicleDocumentRepository { Documents = [document] };
        var fileStorage = new FakeFileStorageService();
        var handler = new UploadVehicleDocumentCommandHandler(
            new FakeVehicleRepository(vehicle), vehicleDocumentRepository, fileStorage, new FakeCurrentUserService(organizationId));

        await handler.Handle(CreateCommand(vehicle.Id, VehicleDocumentType.Seguro), TestContext.Current.CancellationToken);

        Assert.Contains("vehicle-documents/old-key.pdf", fileStorage.DeletedKeys);
        Assert.Single(fileStorage.SavedKeys);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var vehicleDocumentRepository = new FakeVehicleDocumentRepository();
        var fileStorage = new FakeFileStorageService();
        var handler = new UploadVehicleDocumentCommandHandler(
            new FakeVehicleRepository(vehicle: null), vehicleDocumentRepository, fileStorage, new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(Guid.NewGuid(), VehicleDocumentType.Seguro), TestContext.Current.CancellationToken));
    }
}
