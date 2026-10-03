using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Vehicles.Commands;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Vehicles;

namespace Checkbus.Tests.Vehicles;

public class CreateVehicleCommandHandlerTests
{
    private sealed class FakeVehicleRepository : IVehicleRepository
    {
        public List<Vehicle> AddedVehicles { get; } = [];
        public bool PatentExists { get; set; }

        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleCommandHandler.");

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
        {
            AddedVehicles.Add(vehicle);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleCommandHandler.");

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
            => Task.FromResult(PatentExists);
    }

    private sealed class FakeVehicleDocumentRepository : IVehicleDocumentRepository
    {
        public List<VehicleDocument> AddedDocuments { get; } = [];

        public Task<IReadOnlyList<VehicleDocument>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleCommandHandler.");

        public Task AddRangeAsync(IEnumerable<VehicleDocument> documents, CancellationToken cancellationToken)
        {
            AddedDocuments.AddRange(documents);
            return Task.CompletedTask;
        }

        public Task AddAsync(VehicleDocument document, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleCommandHandler.");

        public Task UpdateAsync(VehicleDocument document, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleCommandHandler.");

        public Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleCommandHandler.");
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? UserToReturn { get; set; }

        public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleCommandHandler.");

        public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(UserToReturn);

        public Task AddAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleCommandHandler.");

        public Task UpdateAsync(User user, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleCommandHandler.");

        public Task<bool> DocumentNumberExistsInOrganizationAsync(
            string documentNumber, Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleCommandHandler.");

        public Task<IReadOnlyList<string>> FindEmailsByLocalPartPrefixAsync(
            string localPartPrefix, string domain, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleCommandHandler.");

        public Task<IReadOnlyList<User>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleCommandHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Administrador";
        public string? Email => "admin@checkbus-demo.com";
    }

    private static User CreateChoferUser(Guid organizationId) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Jose",
        Surname = "Diaz",
        Email = "jose.diaz@checkbus-demo.com",
        PasswordHash = "hashed-password",
        DocumentNumber = "40123456",
        Role = Role.Chofer,
        OrganizationId = organizationId,
        IsActive = true
    };

    private static CreateVehicleCommand CreateOrganizacionCommand() => new()
    {
        Brand = "Mercedes-Benz",
        Model = "O500",
        Year = 2020,
        Patent = "AB123CD",
        Capacity = 45,
        Mileage = 1000,
        Status = VehicleStatus.Activo,
        OwnerType = VehicleOwnerType.Organizacion
    };

    private static readonly Guid DefaultOrganizationId = Guid.NewGuid();

    private static CreateVehicleCommandHandler CreateHandler(
        FakeVehicleRepository? vehicleRepository = null,
        FakeVehicleDocumentRepository? vehicleDocumentRepository = null,
        FakeUserRepository? userRepository = null,
        bool hasOrganizationIdOverride = false,
        Guid? organizationIdOverride = null) => new(
            vehicleRepository ?? new FakeVehicleRepository(),
            vehicleDocumentRepository ?? new FakeVehicleDocumentRepository(),
            userRepository ?? new FakeUserRepository(),
            new FakeCurrentUserService(hasOrganizationIdOverride ? organizationIdOverride : DefaultOrganizationId));

    [Fact]
    public async Task Handle_OwnerTypeOrganizacion_CreatesVehicleWithNoOwnerFields()
    {
        var organizationId = Guid.NewGuid();
        var vehicleRepository = new FakeVehicleRepository();
        var handler = CreateHandler(vehicleRepository: vehicleRepository, hasOrganizationIdOverride: true, organizationIdOverride: organizationId);

        var result = await handler.Handle(CreateOrganizacionCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(VehicleOwnerType.Organizacion, result.OwnerType);
        Assert.Null(result.OwnerUserId);
        Assert.Null(result.OwnerName);
        Assert.Null(result.OwnerDocumentNumber);
        Assert.Equal(organizationId, result.OrganizationId);
        Assert.Single(vehicleRepository.AddedVehicles);
    }

    [Fact]
    public async Task Handle_OwnerTypeChofer_ResolvesAgainstExistingOrgScopedChoferUser()
    {
        var organizationId = Guid.NewGuid();
        var choferUser = CreateChoferUser(organizationId);
        var userRepository = new FakeUserRepository { UserToReturn = choferUser };
        var handler = CreateHandler(userRepository: userRepository, hasOrganizationIdOverride: true, organizationIdOverride: organizationId);
        var command = CreateOrganizacionCommand();
        command.OwnerType = VehicleOwnerType.Chofer;
        command.OwnerUserId = choferUser.Id;

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(VehicleOwnerType.Chofer, result.OwnerType);
        Assert.Equal(choferUser.Id, result.OwnerUserId);
        Assert.Null(result.OwnerName);
        Assert.Null(result.OwnerDocumentNumber);
    }

    [Fact]
    public async Task Handle_OwnerTypeOtro_PersistsNameAndDocumentNumber()
    {
        var handler = CreateHandler();
        var command = CreateOrganizacionCommand();
        command.OwnerType = VehicleOwnerType.Otro;
        command.OwnerName = "Juan Perez";
        command.OwnerDocumentNumber = "30111222";

        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        Assert.Equal(VehicleOwnerType.Otro, result.OwnerType);
        Assert.Null(result.OwnerUserId);
        Assert.Equal("Juan Perez", result.OwnerName);
        Assert.Equal("30111222", result.OwnerDocumentNumber);
    }

    [Fact]
    public async Task Handle_AnyOwnerType_CreatesExactlyTwoUniversalDocumentsPendienteNoDates()
    {
        var vehicleDocumentRepository = new FakeVehicleDocumentRepository();
        var handler = CreateHandler(vehicleDocumentRepository: vehicleDocumentRepository);

        var result = await handler.Handle(CreateOrganizacionCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(2, vehicleDocumentRepository.AddedDocuments.Count);
        Assert.All(vehicleDocumentRepository.AddedDocuments, d =>
        {
            Assert.Equal(result.VehicleId, d.VehicleId);
            Assert.Equal(VehicleDocumentStatus.Pendiente, d.Status);
            Assert.False(d.DocumentPresent);
            Assert.Null(d.IssueDate);
            Assert.Null(d.ExpirationDate);
        });
        Assert.Contains(vehicleDocumentRepository.AddedDocuments, d => d.Type == VehicleDocumentType.Seguro);
        Assert.Contains(vehicleDocumentRepository.AddedDocuments, d => d.Type == VehicleDocumentType.RTO_VTV);
        Assert.DoesNotContain(vehicleDocumentRepository.AddedDocuments, d => d.Type == VehicleDocumentType.TituloPropiedad);
        Assert.DoesNotContain(vehicleDocumentRepository.AddedDocuments, d => d.Type == VehicleDocumentType.LeasingInscripto);
        Assert.DoesNotContain(vehicleDocumentRepository.AddedDocuments, d => d.Type == VehicleDocumentType.ContratoAlquiler);
        Assert.DoesNotContain(vehicleDocumentRepository.AddedDocuments, d => d.Type == VehicleDocumentType.HabilitacionEspecifica);
    }

    [Fact]
    public async Task Handle_OwnerTypeChofer_OwnerUserDoesNotExist_ThrowsVehicleOwnerNotFoundException()
    {
        var userRepository = new FakeUserRepository { UserToReturn = null };
        var vehicleRepository = new FakeVehicleRepository();
        var handler = CreateHandler(vehicleRepository: vehicleRepository, userRepository: userRepository);
        var command = CreateOrganizacionCommand();
        command.OwnerType = VehicleOwnerType.Chofer;
        command.OwnerUserId = Guid.NewGuid();

        await Assert.ThrowsAsync<VehicleOwnerNotFoundException>(
            () => handler.Handle(command, TestContext.Current.CancellationToken));

        Assert.Empty(vehicleRepository.AddedVehicles);
    }

    [Fact]
    public async Task Handle_OwnerTypeChofer_OwnerUserIsNotChofer_ThrowsVehicleOwnerNotFoundException()
    {
        var organizationId = Guid.NewGuid();
        var nonChoferUser = CreateChoferUser(organizationId);
        nonChoferUser.Role = Role.Planificador;
        var userRepository = new FakeUserRepository { UserToReturn = nonChoferUser };
        var handler = CreateHandler(userRepository: userRepository, hasOrganizationIdOverride: true, organizationIdOverride: organizationId);
        var command = CreateOrganizacionCommand();
        command.OwnerType = VehicleOwnerType.Chofer;
        command.OwnerUserId = nonChoferUser.Id;

        await Assert.ThrowsAsync<VehicleOwnerNotFoundException>(
            () => handler.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_OwnerTypeChofer_OwnerUserInDifferentOrganization_ThrowsVehicleOwnerNotFoundException()
    {
        // IDOR-class check: an Administrador in org A must not be able to attach a Chofer
        // from org B as a vehicle owner, even though the user genuinely exists and has the
        // right role.
        var callerOrganizationId = Guid.NewGuid();
        var otherOrganizationChofer = CreateChoferUser(Guid.NewGuid());
        var userRepository = new FakeUserRepository { UserToReturn = otherOrganizationChofer };
        var handler = CreateHandler(userRepository: userRepository, hasOrganizationIdOverride: true, organizationIdOverride: callerOrganizationId);
        var command = CreateOrganizacionCommand();
        command.OwnerType = VehicleOwnerType.Chofer;
        command.OwnerUserId = otherOrganizationChofer.Id;

        await Assert.ThrowsAsync<VehicleOwnerNotFoundException>(
            () => handler.Handle(command, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_DuplicatePatent_ThrowsPatentAlreadyRegisteredException()
    {
        var vehicleRepository = new FakeVehicleRepository { PatentExists = true };
        var vehicleDocumentRepository = new FakeVehicleDocumentRepository();
        var handler = CreateHandler(vehicleRepository: vehicleRepository, vehicleDocumentRepository: vehicleDocumentRepository);

        await Assert.ThrowsAsync<PatentAlreadyRegisteredException>(
            () => handler.Handle(CreateOrganizacionCommand(), TestContext.Current.CancellationToken));

        Assert.Empty(vehicleRepository.AddedVehicles);
        Assert.Empty(vehicleDocumentRepository.AddedDocuments);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = CreateHandler(hasOrganizationIdOverride: true, organizationIdOverride: null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateOrganizacionCommand(), TestContext.Current.CancellationToken));
    }
}
