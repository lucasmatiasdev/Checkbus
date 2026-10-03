using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.MaintenanceRecords.Commands;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.VehicleDocuments;

namespace Checkbus.Tests.MaintenanceRecords;

public class CreateMaintenanceRecordCommandHandlerTests
{
    private sealed class FakeVehicleRepository(Vehicle? vehicle) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(vehicle);

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateMaintenanceRecordCommandHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateMaintenanceRecordCommandHandler.");

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateMaintenanceRecordCommandHandler.");
    }

    private sealed class FakeMaintenanceRecordRepository : IMaintenanceRecordRepository
    {
        public List<MaintenanceRecord> AddedRecords { get; } = [];

        public Task<MaintenanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateMaintenanceRecordCommandHandler.");

        public Task AddAsync(MaintenanceRecord record, CancellationToken cancellationToken)
        {
            AddedRecords.Add(record);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(MaintenanceRecord record, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateMaintenanceRecordCommandHandler.");

        public Task<IReadOnlyList<MaintenanceRecord>> GetByVehicleIdsAsync(IEnumerable<Guid> vehicleIds, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateMaintenanceRecordCommandHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Mecanico";
        public string? Email => "mecanico@checkbus-demo.com";
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

    private static CreateMaintenanceRecordCommand CreateCommand(Guid vehicleId) => new()
    {
        VehicleId = vehicleId,
        Type = MaintenanceType.Preventivo,
        ScheduledDate = new DateOnly(2026, 1, 1),
        Description = "Cambio de aceite"
    };

    [Fact]
    public async Task Handle_SameOrganization_CreatesRecordWithProgramadoStatus()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var repository = new FakeMaintenanceRecordRepository();
        var handler = new CreateMaintenanceRecordCommandHandler(
            new FakeVehicleRepository(vehicle), repository, new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(CreateCommand(vehicle.Id), TestContext.Current.CancellationToken);

        var created = Assert.Single(repository.AddedRecords);
        Assert.Equal(vehicle.Id, created.VehicleId);
        Assert.Equal(MaintenanceType.Preventivo, created.Type);
        Assert.Equal(MaintenanceStatus.Programado, created.Status);
        Assert.Equal("Cambio de aceite", created.Description);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal(MaintenanceStatus.Programado, result.Status);
    }

    [Fact]
    public async Task Handle_CrossOrganizationVehicle_ThrowsVehicleNotFoundException()
    {
        var vehicle = CreateVehicle(Guid.NewGuid()); // different organization than the caller
        var repository = new FakeMaintenanceRecordRepository();
        var handler = new CreateMaintenanceRecordCommandHandler(
            new FakeVehicleRepository(vehicle), repository, new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<VehicleNotFoundException>(
            () => handler.Handle(CreateCommand(vehicle.Id), TestContext.Current.CancellationToken));

        Assert.Empty(repository.AddedRecords);
    }

    [Fact]
    public async Task Handle_MissingVehicle_ThrowsVehicleNotFoundException()
    {
        var repository = new FakeMaintenanceRecordRepository();
        var handler = new CreateMaintenanceRecordCommandHandler(
            new FakeVehicleRepository(vehicle: null), repository, new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<VehicleNotFoundException>(
            () => handler.Handle(CreateCommand(Guid.NewGuid()), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var repository = new FakeMaintenanceRecordRepository();
        var handler = new CreateMaintenanceRecordCommandHandler(
            new FakeVehicleRepository(vehicle: null), repository, new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(Guid.NewGuid()), TestContext.Current.CancellationToken));
    }
}
