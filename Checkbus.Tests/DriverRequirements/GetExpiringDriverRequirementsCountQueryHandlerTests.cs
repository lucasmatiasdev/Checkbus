using Checkbus.ApiService.Application.DriverRequirements.Queries;
using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Entities.Documents;

namespace Checkbus.Tests.DriverRequirements;

public class GetExpiringDriverRequirementsCountQueryHandlerTests
{
    private sealed class FakeDriverRequirementRepository : IDriverRequirementRepository
    {
        public int CountToReturn { get; set; }
        public Guid? ReceivedOrganizationId { get; private set; }
        public DateOnly? ReceivedThreshold { get; private set; }

        public Task<IReadOnlyList<DriverRequirement>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetExpiringDriverRequirementsCountQueryHandler.");

        public Task AddRangeAsync(IEnumerable<DriverRequirement> requirements, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetExpiringDriverRequirementsCountQueryHandler.");

        public Task UpdateAsync(DriverRequirement requirement, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetExpiringDriverRequirementsCountQueryHandler.");

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
        var repository = new FakeDriverRequirementRepository { CountToReturn = 7 };
        var handler = new GetExpiringDriverRequirementsCountQueryHandler(repository, new FakeCurrentUserService(organizationId));

        var result = await handler.Handle(new GetExpiringDriverRequirementsCountQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(7, result);
        Assert.Equal(organizationId, repository.ReceivedOrganizationId);
    }

    [Fact]
    public async Task Handle_ValidRequest_PassesTodayPlusThirtyDaysAsThreshold()
    {
        var repository = new FakeDriverRequirementRepository();
        var handler = new GetExpiringDriverRequirementsCountQueryHandler(repository, new FakeCurrentUserService(Guid.NewGuid()));
        var expectedThreshold = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30);

        await handler.Handle(new GetExpiringDriverRequirementsCountQuery(), TestContext.Current.CancellationToken);

        Assert.NotNull(repository.ReceivedThreshold);
        // Tolerance of 1 day guards against a UTC-midnight rollover between the two
        // DateTime.UtcNow reads (test and handler), without asserting exact clock timing.
        var diffDays = Math.Abs(repository.ReceivedThreshold!.Value.DayNumber - expectedThreshold.DayNumber);
        Assert.InRange(diffDays, 0, 1);
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetExpiringDriverRequirementsCountQueryHandler(
            new FakeDriverRequirementRepository(), new FakeCurrentUserService(organizationId: null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetExpiringDriverRequirementsCountQuery(), TestContext.Current.CancellationToken));
    }
}
