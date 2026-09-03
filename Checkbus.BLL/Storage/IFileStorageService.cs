namespace Checkbus.BLL.Storage;

/// <summary>
/// Stores and retrieves attachment files referenced by
/// <see cref="Checkbus.BEL.Fleet.Attachment.StoragePath"/>. Decision: local
/// disk storage (not S3-compatible object storage) for the project's current
/// scale — see engram decision #30/#31. The base path is environment-
/// configurable (decision #41), so the same implementation works unchanged
/// against a developer's local disk or the VPS's local disk.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves <paramref name="content"/> under the given organization's folder
    /// and returns the relative <c>StoragePath</c> to persist on the owning
    /// <see cref="Checkbus.BEL.Fleet.Attachment"/> (e.g.
    /// "{organizationId}/drivers/{driverId}/license.pdf").
    /// </summary>
    Task<string> SaveAsync(
        int organizationId,
        string ownerRelativeFolder,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default);

    /// <summary>Opens a previously saved file for reading, given its relative <c>StoragePath</c>.</summary>
    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);

    /// <summary>Deletes a previously saved file, given its relative <c>StoragePath</c>. No-op if it doesn't exist.</summary>
    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}
