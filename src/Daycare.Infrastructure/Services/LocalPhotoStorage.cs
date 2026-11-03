using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Daycare.Infrastructure.Services;

public class LocalPhotoStorage : IPhotoStorage
{
    private readonly string _rootPath;
    private readonly ILogger<LocalPhotoStorage> _logger;

    public LocalPhotoStorage(
        IOptions<PhotoOptions> options,
        IHostEnvironment? environment,
        ILogger<LocalPhotoStorage> logger)
    {
        _logger = logger;
        var configuredPath = options?.Value?.RootPath ?? "data/photos";
        var contentRoot = environment?.ContentRootPath ?? AppContext.BaseDirectory;
        var fullPath = Path.IsPathRooted(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(Path.Combine(contentRoot, configuredPath));

        if (!fullPath.EndsWith(Path.DirectorySeparatorChar))
        {
            fullPath += Path.DirectorySeparatorChar;
        }

        _rootPath = fullPath;
    }

    public async Task SaveAsync(string key, Stream content, CancellationToken ct = default)
    {
        var resolvedPath = GetSecurePath(key);
        var dir = Path.GetDirectoryName(resolvedPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        await using var fileStream = new FileStream(resolvedPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await content.CopyToAsync(fileStream, ct);
    }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken ct = default)
    {
        var resolvedPath = GetSecurePath(key);
        if (!File.Exists(resolvedPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(resolvedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var resolvedPath = GetSecurePath(key);
            if (File.Exists(resolvedPath))
            {
                File.Delete(resolvedPath);
            }
        }
        catch (Exception ex)
        {
            // BR-33: Logs never contain image bytes, storage paths, or original file names.
            _logger.LogWarning(ex, "Failed to delete stored photo file.");
        }

        return Task.CompletedTask;
    }

    private string GetSecurePath(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        var normalizedKey = key.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar).TrimStart(Path.DirectorySeparatorChar);
        var resolvedPath = Path.GetFullPath(Path.Combine(_rootPath, normalizedKey));

        if (!resolvedPath.StartsWith(_rootPath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid storage key: path traversal detected.");
        }

        return resolvedPath;
    }
}

