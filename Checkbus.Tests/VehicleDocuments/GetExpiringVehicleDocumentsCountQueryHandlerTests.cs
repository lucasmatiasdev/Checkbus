using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.VehicleDocuments.Queries;
using Checkbus.ApiService.Domain.Entities.Vehicles;

namespace Checkbus.Tests.VehicleDocuments;

public class GetExpiringVehicleDocumentsCountQueryHandlerTests
{
    private sealed class FakeVehicleDocumentRepository : IVehicleDocumentRepository
    {
        public int CountToReturn { get; set; }
        public Guid? ReceivedOrganizationId { get; private set; }
        public DateOnly? ReceivedThreshold { get; private set; }

        public Task<IReadOnlyList<VehicleDocument>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetExpiringVehicleDocumentsCountQueryHandler.");

        public Task AddRangeAsync(IEnumerable<VehicleDocument> documents, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetExpiringVehicleDocumentsCountQueryHandler.");

        public Task AddAsync(VehicleDocument document, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetExpiringVehicleDocumentsCountQueryHandler.");

        public Task UpdateAsync(VehicleDocument document, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetExpiringVehicleDocumentsCountQueryHandler.");

        public Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken)
        {
            ReceivedOrganizationId = organizationId;
            ReceivedThreshold = expiringThresholdDate;
            return Task.FromResult(CountToReturn);
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

    [Fact]
    public async Task Handle_ValidRequest_ReturnsRepositoryCountForCallersOrganization()
    {
        var organizationId = Guid.NewGuid();
        var repository = new FakeVehicleDocumentRepository { CountToReturn = 4 };
        var handler = new GetExpiringVehicleDocumentsCountQueryHandler(repository, new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(new GetExpiringVehicleDocumentsCountQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(4, result);
        Assert.Equal(organizationId, repository.ReceivedOrganizationId);
    }

    [Fact]
    public async Task Handle_ValidRequest_PassesTodayPlusThirtyDaysAsThreshold()
    {
        var repository = new FakeVehicleDocumentRepository();
        var handler = new GetExpiringVehicleDocumentsCountQueryHandler(repository, new FakeCurrentUserService(Guid.NewGuid()));
        var expectedThreshold = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);

        await handler.Handle(new GetExpiringVehicleDocumentsCountQuery(), TestContext.Current.CancellationToken);

        Assert.NotNull(repository.ReceivedThreshold);
        // Tolerance of 1 day guards against a UTC-midnight rollover between the two
        // DateTime.UtcNow reads (test and handler), without asserting exact clock timing.
        var diffDays = Math.Abs(repository.ReceivedThreshold!.Value.DayNumber - expectedThreshold.DayNumber);
        Assert.InRange(diffDays, 0, 1);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetExpiringVehicleDocumentsCountQueryHandler(
            new FakeVehicleDocumentRepository(), new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetExpiringVehicleDocumentsCountQuery(), TestContext.Current.CancellationToken));
    }
}
