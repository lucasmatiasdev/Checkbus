using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.VehicleDiagnostics.Commands;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Maintenance;

namespace Checkbus.Tests.VehicleDiagnostics;

public class CreateVehicleDiagnosticCommandHandlerTests
{
    private sealed class FakeVehicleRepository(Vehicle? vehicle) : IVehicleRepository
    {
        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(vehicle);

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleDiagnosticCommandHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleDiagnosticCommandHandler.");

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleDiagnosticCommandHandler.");
    }

    private sealed class FakeMaintenanceRecordRepository(MaintenanceRecord? record) : IMaintenanceRecordRepository
    {
        public Task<MaintenanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => Task.FromResult(record);

        public Task AddAsync(MaintenanceRecord record, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleDiagnosticCommandHandler.");

        public Task UpdateAsync(MaintenanceRecord record, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleDiagnosticCommandHandler.");

        public Task<IReadOnlyList<MaintenanceRecord>> GetByVehicleIdsAsync(IEnumerable<Guid> vehicleIds, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleDiagnosticCommandHandler.");
    }

    private sealed class FakeVehicleDiagnosticRepository : IVehicleDiagnosticRepository
    {
        public List<VehicleDiagnostic> AddedDiagnostics { get; } = [];
        public List<ComponentDiagnostic> AddedComponents { get; } = [];

        public Task AddAsync(VehicleDiagnostic diagnostic, IEnumerable<ComponentDiagnostic> components, CancellationToken cancellationToken)
        {
            AddedDiagnostics.Add(diagnostic);
            AddedComponents.AddRange(components);
            return Task.CompletedTask;
        }

        public Task<VehicleDiagnostic?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleDiagnosticCommandHandler.");

        public Task<IReadOnlyList<VehicleDiagnostic>> GetByMaintenanceRecordIdAsync(Guid maintenanceRecordId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleDiagnosticCommandHandler.");

        public Task<IReadOnlyList<ComponentDiagnostic>> GetComponentsByDiagnosticIdsAsync(IEnumerable<Guid> diagnosticIds, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by CreateVehicleDiagnosticCommandHandler.");
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

    private static CreateVehicleDiagnosticCommand CreateCommand(Guid maintenanceRecordId, List<ComponentDiagnosticInput>? components = null) => new()
    {
        MaintenanceRecordId = maintenanceRecordId,
        DiagnosedAt = new DateOnly(2026, 1, 2),
        Notes = "Revision general",
        Components = components ??
        [
            new ComponentDiagnosticInput(VehicleComponent.Motor, ComponentCondition.Bueno, null),
            new ComponentDiagnosticInput(VehicleComponent.Frenos, ComponentCondition.Desgastado, "Pastillas al 20%")
        ]
    };

    [Fact]
    public async Task Handle_SameOrganization_MultipleComponents_PersistsAtomicallyAndReturnsDto()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var record = CreateRecord(vehicle.Id);
        var diagnosticRepository = new FakeVehicleDiagnosticRepository();
        var handler = new CreateVehicleDiagnosticCommandHandler(
            new FakeMaintenanceRecordRepository(record),
            new FakeVehicleRepository(vehicle),
            diagnosticRepository,
            new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(CreateCommand(record.Id), TestContext.Current.CancellationToken);

        var createdDiagnostic = Assert.Single(diagnosticRepository.AddedDiagnostics);
        Assert.Equal(vehicle.Id, createdDiagnostic.VehicleId);
        Assert.Equal(record.Id, createdDiagnostic.MaintenanceRecordId);
        Assert.Equal(2, diagnosticRepository.AddedComponents.Count);
        Assert.All(diagnosticRepository.AddedComponents, c => Assert.Equal(createdDiagnostic.Id, c.VehicleDiagnosticId));

        Assert.Equal(createdDiagnostic.Id, result.Id);
        Assert.Equal(2, result.Components.Count);
        Assert.Contains(result.Components, c => c.Component == VehicleComponent.Motor && c.Condition == ComponentCondition.Bueno);
        Assert.Contains(result.Components, c => c.Component == VehicleComponent.Frenos && c.Notes == "Pastillas al 20%");
    }

    [Fact]
    public async Task Handle_CrossOrganizationVehicle_ThrowsMaintenanceRecordNotFoundException()
    {
        var vehicle = CreateVehicle(Guid.NewGuid()); // different organization than the caller
        var record = CreateRecord(vehicle.Id);
        var diagnosticRepository = new FakeVehicleDiagnosticRepository();
        var handler = new CreateVehicleDiagnosticCommandHandler(
            new FakeMaintenanceRecordRepository(record),
            new FakeVehicleRepository(vehicle),
            diagnosticRepository,
            new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<MaintenanceRecordNotFoundException>(
            () => handler.Handle(CreateCommand(record.Id), TestContext.Current.CancellationToken));

        Assert.Empty(diagnosticRepository.AddedDiagnostics);
    }

    [Fact]
    public async Task Handle_MissingMaintenanceRecord_ThrowsMaintenanceRecordNotFoundException()
    {
        var diagnosticRepository = new FakeVehicleDiagnosticRepository();
        var handler = new CreateVehicleDiagnosticCommandHandler(
            new FakeMaintenanceRecordRepository(record: null),
            new FakeVehicleRepository(vehicle: null),
            diagnosticRepository,
            new FakeCurrentUserService(Guid.NewGuid()));

        await Assert.ThrowsAsync<MaintenanceRecordNotFoundException>(
            () => handler.Handle(CreateCommand(Guid.NewGuid()), TestContext.Current.CancellationToken));

        Assert.Empty(diagnosticRepository.AddedDiagnostics);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var diagnosticRepository = new FakeVehicleDiagnosticRepository();
        var handler = new CreateVehicleDiagnosticCommandHandler(
            new FakeMaintenanceRecordRepository(record: null),
            new FakeVehicleRepository(vehicle: null),
            diagnosticRepository,
            new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(Guid.NewGuid()), TestContext.Current.CancellationToken));
    }
}
