using Daycare.Application.Abstractions;
using Daycare.Domain.Exceptions;
using SkiaSharp;

namespace Daycare.Infrastructure.Services;

public class SkiaImageProcessor : IImageProcessor
{
    public async Task<ProcessedImage> NormalizeAsync(Stream input, int maxDimension, int quality, CancellationToken ct = default)
    {
        if (input == null || input.Length == 0)
        {
            throw new DomainRuleException("VALIDATION_FAILED", "Image stream is empty.");
        }

        byte[] buffer;
        if (input is MemoryStream ms)
        {
            buffer = ms.ToArray();
        }
        else
        {
            using var memoryStream = new MemoryStream();
            await input.CopyToAsync(memoryStream, ct);
            buffer = memoryStream.ToArray();
        }

        if (buffer.Length == 0)
        {
            throw new DomainRuleException("VALIDATION_FAILED", "Image stream is empty.");
        }

        // 3. Magic bytes validation (JPEG, PNG, WebP)
        if (!IsValidMagicBytes(buffer))
        {
            throw new DomainRuleException("PHOTO_INVALID_TYPE", "File type must be JPEG, PNG, or WebP.");
        }

        using var skData = SKData.CreateCopy(buffer);
        using var codec = SKCodec.Create(skData);
        if (codec is null)
        {
            throw new DomainRuleException("PHOTO_INVALID", "Image cannot be decoded.");
        }

        var width = codec.Info.Width;
        var height = codec.Info.Height;

        // 4. Declared image dimensions above 8000 x 8000, or above 25 million pixels
        if (width <= 0 || height <= 0 || width > 8000 || height > 8000 || (long)width * height > 25_000_000)
        {
            throw new DomainRuleException("PHOTO_INVALID", "Image exceeds maximum allowed dimensions or pixel count.");
        }

        // 5. Decode to SKBitmap
        using var originalBitmap = SKBitmap.Decode(codec);
        if (originalBitmap is null)
        {
            throw new DomainRuleException("PHOTO_INVALID", "Image cannot be decoded.");
        }

        // Downscale to at most maxDimension on longest side
        int targetWidth = originalBitmap.Width;
        int targetHeight = originalBitmap.Height;

        if (originalBitmap.Width > maxDimension || originalBitmap.Height > maxDimension)
        {
            if (originalBitmap.Width >= originalBitmap.Height)
            {
                targetWidth = maxDimension;
                targetHeight = Math.Max(1, (int)Math.Round((double)originalBitmap.Height * maxDimension / originalBitmap.Width));
            }
            else
            {
                targetHeight = maxDimension;
                targetWidth = Math.Max(1, (int)Math.Round((double)originalBitmap.Width * maxDimension / originalBitmap.Height));
            }
        }

        SKBitmap workingBitmap;
        bool resized = false;
        if (targetWidth != originalBitmap.Width || targetHeight != originalBitmap.Height)
        {
            var resizeInfo = new SKImageInfo(targetWidth, targetHeight, originalBitmap.ColorType, originalBitmap.AlphaType);
            var scaled = originalBitmap.Resize(resizeInfo, SKSamplingOptions.Default);
            if (scaled is null)
            {
                throw new DomainRuleException("PHOTO_INVALID", "Failed to resize image.");
            }
            workingBitmap = scaled;
            resized = true;
        }
        else
        {
            workingBitmap = originalBitmap;
        }

        SKBitmap finalBitmap = workingBitmap;
        bool compositeCreated = false;
        try
        {
            // If the image has alpha, flatten onto white background to avoid black boxes when saving to JPEG
            if (workingBitmap.AlphaType != SKAlphaType.Opaque)
            {
                var opaqueBitmap = new SKBitmap(targetWidth, targetHeight, SKColorType.Rgba8888, SKAlphaType.Opaque);
                using var canvas = new SKCanvas(opaqueBitmap);
                canvas.Clear(SKColors.White);
                using var tempImg = SKImage.FromBitmap(workingBitmap);
                canvas.DrawImage(tempImg, 0, 0, SKSamplingOptions.Default);
                finalBitmap = opaqueBitmap;
                compositeCreated = true;
            }

            using var image = SKImage.FromBitmap(finalBitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, quality);
            if (encoded is null)
            {
                throw new DomainRuleException("PHOTO_INVALID", "Failed to encode image to JPEG.");
            }

            var bytes = encoded.ToArray();
            return new ProcessedImage(bytes, bytes.Length);
        }
        finally
        {
            if (compositeCreated)
            {
                finalBitmap.Dispose();
            }
            if (resized)
            {
                workingBitmap.Dispose();
            }
        }
    }

    private static bool IsValidMagicBytes(byte[] bytes)
    {
        if (bytes.Length < 12)
            return false;

        // JPEG: FF D8 FF
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            return true;

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
            return true;

        // WebP: RIFF (bytes 0..3) and WEBP (bytes 8..11)
        if (bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
            bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
            return true;

        return false;
    }
}
