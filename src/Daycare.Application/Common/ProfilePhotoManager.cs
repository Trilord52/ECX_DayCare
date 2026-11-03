using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Common.Models;
using Daycare.Domain.Exceptions;
using Daycare.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Daycare.Application.Common;

public class ProfilePhotoManager
{
    private readonly IPhotoStorage _photoStorage;
    private readonly IImageProcessor _imageProcessor;
    private readonly PhotoOptions _options;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ProfilePhotoManager> _logger;

    public ProfilePhotoManager(
        IPhotoStorage photoStorage,
        IImageProcessor whirlwindImageProcessor,
        IOptions<PhotoOptions> options,
        IDateTimeProvider dateTimeProvider,
        ILogger<ProfilePhotoManager> logger)
    {
        _photoStorage = photoStorage;
        _imageProcessor = whirlwindImageProcessor;
        _options = options.Value;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<(ProfilePhoto Photo, string Key)> UploadAndProcessAsync(
        string category,
        Guid ownerId,
        Stream content,
        long? contentLength,
        CancellationToken ct = default)
    {
        if (content == null || (contentLength.HasValue && contentLength.Value == 0) || (content.CanSeek && content.Length == 0))
        {
            throw new ValidationFailedException("file", "File is empty.");
        }

        if (contentLength.HasValue && contentLength.Value > _options.MaxUploadBytes)
        {
            throw new DomainRuleException("PHOTO_TOO_LARGE", $"File exceeds maximum allowed size of {_options.MaxUploadBytes} bytes.");
        }

        if (content.CanSeek && content.Length > _options.MaxUploadBytes)
        {
            throw new DomainRuleException("PHOTO_TOO_LARGE", $"File exceeds maximum allowed size of {_options.MaxUploadBytes} bytes.");
        }

        var processed = await _imageProcessor.NormalizeAsync(content, _options.MaxDimension, _options.JpegQuality, ct);

        var key = $"{category}/{ownerId}/{Guid.NewGuid():N}.jpg";
        await _photoStorage.SaveAsync(key, new MemoryStream(processed.Bytes), ct);

        var photo = new ProfilePhoto(key, "image/jpeg", processed.SizeBytes, _dateTimeProvider.UtcNow);
        return (photo, key);
    }

    public async Task DeleteStoredPhotoAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        try
        {
            await _photoStorage.DeleteAsync(key);
        }
        catch (Exception ex)
        {
            // BR-33: Logs never contain image bytes, storage paths, or original file names.
            _logger.LogWarning(ex, "Failed to delete stored photo file.");
        }
    }

    public async Task<PhotoDownloadResult> GetPhotoAsync(ProfilePhoto? photo, CancellationToken ct = default)
    {
        if (photo == null)
        {
            throw new NotFoundException("Profile photo not found.");
        }

        var stream = await _photoStorage.OpenReadAsync(photo.Key, ct);
        if (stream == null)
        {
            throw new NotFoundException("Profile photo file not found.");
        }

        return new PhotoDownloadResult(stream, photo.ContentType, photo.UpdatedAt);
    }
}

