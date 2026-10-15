using Checkbus.ApiService.Domain.Authorization;
using Checkbus.ApiService.Domain.Entities.Authentication;
using Checkbus.ApiService.Domain.Entities.Support;
using Checkbus.ApiService.Domain.Entities.Tenancy;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Infrastructure.Implementations.Repositories;
using Checkbus.ApiService.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Checkbus.Tests.Persistence;

public class SupportTicketRepositoryTests
{
    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=True");
        connection.Open();
        return connection;
    }

    private static CheckbusDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<CheckbusDbContext>()
            .UseSqlite(connection)
            .Options;
        return new CheckbusDbContext(options);
    }

    private static Organization CreateOrganization(string slug) => new()
    {
        Id = Guid.NewGuid(),
        CUIT = "20-12345678-9",
        Name = "Checkbus Demo",
        Slug = slug,
        LogoUrl = "",
        IsActive = true
    };

    private static User CreateUser(Guid organizationId, string documentNumber, string email) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Jose",
        Surname = "Diaz",
        Email = email,
        PasswordHash = "hashed-password",
        DocumentNumber = documentNumber,
        Role = Role.Planificador,
        OrganizationId = organizationId,
        IsActive = true
    };

    private static SupportTicket CreateTicket(Guid organizationId, Guid createdByUserId, DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = organizationId,
        CreatedByUserId = createdByUserId,
        Category = SupportTicketCategory.ProblemaTecnico,
        Subject = "No puedo ver mis reservas",
        Description = "El listado de reservas no carga desde ayer.",
        Priority = SupportTicketPriority.Alta,
        Status = SupportTicketStatus.Abierto,
        CreatedAt = createdAt
    };

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTrips()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organization = CreateOrganization("checkbus-demo");
        var user = CreateUser(organization.Id, "30111222", "jose@checkbus-demo.com");
        db.Organizations.Add(organization);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new SupportTicketRepository(db);
        var ticket = CreateTicket(organization.Id, user.Id, DateTime.UtcNow);

        await repository.AddAsync(ticket, cancellationToken);
        var result = await repository.GetByIdAsync(ticket.Id, cancellationToken);

        Assert.NotNull(result);
        Assert.Equal(ticket.Subject, result!.Subject);
        Assert.Equal(SupportTicketStatus.Abierto, result.Status);
        Assert.Equal(organization.Id, result.OrganizationId);
        Assert.Equal(user.Id, result.CreatedByUserId);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var repository = new SupportTicketRepository(db);

        var result = await repository.GetByIdAsync(Guid.NewGuid(), cancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllByOrganizationAsync_ReturnsOnlyThatOrganizationsTickets_OrderedByCreatedAtDescending()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        using var connection = CreateOpenConnection();
        using var db = CreateContext(connection);
        db.Database.EnsureCreated();

        var organizationA = CreateOrganization("checkbus-demo");
        var organizationB = CreateOrganization("brahman");
        var userA = CreateUser(organizationA.Id, "30111222", "jose@checkbus-demo.com");
        var userB = CreateUser(organizationB.Id, "30333444", "ana@brahman.com");
        db.Organizations.AddRange(organizationA, organizationB);
        db.Users.AddRange(userA, userB);
        await db.SaveChangesAsync(cancellationToken);

        var repository = new SupportTicketRepository(db);
        var now = DateTime.UtcNow;
        var oldest = CreateTicket(organizationA.Id, userA.Id, now.AddDays(-2));
        var newest = CreateTicket(organizationA.Id, userA.Id, now.AddDays(-1));
        var otherOrgTicket = CreateTicket(organizationB.Id, userB.Id, now);
        await repository.AddAsync(oldest, cancellationToken);
        await repository.AddAsync(newest, cancellationToken);
        await repository.AddAsync(otherOrgTicket, cancellationToken);

        var result = await repository.GetAllByOrganizationAsync(organizationA.Id, cancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Equal(newest.Id, result[0].Id);
        Assert.Equal(oldest.Id, result[1].Id);
        Assert.All(result, t => Assert.Equal(organizationA.Id, t.OrganizationId));
    }
}
