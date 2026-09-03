using System.Text;
using Checkbus.BLL.Storage;

namespace Checkbus.Tests.Unit;

/// <summary>
/// S3.6 — <see cref="LocalDiskFileStorageService"/>: local-disk storage
/// (decision #30/#31), organized by <c>OrganizationId</c>, base path
/// environment-configurable (decision #41).
/// </summary>
public class LocalDiskFileStorageServiceTests : IDisposable
{
    private readonly string _basePath = Path.Combine(Path.GetTempPath(), "checkbus-tests-" + Guid.NewGuid());

    [Fact]
    public async Task SaveAsync_Writes_The_File_Under_The_Organization_Folder()
    {
        var sut = new LocalDiskFileStorageService(_basePath);
        var content = new MemoryStream(Encoding.UTF8.GetBytes("license contents"));

        var storagePath = await sut.SaveAsync(5, "drivers/12", "license.pdf", content);

        Assert.Equal("5/drivers/12/license.pdf", storagePath);
        var fullPath = Path.Combine(_basePath, "5", "drivers", "12", "license.pdf");
        Assert.True(File.Exists(fullPath));
        Assert.Equal("license contents", await File.ReadAllTextAsync(fullPath));
    }

    [Fact]
    public async Task OpenReadAsync_Reads_Back_A_Previously_Saved_File()
    {
        var sut = new LocalDiskFileStorageService(_basePath);
        var content = new MemoryStream(Encoding.UTF8.GetBytes("vtv scan"));
        var storagePath = await sut.SaveAsync(3, "vehicles/1/documentation/2", "vtv.pdf", content);

        await using var stream = await sut.OpenReadAsync(storagePath);
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync();

        Assert.Equal("vtv scan", text);
    }

    [Fact]
    public async Task DeleteAsync_Removes_A_Previously_Saved_File()
    {
        var sut = new LocalDiskFileStorageService(_basePath);
        var content = new MemoryStream(Encoding.UTF8.GetBytes("temp"));
        var storagePath = await sut.SaveAsync(1, "drivers/9", "id.jpg", content);

        await sut.DeleteAsync(storagePath);

        var fullPath = Path.Combine(_basePath, "1", "drivers", "9", "id.jpg");
        Assert.False(File.Exists(fullPath));
    }

    [Fact]
    public async Task DeleteAsync_Is_A_NoOp_When_The_File_Does_Not_Exist()
    {
        var sut = new LocalDiskFileStorageService(_basePath);

        await sut.DeleteAsync("1/drivers/999/missing.pdf");
    }

    public void Dispose()
    {
        if (Directory.Exists(_basePath))
        {
            Directory.Delete(_basePath, recursive: true);
        }
    }
}
