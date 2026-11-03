using Daycare.Application.Common;
using Daycare.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Text;
using Xunit;

namespace Daycare.Application.Tests.Photos;

public class LocalPhotoStorageTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly LocalPhotoStorage _storage;

    public LocalPhotoStorageTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "daycare_photos_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);

        var options = Options.Create(new PhotoOptions
        {
            RootPath = _tempDirectory,
            MaxUploadBytes = 2 * 1024 * 1024,
            MaxDimension = 512,
            JpegQuality = 85
        });

        _storage = new LocalPhotoStorage(options, null, NullLogger<LocalPhotoStorage>.Instance);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch
        {
            // Ignore cleanup failures
        }
    }

    [Fact]
    public async Task SaveAsync_And_OpenReadAsync_ShouldSaveAndRetrieveContent()
    {
        var key = "children/child_123.jpg";
        var payload = "sample photo payload";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

        await _storage.SaveAsync(key, stream);

        using var readStream = await _storage.OpenReadAsync(key);
        readStream.ShouldNotBeNull();

        using var reader = new StreamReader(readStream);
        var readText = await reader.ReadToEndAsync();
        readText.ShouldBe(payload);
    }

    [Fact]
    public async Task OpenReadAsync_WhenFileNotFound_ShouldReturnNull()
    {
        var stream = await _storage.OpenReadAsync("children/non_existent.jpg");
        stream.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteFileAndBeIdempotent()
    {
        var key = "guardians/guardian_456.jpg";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("test"));

        await _storage.SaveAsync(key, stream);

        var existsBefore = await _storage.OpenReadAsync(key);
        existsBefore.ShouldNotBeNull();
        existsBefore.Dispose();

        await _storage.DeleteAsync(key);

        var existsAfter = await _storage.OpenReadAsync(key);
        existsAfter.ShouldBeNull();

        // Second delete is a no-op / idempotent
        await _storage.DeleteAsync(key);
    }

    [Theory]
    [InlineData("../escape.jpg")]
    [InlineData("../../etc/passwd")]
    [InlineData("sub/../../escape.jpg")]
    public async Task SaveAsync_WithPathTraversal_ShouldThrowInvalidOperationException(string maliciousKey)
    {
        using var stream = new MemoryStream([1, 2, 3]);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            _storage.SaveAsync(maliciousKey, stream));
    }
}

