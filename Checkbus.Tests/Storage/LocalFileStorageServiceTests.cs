using Checkbus.ApiService.Domain.Exceptions.Storage;
using Checkbus.ApiService.Infrastructure.Implementations.Storage;

namespace Checkbus.Tests.Storage;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _root;

    public LocalFileStorageServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "checkbus-storage-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private LocalFileStorageService CreateSut(string? root = null)
        => new(new FileStorageOptions { LocalRootPath = root ?? _root });

    public static IEnumerable<object[]> RelativeTraversalKeys =>
        new List<object[]>
        {
            new object[] { "../../etc/passwd" },
            new object[] { "a/../../b" },
        };

    public static IEnumerable<object[]> RootedKeys =>
        new List<object[]>
        {
            new object[] { "C:\\secrets\\file.txt" },
            new object[] { "/etc/passwd" },
        };

    public static IEnumerable<object[]> EmptyOrWhitespaceKeys =>
        new List<object[]>
        {
            new object[] { "" },
            new object[] { "   " },
        };

    [Theory]
    [MemberData(nameof(RelativeTraversalKeys))]
    public async Task SaveAsync_RelativeTraversalKey_ThrowsAndLeavesFilesystemUntouched(string key)
    {
        var sut = CreateSut();
        using var content = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<InvalidStorageKeyException>(
            () => sut.SaveAsync(key, content, "application/octet-stream", TestContext.Current.CancellationToken));

        Assert.Empty(Directory.GetFileSystemEntries(_root));
    }

    [Theory]
    [MemberData(nameof(RootedKeys))]
    public async Task SaveAsync_RootedKey_ThrowsAndLeavesFilesystemUntouched(string key)
    {
        var sut = CreateSut();
        using var content = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<InvalidStorageKeyException>(
            () => sut.SaveAsync(key, content, "application/octet-stream", TestContext.Current.CancellationToken));

        Assert.Empty(Directory.GetFileSystemEntries(_root));
    }

    [Theory]
    [MemberData(nameof(EmptyOrWhitespaceKeys))]
    public async Task SaveAsync_EmptyOrWhitespaceKey_ThrowsAndLeavesFilesystemUntouched(string key)
    {
        var sut = CreateSut();
        using var content = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<InvalidStorageKeyException>(
            () => sut.SaveAsync(key, content, "application/octet-stream", TestContext.Current.CancellationToken));

        Assert.Empty(Directory.GetFileSystemEntries(_root));
    }

    [Fact]
    public async Task SaveAsync_SiblingPrefixRoot_ThrowsAndLeavesFilesystemUntouched()
    {
        var parent = Path.Combine(Path.GetTempPath(), "checkbus-storage-tests", Guid.NewGuid().ToString());
        var root = Path.Combine(parent, "storage");
        Directory.CreateDirectory(root);
        try
        {
            var sut = new LocalFileStorageService(new FileStorageOptions { LocalRootPath = root });
            using var content = new MemoryStream([9]);

            await Assert.ThrowsAsync<InvalidStorageKeyException>(
                () => sut.SaveAsync("../storage-evil/file.txt", content, "text/plain", TestContext.Current.CancellationToken));

            var evilPath = Path.Combine(parent, "storage-evil", "file.txt");
            Assert.False(File.Exists(evilPath));
            Assert.Empty(Directory.GetFileSystemEntries(root));
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public async Task ExistsAsync_DriveRootConfiguredRoot_ValidKeyDoesNotFalselyReject()
    {
        var driveRoot = Path.GetPathRoot(Path.GetTempPath())!;
        var sut = new LocalFileStorageService(new FileStorageOptions { LocalRootPath = driveRoot });

        var exists = await sut.ExistsAsync(
            "checkbus-storage-tests-nonexistent-key.txt", TestContext.Current.CancellationToken);

        Assert.False(exists);
    }

    [Fact]
    public async Task RoundTrip_SaveExistsOpenReadDelete_BehavesAsExpected()
    {
        var sut = CreateSut();
        var bytes = new byte[] { 10, 20, 30, 40 };
        var cancellationToken = TestContext.Current.CancellationToken;

        using (var content = new MemoryStream(bytes))
        {
            var returnedKey = await sut.SaveAsync("documents/report.pdf", content, "application/pdf", cancellationToken);
            Assert.Equal("documents/report.pdf", returnedKey);
        }

        Assert.True(await sut.ExistsAsync("documents/report.pdf", cancellationToken));

        await using (var read = await sut.OpenReadAsync("documents/report.pdf", cancellationToken))
        {
            Assert.NotNull(read);
            using var buffer = new MemoryStream();
            await read!.CopyToAsync(buffer, cancellationToken);
            Assert.Equal(bytes, buffer.ToArray());
        }

        await sut.DeleteAsync("documents/report.pdf", cancellationToken);

        Assert.False(await sut.ExistsAsync("documents/report.pdf", cancellationToken));
    }

    [Fact]
    public async Task OpenReadAsync_MissingKey_ReturnsNull()
    {
        var sut = CreateSut();

        var result = await sut.OpenReadAsync("never-saved.txt", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_NestedKey_CreatesIntermediateDirectories()
    {
        var sut = CreateSut();
        using var content = new MemoryStream([1]);

        await sut.SaveAsync("a/b/c/nested.bin", content, "application/octet-stream", TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(_root, "a", "b", "c", "nested.bin")));
    }

    [Fact]
    public async Task DeleteAsync_MissingKey_DoesNotThrow()
    {
        var sut = CreateSut();

        var exception = await Record.ExceptionAsync(
            () => sut.DeleteAsync("does-not-exist.txt", TestContext.Current.CancellationToken));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SaveAsync_ExistingKey_OverwritesPriorContent()
    {
        var sut = CreateSut();
        var cancellationToken = TestContext.Current.CancellationToken;

        using (var first = new MemoryStream([1, 1, 1]))
            await sut.SaveAsync("overwrite.txt", first, "text/plain", cancellationToken);

        using (var second = new MemoryStream([2, 2]))
            await sut.SaveAsync("overwrite.txt", second, "text/plain", cancellationToken);

        await using var read = await sut.OpenReadAsync("overwrite.txt", cancellationToken);
        using var buffer = new MemoryStream();
        await read!.CopyToAsync(buffer, cancellationToken);
        Assert.Equal(new byte[] { 2, 2 }, buffer.ToArray());
    }
}
