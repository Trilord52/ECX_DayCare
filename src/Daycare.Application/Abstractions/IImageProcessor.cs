namespace Daycare.Application.Abstractions;

public record ProcessedImage(byte[] Bytes, int SizeBytes);

public interface IImageProcessor
{
    Task<ProcessedImage> NormalizeAsync(Stream input, int maxDimension, int quality, CancellationToken ct = default);
}

