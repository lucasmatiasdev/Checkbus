using Checkbus.ApiService.Application.Interfaces.Storage;
using Checkbus.ApiService.Domain.Exceptions.Storage;

namespace Checkbus.ApiService.Infrastructure.Implementations.Storage
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly string _root;
        private readonly string _rootPrefix;

        public LocalFileStorageService(FileStorageOptions options)
        {
            _root = options.LocalRootPath;

            if (!Path.IsPathFullyQualified(_root))
                throw new InvalidOperationException("FileStorage LocalRootPath must be an absolute, fully qualified path.");

            _rootPrefix = _root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        }

        public async Task<string> SaveAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
        {
            var path = ResolveKey(key);

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            await using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            await content.CopyToAsync(fileStream, cancellationToken);

            return key;
        }

        public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
        {
            var path = ResolveKey(key);

            if (!File.Exists(path))
                return Task.FromResult<Stream?>(null);

            return Task.FromResult<Stream?>(new FileStream(path, FileMode.Open, FileAccess.Read));
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
        {
            var path = ResolveKey(key);
            return Task.FromResult(File.Exists(path));
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            var path = ResolveKey(key);

            if (File.Exists(path))
                File.Delete(path);

            return Task.CompletedTask;
        }

        private string ResolveKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidStorageKeyException();

            if (Path.IsPathRooted(key))
                throw new InvalidStorageKeyException();

            string full;
            try
            {
                full = Path.GetFullPath(key, _root);
            }
            catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
            {
                throw new InvalidStorageKeyException();
            }

            if (!full.StartsWith(_rootPrefix, StringComparison.Ordinal))
                throw new InvalidStorageKeyException();

            return full;
        }
    }
}
