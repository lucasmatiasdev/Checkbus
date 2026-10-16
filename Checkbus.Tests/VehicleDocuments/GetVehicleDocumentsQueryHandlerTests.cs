using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.VehicleDocuments.Queries;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.VehicleDocuments;

namespace Checkbus.Tests.VehicleDocuments;

public class GetVehicleDocumentsQueryHandlerTests
{
    private sealed class FakeVehicleRepository(Vehicle? vehicle) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(vehicle);

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentsQueryHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentsQueryHandler.");

        public Task<bool> PatentExistsAsync(string patent, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentsQueryHandler.");
    }

    private sealed class FakeVehicleDocumentRepository : IVehicleDocumentRepository
    {
        public IReadOnlyList<VehicleDocument> Documents { get; set; } = [];

        public Task<IReadOnlyList<VehicleDocument>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
            => Task.FromResult(Documents);

        public Task AddRangeAsync(IEnumerable<VehicleDocument> documents, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentsQueryHandler.");

        public Task AddAsync(VehicleDocument document, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentsQueryHandler.");

        public Task UpdateAsync(VehicleDocument document, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentsQueryHandler.");

        public Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentsQueryHandler.");

        public Task<bool> DocumentNumberExistsAsync(
            Guid organizationId, VehicleDocumentType type, string documentNumber, Guid excludeVehicleId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentsQueryHandler.");
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Dictionary<Guid, User> UsersById { get; } = [];

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(UsersById.GetValueOrDefault(id));

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentsQueryHandler.");

        public Task AddAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentsQueryHandler.");

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentsQueryHandler.");

        public Task<bool> DocumentNumberExistsInOrganizationAsync(
            string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentsQueryHandler.");

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(
            string localPartPrefix, string domain, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentsQueryHandler.");

        public Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDocumentsQueryHandler.");
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

    private static VehicleDocument CreateDocument(Guid vehicleId) => new()
    {
        Id = Guid.NewGuid(),
        VehicleId = vehicleId,
        Type = VehicleDocumentType.Seguro,
        Status = VehicleDocumentStatus.Apto,
        IssueDate = new DateOnly(2026, 1, 1),
        ExpirationDate = new DateOnly(2027, 1, 1),
        DocumentPresent = true,
        FileKey = "vehicle-documents/some/key.pdf",
        FileContentType = "application/pdf",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_SameOrganization_ReturnsExistingRowsOnly()
    {
        // Conditional types that were never uploaded must NOT be synthesized into the
        // response — only rows that actually exist in the repository are returned.
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var repository = new FakeVehicleDocumentRepository { Documents = [CreateDocument(vehicle.Id)] };
        var handler = new GetVehicleDocumentsQueryHandler(
            new FakeVehicleRepository(vehicle), repository, new FakeUserRepository(), new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(
            new GetVehicleDocumentsQuery { VehicleId = vehicle.Id }, TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal(VehicleDocumentType.Seguro, dto.Type);
        Assert.Equal(VehicleDocumentStatus.Apto, dto.Status);
        Assert.True(dto.DocumentPresent);
    }

    [Fact]
    public async Task Handle_ValidatedDocument_ResolvesValidatorDisplayName()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var validatorId = Guid.NewGuid();
        var validator = new User
        {
            Id = validatorId,
            Name = "Ana",
            Surname = "Gomez",
            Email = "ana.gomez@checkbus-demo.com",
            PasswordHash = "hash",
            DocumentNumber = "12345678",
            Role = Checkbus.ApiService.Domain.Authorization.Role.Administrador,
            OrganizationId = organizationId
        };
        var document = CreateDocument(vehicle.Id);
        document.ValidatedBy = validatorId;
        var repository = new FakeVehicleDocumentRepository { Documents = [document] };
        var userRepository = new FakeUserRepository();
        userRepository.UsersById[validatorId] = validator;
        var handler = new GetVehicleDocumentsQueryHandler(
            new FakeVehicleRepository(vehicle), repository, userRepository, new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(
            new GetVehicleDocumentsQuery { VehicleId = vehicle.Id }, TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal("Ana Gomez", dto.ValidatedByName);
    }

    [Fact]
    public async Task Handle_NeverValidatedDocument_ValidatedByNameIsNull()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var document = CreateDocument(vehicle.Id); // ValidatedBy left null
        var repository = new FakeVehicleDocumentRepository { Documents = [document] };
        var handler = new GetVehicleDocumentsQueryHandler(
            new FakeVehicleRepository(vehicle), repository, new FakeUserRepository(), new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(
            new GetVehicleDocumentsQuery { VehicleId = vehicle.Id }, TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Null(dto.ValidatedByName);
    }

    [Fact]
    public async Task Handle_CrossOrganizationVehicle_ThrowsVehicleNotFoundException()
    {
        var vehicle = CreateVehicle(Guid.NewGuid()); // different organization than the caller
        var repository = new FakeVehicleDocumentRepository();
        var handler = new GetVehicleDocumentsQueryHandler(
            new FakeVehicleRepository(vehicle), repository, new FakeUserRepository(), new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<VehicleNotFoundException>(() => handler.Handle(
            new GetVehicleDocumentsQuery { VehicleId = vehicle.Id }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_MissingVehicle_ThrowsVehicleNotFoundException()
    {
        var repository = new FakeVehicleDocumentRepository();
        var handler = new GetVehicleDocumentsQueryHandler(
            new FakeVehicleRepository(vehicle: null), repository, new FakeUserRepository(), new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<VehicleNotFoundException>(() => handler.Handle(
            new GetVehicleDocumentsQuery { VehicleId = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_ValidRequest_DtoExcludesFileKeyAndFileContentType()
    {
        // Compile-time proof: VehicleDocumentDto has no FileKey/FileContentType members at
        // all, so the file is never reachable except through the dedicated download
        // endpoint. Mirrors the equivalent DriverRequirementDto test.
        var dtoProperties = typeof(VehicleDocumentDto).GetProperties().Select(p => p.Name).ToArray();

        Assert.DoesNotContain("FileKey", dtoProperties);
        Assert.DoesNotContain("FileContentType", dtoProperties);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var repository = new FakeVehicleDocumentRepository();
        var handler = new GetVehicleDocumentsQueryHandler(
            new FakeVehicleRepository(vehicle: null), repository, new FakeUserRepository(), new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(
            new GetVehicleDocumentsQuery { VehicleId = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }
}
