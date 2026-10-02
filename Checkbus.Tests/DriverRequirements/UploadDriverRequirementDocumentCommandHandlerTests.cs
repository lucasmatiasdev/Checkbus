using Checkbus.ApiService.Application.DriverRequirements.Commands;
using Checkbus.ApiService.Application.Interfaces.Authentication;
using Checkbus.ApiService.Application.Interfaces.Repositories;
using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.Domain.Entities.Documents;
using Checkbus.ApiService.Domain.Enums;
using Checkbus.ApiService.Domain.Exceptions.DriverRequirements;

namespace Checkbus.Tests.DriverRequirements;

public class UploadDriverRequirementDocumentCommandHandlerTests
{
    private sealed class FakeDriverRequirementRepository : IDriverRequirementRepository
    {
        public IReadOnlyList<DriverRequirement> Requirements { get; set; } = [];
        public List<DriverRequirement> UpdatedRequirements { get; } = [];

        public Task<IReadOnlyList<DriverRequirement>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
            => Task.FromResult(Requirements);

        public Task AddRangeAsync(IEnumerable<DriverRequirement> requirements, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UploadDriverRequirementDocumentCommandHandler.");

        public Task UpdateAsync(DriverRequirement requirement, CancellationToken cancellationToken)
        {
            UpdatedRequirements.Add(requirement);
            return Task.CompletedTask;
        }

        public Task<int> GetExpiringOrExpiredCountByOrganizationAsync(
            Guid organizationId, DateOnly expiringThresholdDate, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UploadDriverRequirementDocumentCommandHandler.");
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public List<string> SavedKeys { get; } = [];
        public List<string> DeletedKeys { get; } = [];
        public string? LastSavedContentType { get; private set; }

        public Task<string> SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
        {
            SavedKeys.Add(key);
            LastSavedContentType = contentType;
            return Task.FromResult(key);
        }

        public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UploadDriverRequirementDocumentCommandHandler.");

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
            => throw new NotSupportedException("Not needed by UploadDriverRequirementDocumentCommandHandler.");

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            DeletedKeys.Add(key);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCurrentUserService(Guid? userId, string? role) : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public Guid? UserId => userId;
        public Guid? OrganizationId => Guid.NewGuid();
        public string? Role => role;
        public string? Email => "caller@checkbus-demo.com";
    }

    private static DriverRequirement CreateRequirement(Guid userId, DriverRequirementType type, DriverRequirementStatus status, string? fileKey = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Type = type,
        Status = status,
        DocumentPresent = fileKey is not null,
        FileKey = fileKey,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static UploadDriverRequirementDocumentCommand CreateCommand(Guid targetUserId) => new()
    {
        TargetUserId = targetUserId,
        Type = DriverRequirementType.LicenciaConducir,
        FileStream = new MemoryStream([1, 2, 3]),
        ContentType = "application/pdf",
        FileSizeBytes = 3,
        OriginalFileName = "licencia.pdf",
        ExpirationDate = new DateOnly(2027, 1, 1),
        IssueDate = new DateOnly(2026, 1, 1)
    };

    [Fact]
    public async Task Handle_ChoferSelfUpload_SucceedsAndResetsStatusToPendiente()
    {
        var targetUserId = Guid.NewGuid();
        var requirement = CreateRequirement(targetUserId, DriverRequirementType.LicenciaConducir, DriverRequirementStatus.Apto);
        var repository = new FakeDriverRequirementRepository { Requirements = [requirement] };
        var fileStorage = new FakeFileStorageService();
        var handler = new UploadDriverRequirementDocumentCommandHandler(
            repository, fileStorage, new FakeCurrentUserService(targetUserId, "Chofer"));

        await handler.Handle(CreateCommand(targetUserId), TestContext.Current.CancellationToken);

        var updated = Assert.Single(repository.UpdatedRequirements);
        Assert.Equal(DriverRequirementStatus.Pendiente, updated.Status);
        Assert.True(updated.DocumentPresent);
        Assert.Equal(new DateOnly(2026, 1, 1), updated.IssueDate);
        Assert.Equal(new DateOnly(2027, 1, 1), updated.ExpirationDate);
        Assert.NotNull(updated.FileKey);
        Assert.Equal("application/pdf", updated.FileContentType);
        Assert.Single(fileStorage.SavedKeys);
    }

    [Fact]
    public async Task Handle_AdminAttemptingUpload_IsDeniedEvenSameOrganization()
    {
        var targetUserId = Guid.NewGuid();
        var requirement = CreateRequirement(targetUserId, DriverRequirementType.LicenciaConducir, DriverRequirementStatus.Pendiente);
        var repository = new FakeDriverRequirementRepository { Requirements = [requirement] };
        var fileStorage = new FakeFileStorageService();
        // Even an Administrador calling with the chofer's own id as TargetUserId must be
        // denied: uploading is chofer-self-only by role, not just by id match.
        var handler = new UploadDriverRequirementDocumentCommandHandler(
            repository, fileStorage, new FakeCurrentUserService(targetUserId, "Administrador"));

        await Assert.ThrowsAsync<DriverRequirementAccessDeniedException>(
            () => handler.Handle(CreateCommand(targetUserId), TestContext.Current.CancellationToken));

        Assert.Empty(repository.UpdatedRequirements);
        Assert.Empty(fileStorage.SavedKeys);
    }

    [Fact]
    public async Task Handle_ChoferUploadingToAnotherChofersId_IsDenied()
    {
        var targetUserId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var repository = new FakeDriverRequirementRepository();
        var fileStorage = new FakeFileStorageService();
        var handler = new UploadDriverRequirementDocumentCommandHandler(
            repository, fileStorage, new FakeCurrentUserService(callerId, "Chofer"));

        await Assert.ThrowsAsync<DriverRequirementAccessDeniedException>(
            () => handler.Handle(CreateCommand(targetUserId), TestContext.Current.CancellationToken));

        Assert.Empty(fileStorage.SavedKeys);
    }

    [Fact]
    public async Task Handle_ReUpload_DeletesOldFileKey()
    {
        var targetUserId = Guid.NewGuid();
        var requirement = CreateRequirement(
            targetUserId, DriverRequirementType.LicenciaConducir, DriverRequirementStatus.Apto, fileKey: "driver-requirements/old-key.pdf");
        var repository = new FakeDriverRequirementRepository { Requirements = [requirement] };
        var fileStorage = new FakeFileStorageService();
        var handler = new UploadDriverRequirementDocumentCommandHandler(
            repository, fileStorage, new FakeCurrentUserService(targetUserId, "Chofer"));

        await handler.Handle(CreateCommand(targetUserId), TestContext.Current.CancellationToken);

        Assert.Contains("driver-requirements/old-key.pdf", fileStorage.DeletedKeys);
        Assert.Single(fileStorage.SavedKeys);
    }

    [Fact]
    public async Task Handle_MissingRequirementRow_ThrowsInvalidOperationException()
    {
        var targetUserId = Guid.NewGuid();
        var repository = new FakeDriverRequirementRepository(); // no rows at all - data invariant violation
        var fileStorage = new FakeFileStorageService();
        var handler = new UploadDriverRequirementDocumentCommandHandler(
            repository, fileStorage, new FakeCurrentUserService(targetUserId, "Chofer"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(CreateCommand(targetUserId), TestContext.Current.CancellationToken));
    }
}
