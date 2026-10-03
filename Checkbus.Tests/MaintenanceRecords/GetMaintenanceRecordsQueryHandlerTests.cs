using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.MaintenanceRecords.Queries;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.Tests.MaintenanceRecords;

public class GetMaintenanceRecordsQueryHandlerTests
{
    private sealed class FakeVehicleRepository(IReadOnlyList<Vehicle> vehicles) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetMaintenanceRecordsQueryHandler.");

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetMaintenanceRecordsQueryHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(vehicles);

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetMaintenanceRecordsQueryHandler.");
    }

    private sealed class FakeMaintenanceRecordRepository(IReadOnlyList<MaintenanceRecord> records) : IMaintenanceRecordRepository
    {
        public Task<MaintenanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetMaintenanceRecordsQueryHandler.");

        public Task AddAsync(MaintenanceRecord record, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetMaintenanceRecordsQueryHandler.");

        public Task UpdateAsync(MaintenanceRecord record, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetMaintenanceRecordsQueryHandler.");

        public Task<IReadOnlyList<MaintenanceRecord>> GetByVehicleIdsAsync(IEnumerable<Guid> vehicleIds, CancellationToken cancellationToken)
        {
            var idSet = vehicleIds.ToList();
            return Task.FromResult<IReadOnlyList<MaintenanceRecord>>(
                records.Where(r => idSet.Contains(r.VehicleId)).ToList());
        }
    }

    private sealed class FakeCurrentUserService(Guid? organizationId) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => "Mecanico";
        public string? Email => "mecanico@checkbus-demo.com";
    }

    private static Vehicle CreateVehicle(Guid organizationId, string patent) => new()
    {
        Id = Guid.NewGuid(),
        Brand = "Mercedes-Benz",
        Model = "O500",
        Year = 2020,
        Patent = patent,
        Capacity = 45,
        Mileage = 1000,
        Status = VehicleStatus.Activo,
        OrganizationId = organizationId,
        OwnerType = VehicleOwnerType.Organizacion
    };

    private static MaintenanceRecord CreateRecord(Guid vehicleId, MaintenanceType type, MaintenanceStatus status) => new()
    {
        Id = Guid.NewGuid(),
        VehicleId = vehicleId,
        Type = type,
        ScheduledDate = new DateOnly(2026, 1, 1),
        Status = status,
        Description = "Cambio de aceite",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_ReturnsRecordsScopedToOrganizationVehicles_WithVehicleDisplayFields()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId, "AB123CD");
        var record = CreateRecord(vehicle.Id, MaintenanceType.Preventivo, MaintenanceStatus.Programado);
        var handler = new GetMaintenanceRecordsQueryHandler(
            new FakeVehicleRepository([vehicle]),
            new FakeMaintenanceRecordRepository([record]),
            new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(new GetMaintenanceRecordsQuery(), TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal(record.Id, dto.Id);
        Assert.Equal("AB123CD", dto.VehiclePatent);
    }

    [Fact]
    public async Task Handle_FilterByVehicleId_OtherOrganizationVehicleId_ReturnsEmpty()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId, "AB123CD");
        var record = CreateRecord(vehicle.Id, MaintenanceType.Preventivo, MaintenanceStatus.Programado);
        var handler = new GetMaintenanceRecordsQueryHandler(
            new FakeVehicleRepository([vehicle]),
            new FakeMaintenanceRecordRepository([record]),
            new FakeCurrentUserService(organizationId));

        // A vehicleId from a different org must never surface records — proof this never
        // special-cases into a VehicleNotFoundException leak, it just yields no matches.
        var otherOrgVehicleId = Guid.NewGuid();
        var result = await handler.Handle(
            new GetMaintenanceRecordsQuery { VehicleId = otherOrgVehicleId }, TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_FilterByStatus_OnlyMatchingStatusReturned()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId, "AB123CD");
        var programado = CreateRecord(vehicle.Id, MaintenanceType.Preventivo, MaintenanceStatus.Programado);
        var completado = CreateRecord(vehicle.Id, MaintenanceType.Preventivo, MaintenanceStatus.Completado);
        var handler = new GetMaintenanceRecordsQueryHandler(
            new FakeVehicleRepository([vehicle]),
            new FakeMaintenanceRecordRepository([programado, completado]),
            new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(
            new GetMaintenanceRecordsQuery { Status = MaintenanceStatus.Completado }, TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal(completado.Id, dto.Id);
    }

    [Fact]
    public async Task Handle_FilterByType_OnlyMatchingTypeReturned()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId, "AB123CD");
        var preventivo = CreateRecord(vehicle.Id, MaintenanceType.Preventivo, MaintenanceStatus.Programado);
        var reactivo = CreateRecord(vehicle.Id, MaintenanceType.Reactivo, MaintenanceStatus.Programado);
        var handler = new GetMaintenanceRecordsQueryHandler(
            new FakeVehicleRepository([vehicle]),
            new FakeMaintenanceRecordRepository([preventivo, reactivo]),
            new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(
            new GetMaintenanceRecordsQuery { Type = MaintenanceType.Reactivo }, TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal(reactivo.Id, dto.Id);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetMaintenanceRecordsQueryHandler(
            new FakeVehicleRepository([]),
            new FakeMaintenanceRecordRepository([]),
            new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetMaintenanceRecordsQuery(), TestContext.Current.CancellationToken));
    }
}
