using Checkbus.ApiService.Application.Audit.Queries;
using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Audit;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.Audit;

namespace Checkbus.Tests.Audit;

public class GetAuditLogQueryHandlerTests
{
    // Local fake routing to the right repository method depending on the scope the handler
    // chose, same split-by-method idea as FakeSupportTicketRepository in
    // GetAllSupportTicketsQueryHandlerTests.
    private sealed class FakeAuditLogRepository(IReadOnlyList<AuditLogEntry> allEntries) : IAuditLogRepository
    {
        public Task AddAsync(AuditLogEntry entry, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetAuditLogQueryHandler.");

        public Task<IReadOnlyList<AuditLogEntry>> GetAllByOrganizationAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<AuditLogEntry>>(
                allEntries.Where(e => e.OrganizationId == organizationId).ToList());

        public Task<IReadOnlyList<AuditLogEntry>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult(allEntries);
    }

    private sealed class FakeOrganizationRepository(string? callerSlug, IReadOnlyDictionary<Guid, string?> namesById) : IOrganizationRepository
    {
        public Task AddAsync(Organization organization, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetAuditLogQueryHandler.");

        public Task<string?> GetSlugByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(callerSlug);

        public Task<string?> GetNameByIdAsync(Guid organizationId, CancellationToken cancellationToken)
            => Task.FromResult(namesById.TryGetValue(organizationId, out var name) ? name : null);

        public Task<Guid?> GetIdBySlugAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetAuditLogQueryHandler.");

        public Task<bool> CuitExistsAsync(string cuit, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetAuditLogQueryHandler.");

        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetAuditLogQueryHandler.");

        public Task<IReadOnlyList<Organization>> GetAllAsync(CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by GetAuditLogQueryHandler.");
    }

    private sealed class FakeCurrentUserService(Guid? organizationId, string? role) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => Guid.NewGuid();
        public Guid? OrganizationId => organizationId;
        public string? Role => role;
        public string? Email => "caller@checkbus-interno.com";
    }

    private static AuditLogEntry CreateEntry(
        Guid? organizationId,
        AuditAction action = AuditAction.LoginExitoso,
        Guid? actorUserId = null,
        DateTime? createdAt = null,
        string actorEmail = "actor@example.com") => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        ActorUserId = actorUserId,
        ActorEmail = actorEmail,
        Action = action,
        EntityType = "User",
        EntityId = Guid.NewGuid(),
        Description = "Evento de prueba",
        CreatedAt = createdAt ?? DateTime.UtcNow
    };

    [Fact]
    public async Task Handle_Administrador_ReturnsOnlyOwnOrganizationAndIgnoresOrganizationIdFilter()
    {
        var callerOrganizationId = Guid.NewGuid();
        var otherOrganizationId = Guid.NewGuid();
        var ownEntry = CreateEntry(callerOrganizationId);
        var otherEntry = CreateEntry(otherOrganizationId);
        var handler = new GetAuditLogQueryHandler(
            new FakeAuditLogRepository([ownEntry, otherEntry]),
            new FakeOrganizationRepository(null, new Dictionary<Guid, string?>()),
            new FakeCurrentUserService(callerOrganizationId, nameof(Role.Administrador)));

        // Even trying to peek into another organization via the filter stays scoped to the
        // Administrador's own organization (CU-16 main flow).
        var result = await handler.Handle(
            new GetAuditLogQuery { OrganizationId = otherOrganizationId },
            TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal(ownEntry.Id, dto.Id);
        Assert.Null(dto.OrganizationName);
    }

    [Fact]
    public async Task Handle_CheckbusSoporteWithoutOrganizationFilter_ReturnsEntriesAcrossOrganizationsWithResolvedNames()
    {
        var sentinelOrganizationId = Guid.NewGuid();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var entryA = CreateEntry(organizationA);
        var entryB = CreateEntry(organizationB);
        var handler = new GetAuditLogQueryHandler(
            new FakeAuditLogRepository([entryA, entryB]),
            new FakeOrganizationRepository(
                CheckbusInternalOrganization.Slug,
                new Dictionary<Guid, string?> { [organizationA] = "Organizacion A", [organizationB] = "Organizacion B" }),
            new FakeCurrentUserService(sentinelOrganizationId, nameof(Role.CheckbusSoporte)));

        var result = await handler.Handle(new GetAuditLogQuery(), TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        var dtoA = Assert.Single(result, dto => dto.Id == entryA.Id);
        Assert.Equal("Organizacion A", dtoA.OrganizationName);
        var dtoB = Assert.Single(result, dto => dto.Id == entryB.Id);
        Assert.Equal("Organizacion B", dtoB.OrganizationName);
    }

    [Fact]
    public async Task Handle_CheckbusSoporteWithOrganizationFilter_ReturnsOnlyThatOrganization()
    {
        var sentinelOrganizationId = Guid.NewGuid();
        var organizationA = Guid.NewGuid();
        var organizationB = Guid.NewGuid();
        var entryA = CreateEntry(organizationA);
        var entryB = CreateEntry(organizationB);
        var handler = new GetAuditLogQueryHandler(
            new FakeAuditLogRepository([entryA, entryB]),
            new FakeOrganizationRepository(
                CheckbusInternalOrganization.Slug,
                new Dictionary<Guid, string?> { [organizationA] = "Organizacion A" }),
            new FakeCurrentUserService(sentinelOrganizationId, nameof(Role.CheckbusSoporte)));

        var result = await handler.Handle(
            new GetAuditLogQuery { OrganizationId = organizationA },
            TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal(entryA.Id, dto.Id);
        Assert.Equal("Organizacion A", dto.OrganizationName);
    }

    [Fact]
    public async Task Handle_CheckbusSoporteCallerOrganizationIsNotSentinel_ThrowsAuditLogAccessDeniedException()
    {
        // Defense in depth: having the CheckbusSoporte Role claim is not enough - the caller's
        // own OrganizationId must resolve to CheckbusInternalOrganization.Slug.
        var callerOrganizationId = Guid.NewGuid();
        var handler = new GetAuditLogQueryHandler(
            new FakeAuditLogRepository([CreateEntry(Guid.NewGuid())]),
            new FakeOrganizationRepository("otra-organizacion", new Dictionary<Guid, string?>()),
            new FakeCurrentUserService(callerOrganizationId, nameof(Role.CheckbusSoporte)));

        await Assert.ThrowsAsync<AuditLogAccessDeniedException>(
            () => handler.Handle(new GetAuditLogQuery(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_UnrelatedRole_ThrowsAuditLogAccessDeniedException()
    {
        var callerOrganizationId = Guid.NewGuid();
        var handler = new GetAuditLogQueryHandler(
            new FakeAuditLogRepository([]),
            new FakeOrganizationRepository(null, new Dictionary<Guid, string?>()),
            new FakeCurrentUserService(callerOrganizationId, nameof(Role.Chofer)));

        await Assert.ThrowsAsync<AuditLogAccessDeniedException>(
            () => handler.Handle(new GetAuditLogQuery(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_NullOrganizationIdClaim_ThrowsInvalidOperationException()
    {
        var handler = new GetAuditLogQueryHandler(
            new FakeAuditLogRepository([]),
            new FakeOrganizationRepository(null, new Dictionary<Guid, string?>()),
            new FakeCurrentUserService(organizationId: null, role: nameof(Role.Administrador)));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetAuditLogQuery(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Handle_CombinesDateActionAndActorUserIdFilters()
    {
        var callerOrganizationId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var otherActorUserId = Guid.NewGuid();
        var baseDate = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        // Matches every filter.
        var matching = CreateEntry(
            callerOrganizationId, AuditAction.ViajeCancelado, actorUserId, baseDate);
        // Wrong action.
        var wrongAction = CreateEntry(
            callerOrganizationId, AuditAction.ViajeIniciado, actorUserId, baseDate);
        // Wrong actor.
        var wrongActor = CreateEntry(
            callerOrganizationId, AuditAction.ViajeCancelado, otherActorUserId, baseDate);
        // Outside the date range.
        var tooEarly = CreateEntry(
            callerOrganizationId, AuditAction.ViajeCancelado, actorUserId, baseDate.AddDays(-10));
        var tooLate = CreateEntry(
            callerOrganizationId, AuditAction.ViajeCancelado, actorUserId, baseDate.AddDays(10));

        var handler = new GetAuditLogQueryHandler(
            new FakeAuditLogRepository([matching, wrongAction, wrongActor, tooEarly, tooLate]),
            new FakeOrganizationRepository(null, new Dictionary<Guid, string?>()),
            new FakeCurrentUserService(callerOrganizationId, nameof(Role.Administrador)));

        var result = await handler.Handle(
            new GetAuditLogQuery
            {
                DateFrom = baseDate.AddDays(-1),
                DateTo = baseDate.AddDays(1),
                Action = AuditAction.ViajeCancelado,
                ActorUserId = actorUserId
            },
            TestContext.Current.CancellationToken);

        var dto = Assert.Single(result);
        Assert.Equal(matching.Id, dto.Id);
    }

    [Fact]
    public async Task Handle_OrdersResultsByCreatedAtDescending()
    {
        var callerOrganizationId = Guid.NewGuid();
        var older = CreateEntry(callerOrganizationId, createdAt: DateTime.UtcNow.AddDays(-2));
        var newer = CreateEntry(callerOrganizationId, createdAt: DateTime.UtcNow);
        var handler = new GetAuditLogQueryHandler(
            new FakeAuditLogRepository([older, newer]),
            new FakeOrganizationRepository(null, new Dictionary<Guid, string?>()),
            new FakeCurrentUserService(callerOrganizationId, nameof(Role.Administrador)));

        var result = await handler.Handle(new GetAuditLogQuery(), TestContext.Current.CancellationToken);

        Assert.Equal([newer.Id, older.Id], result.Select(dto => dto.Id));
    }
}
