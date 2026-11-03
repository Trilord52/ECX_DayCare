namespace Daycare.Application.Common.Models;

public record PhotoUploadResultDto(bool HasPhoto, DateTime? PhotoUpdatedAt);

public record PhotoDownloadResult(Stream Stream, string ContentType, DateTime UpdatedAt);

