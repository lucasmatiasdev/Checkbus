using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.MaintenanceRecords.Commands;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Maintenance;

namespace Checkbus.Tests.MaintenanceRecords;

public class UpdateMaintenanceRecordCommandHandlerTests
{
    private sealed class FakeVehicleRepository(Vehicle? vehicle) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(vehicle);

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateMaintenanceRecordCommandHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateMaintenanceRecordCommandHandler.");

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateMaintenanceRecordCommandHandler.");
    }

    private sealed class FakeMaintenanceRecordRepository(MaintenanceRecord? record) : IMaintenanceRecordRepository
    {
        public List<MaintenanceRecord> UpdatedRecords { get; } = [];

        public Task<MaintenanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(record);

        public Task AddAsync(MaintenanceRecord record, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateMaintenanceRecordCommandHandler.");

        public Task UpdateAsync(MaintenanceRecord record, CancellationToken cancellationToken)
        {
            UpdatedRecords.Add(record);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<MaintenanceRecord>> GetByVehicleIdsAsync(IEnumerable<Guid> vehicleIds, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UpdateMaintenanceRecordCommandHandler.");
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

    private static MaintenanceRecord CreateRecord(Guid vehicleId) => new()
    {
        Id = Guid.NewGuid(),
        VehicleId = vehicleId,
        Type = MaintenanceType.Preventivo,
        ScheduledDate = new DateOnly(2026, 1, 1),
        Status = MaintenanceStatus.Programado,
        Description = "Cambio de aceite",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static UpdateMaintenanceRecordCommand CreateCommand(Guid id) => new()
    {
        Id = id,
        Status = MaintenanceStatus.EnProceso,
        StartDate = new DateOnly(2026, 1, 2),
        MechanicNotes = "Se reemplazaron frenos",
        Cost = 15000m
    };

    [Fact]
    public async Task Handle_SameOrganization_AppliesUpdates()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var record = CreateRecord(vehicle.Id);
        var repository = new FakeMaintenanceRecordRepository(record);
        var handler = new UpdateMaintenanceRecordCommandHandler(
            new FakeVehicleRepository(vehicle), repository, new FakeCurrentUserService(organizationId));

        await handler.Handle(CreateCommand(record.Id), TestContext.Current.CancellationToken);

        var updated = Assert.Single(repository.UpdatedRecords);
        Assert.Same(record, updated);
        Assert.Equal(MaintenanceStatus.EnProceso, updated.Status);
        Assert.Equal(new DateOnly(2026, 1, 2), updated.StartDate);
        Assert.Equal("Se reemplazaron frenos", updated.MechanicNotes);
        Assert.Equal(15000m, updated.Cost);
    }

    [Fact]
    public async Task Handle_MissingRecord_ThrowsMaintenanceRecordNotFoundException()
    {
        var repository = new FakeMaintenanceRecordRepository(record: null);
        var handler = new UpdateMaintenanceRecordCommandHandler(
            new FakeVehicleRepository(vehicle: null), repository, new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<MaintenanceRecordNotFoundException>(
            () => handler.Handle(CreateCommand(Guid.NewGuid()), TestContext.Current.CancellationToken));

        Assert.Empty(repository.UpdatedRecords);
    }

    [Fact]
    public async Task Handle_CrossOrganizationRecord_ThrowsMaintenanceRecordNotFoundException()
    {
        var vehicle = CreateVehicle(Guid.NewGuid()); // different organization than the caller
        var record = CreateRecord(vehicle.Id);
        var repository = new FakeMaintenanceRecordRepository(record);
        var handler = new UpdateMaintenanceRecordCommandHandler(
            new FakeVehicleRepository(vehicle), repository, new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<MaintenanceRecordNotFoundException>(
            () => handler.Handle(CreateCommand(record.Id), TestContext.Current.CancellationToken));

        Assert.Empty(repository.UpdatedRecords);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var repository = new FakeMaintenanceRecordRepository(record: null);
        var handler = new UpdateMaintenanceRecordCommandHandler(
            new FakeVehicleRepository(vehicle: null), repository, new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(Guid.NewGuid()), TestContext.Current.CancellationToken));
    }
}
