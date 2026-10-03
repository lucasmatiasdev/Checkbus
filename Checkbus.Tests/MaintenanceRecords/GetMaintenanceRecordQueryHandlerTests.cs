using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.MaintenanceRecords.Queries;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Maintenance;

namespace Checkbus.Tests.MaintenanceRecords;

public class GetMaintenanceRecordQueryHandlerTests
{
    private sealed class FakeVehicleRepository(Vehicle? vehicle) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(vehicle);

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetMaintenanceRecordQueryHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetMaintenanceRecordQueryHandler.");

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetMaintenanceRecordQueryHandler.");
    }

    private sealed class FakeMaintenanceRecordRepository(MaintenanceRecord? record) : IMaintenanceRecordRepository
    {
        public Task<MaintenanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(record);

        public Task AddAsync(MaintenanceRecord record, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetMaintenanceRecordQueryHandler.");

        public Task UpdateAsync(MaintenanceRecord record, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetMaintenanceRecordQueryHandler.");

        public Task<IReadOnlyList<MaintenanceRecord>> GetByVehicleIdsAsync(IEnumerable<Guid> vehicleIds, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetMaintenanceRecordQueryHandler.");
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

    [Fact]
    public async Task Handle_SameOrganization_ReturnsDto()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var record = CreateRecord(vehicle.Id);
        var handler = new GetMaintenanceRecordQueryHandler(
            new FakeVehicleRepository(vehicle), new FakeMaintenanceRecordRepository(record), new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(new GetMaintenanceRecordQuery { Id = record.Id }, TestContext.Current.CancellationToken);

        Assert.Equal(record.Id, result.Id);
        Assert.Equal(vehicle.Patent, result.VehiclePatent);
    }

    [Fact]
    public async Task Handle_MissingRecord_ThrowsMaintenanceRecordNotFoundException()
    {
        var handler = new GetMaintenanceRecordQueryHandler(
            new FakeVehicleRepository(vehicle: null), new FakeMaintenanceRecordRepository(record: null), new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<MaintenanceRecordNotFoundException>(
            () => handler.Handle(new GetMaintenanceRecordQuery { Id = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_CrossOrganizationRecord_ThrowsMaintenanceRecordNotFoundException()
    {
        var vehicle = CreateVehicle(Guid.NewGuid()); // different organization than the caller
        var record = CreateRecord(vehicle.Id);
        var handler = new GetMaintenanceRecordQueryHandler(
            new FakeVehicleRepository(vehicle), new FakeMaintenanceRecordRepository(record), new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<MaintenanceRecordNotFoundException>(
            () => handler.Handle(new GetMaintenanceRecordQuery { Id = record.Id }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetMaintenanceRecordQueryHandler(
            new FakeVehicleRepository(vehicle: null), new FakeMaintenanceRecordRepository(record: null), new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetMaintenanceRecordQuery { Id = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }
}
