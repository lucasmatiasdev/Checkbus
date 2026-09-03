namespace Checkbus.BLL.Storage;

/// <summary>
/// Default <see cref="IFileStorageService"/>: local disk storage rooted at
/// <see cref="_basePath"/>, organized <c>{basePath}/{organizationId}/{ownerRelativeFolder}/{fileName}</c>.
/// <paramref name="_basePath"/> is environment-configurable (decision #41):
/// a developer's own machine locally, the VPS's disk once deployed — no code
/// change needed, only a configuration value swap.
/// </summary>
public class LocalDiskFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalDiskFileStorageService(string basePath)
    {
        _basePath = basePath;
    }

    public async Task<string> SaveAsync(
        int organizationId,
        string ownerRelativeFolder,
        string fileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var storagePath = BuildStoragePath(organizationId, ownerRelativeFolder, fileName);
        var fullPath = ToFullPath(storagePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return storagePath;
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        Stream stream = File.OpenRead(ToFullPath(storagePath));
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ToFullPath(storagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private static string BuildStoragePath(int organizationId, string ownerRelativeFolder, string fileName) =>
        string.Join('/', organizationId.ToString(), ownerRelativeFolder.Trim('/'), fileName);

    private string ToFullPath(string storagePath) =>
        Path.Combine(_basePath, storagePath.Replace('/', Path.DirectorySeparatorChar));
}
