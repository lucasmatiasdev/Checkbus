using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.VehicleDiagnostics.Queries;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Maintenance;

namespace Checkbus.Tests.VehicleDiagnostics;

public class GetVehicleDiagnosticsQueryHandlerTests
{
    private sealed class FakeVehicleRepository(Vehicle? vehicle) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(vehicle);

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDiagnosticsQueryHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDiagnosticsQueryHandler.");

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDiagnosticsQueryHandler.");
    }

    private sealed class FakeMaintenanceRecordRepository(MaintenanceRecord? record) : IMaintenanceRecordRepository
    {
        public Task<MaintenanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(record);

        public Task AddAsync(MaintenanceRecord record, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDiagnosticsQueryHandler.");

        public Task UpdateAsync(MaintenanceRecord record, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDiagnosticsQueryHandler.");

        public Task<IReadOnlyList<MaintenanceRecord>> GetByVehicleIdsAsync(IEnumerable<Guid> vehicleIds, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDiagnosticsQueryHandler.");
    }

    private sealed class FakeVehicleDiagnosticRepository(
        IReadOnlyList<VehicleDiagnostic> diagnostics,
        IReadOnlyList<ComponentDiagnostic> components) : IVehicleDiagnosticRepository
    {
        public Task AddAsync(VehicleDiagnostic diagnostic, IEnumerable<ComponentDiagnostic> newComponents, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDiagnosticsQueryHandler.");

        public Task<VehicleDiagnostic?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehicleDiagnosticsQueryHandler.");

        public Task<IReadOnlyList<VehicleDiagnostic>> GetByMaintenanceRecordIdAsync(Guid maintenanceRecordId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<VehicleDiagnostic>>(
                diagnostics.Where(d => d.MaintenanceRecordId == maintenanceRecordId).ToList());

        public Task<IReadOnlyList<ComponentDiagnostic>> GetComponentsByDiagnosticIdsAsync(IEnumerable<Guid> diagnosticIds, CancellationToken cancellationToken)
        {
            var idSet = diagnosticIds.ToList();
            return Task.FromResult<IReadOnlyList<ComponentDiagnostic>>(
                components.Where(c => idSet.Contains(c.VehicleDiagnosticId)).ToList());
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
        Description = "Cambio de aceite"
    };

    private static VehicleDiagnostic CreateDiagnostic(Guid vehicleId, Guid maintenanceRecordId) => new()
    {
        Id = Guid.NewGuid(),
        VehicleId = vehicleId,
        MaintenanceRecordId = maintenanceRecordId,
        DiagnosedAt = new DateOnly(2026, 1, 2),
        Notes = "Revision general"
    };

    private static ComponentDiagnostic CreateComponent(Guid diagnosticId, VehicleComponent component, ComponentCondition condition) => new()
    {
        Id = Guid.NewGuid(),
        VehicleDiagnosticId = diagnosticId,
        Component = component,
        Condition = condition
    };

    [Fact]
    public async Task Handle_SameOrganization_ReturnsDiagnosticsWithNestedComponents()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var record = CreateRecord(vehicle.Id);
        var diagnostic = CreateDiagnostic(vehicle.Id, record.Id);
        var components = new[]
        {
            CreateComponent(diagnostic.Id, VehicleComponent.Motor, ComponentCondition.Bueno),
            CreateComponent(diagnostic.Id, VehicleComponent.Frenos, ComponentCondition.Fallado)
        };
        var handler = new GetVehicleDiagnosticsQueryHandler(
            new FakeMaintenanceRecordRepository(record),
            new FakeVehicleRepository(vehicle),
            new FakeVehicleDiagnosticRepository([diagnostic], components),
            new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(
            new GetVehicleDiagnosticsQuery { MaintenanceRecordId = record.Id }, TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal(diagnostic.Id, dto.Id);
        Assert.Equal(2, dto.Components.Count);
        Assert.Contains(dto.Components, c => c.Component == VehicleComponent.Motor && c.Condition == ComponentCondition.Bueno);
        Assert.Contains(dto.Components, c => c.Component == VehicleComponent.Frenos && c.Condition == ComponentCondition.Fallado);
    }

    [Fact]
    public async Task Handle_CrossOrganizationRecord_ThrowsMaintenanceRecordNotFoundException()
    {
        var vehicle = CreateVehicle(Guid.NewGuid()); // different organization than the caller
        var record = CreateRecord(vehicle.Id);
        var handler = new GetVehicleDiagnosticsQueryHandler(
            new FakeMaintenanceRecordRepository(record),
            new FakeVehicleRepository(vehicle),
            new FakeVehicleDiagnosticRepository([], []),
            new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<MaintenanceRecordNotFoundException>(
            () => handler.Handle(new GetVehicleDiagnosticsQuery { MaintenanceRecordId = record.Id }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_MissingMaintenanceRecord_ThrowsMaintenanceRecordNotFoundException()
    {
        var handler = new GetVehicleDiagnosticsQueryHandler(
            new FakeMaintenanceRecordRepository(record: null),
            new FakeVehicleRepository(vehicle: null),
            new FakeVehicleDiagnosticRepository([], []),
            new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<MaintenanceRecordNotFoundException>(
            () => handler.Handle(new GetVehicleDiagnosticsQuery { MaintenanceRecordId = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetVehicleDiagnosticsQueryHandler(
            new FakeMaintenanceRecordRepository(record: null),
            new FakeVehicleRepository(vehicle: null),
            new FakeVehicleDiagnosticRepository([], []),
            new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetVehicleDiagnosticsQuery { MaintenanceRecordId = Guid.NewGuid() }, TestContext.Current.CancellationToken));
    }
}
