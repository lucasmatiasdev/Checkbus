using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Vehicles.Queries;
using Checkbus.ApiService.Domain.Entities.Vehicles;
using Checkbus.ApiService.Domain.Enums;

namespace Checkbus.Tests.Vehicles;

public class GetVehiclesQueryHandlerTests
{
    private sealed class FakeVehicleRepository : IVehicleRepository
    {
        public IReadOnlyList<Vehicle> Vehicles { get; set; } = [];
        public Guid? ReceivedOrganizationId { get; private set; }

        public Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehiclesQueryHandler.");

        public Task AddAsync(Vehicle vehicle, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehiclesQueryHandler.");

        public Task<IReadOnlyList<Vehicle>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
        {
            ReceivedOrganizationId = organizationId;
            return Task.FromResult(Vehicles);
        }

        public Task<bool> PatentExistsAsync(string patent, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetVehiclesQueryHandler.");
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

    [Fact]
    public async Task Handle_ValidRequest_ReturnsRepositoryVehiclesMappedToDto()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var repository = new FakeVehicleRepository { Vehicles = [vehicle] };
        var handler = new GetVehiclesQueryHandler(repository, new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(new GetVehiclesQuery(), TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal(vehicle.Id, dto.Id);
        Assert.Equal(vehicle.Brand, dto.Brand);
        Assert.Equal(vehicle.Model, dto.Model);
        Assert.Equal(vehicle.Year, dto.Year);
        Assert.Equal(vehicle.Patent, dto.Patent);
        Assert.Equal(vehicle.Capacity, dto.Capacity);
        Assert.Equal(vehicle.Mileage, dto.Mileage);
        Assert.Equal(vehicle.Status, dto.Status);
        Assert.Equal(vehicle.OwnerType, dto.OwnerType);
        Assert.Equal(vehicle.OwnerUserId, dto.OwnerUserId);
        Assert.Equal(vehicle.OwnerName, dto.OwnerName);
        Assert.Equal(vehicle.OwnerDocumentNumber, dto.OwnerDocumentNumber);
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsRepositoryWithCallersOrganizationId()
    {
        var organizationId = Guid.NewGuid();
        var repository = new FakeVehicleRepository();
        var handler = new GetVehiclesQueryHandler(repository, new FakeCurrentUserService(organizationId));

        await handler.Handle(new GetVehiclesQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(organizationId, repository.ReceivedOrganizationId);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetVehiclesQueryHandler(new FakeVehicleRepository(), new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetVehiclesQuery(), TestContext.Current.CancellationToken));
    }
}
