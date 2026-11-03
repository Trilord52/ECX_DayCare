using Daycare.Domain.Exceptions;
using Daycare.Infrastructure.Services;
using Shouldly;
using SkiaSharp;
using System.Text;
using Xunit;

namespace Daycare.Application.Tests.Photos;

public class SkiaImageProcessorTests
{
    private readonly SkiaImageProcessor _processor = new();

    private static byte[] CreateBitmapBytes(int width, int height, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 90);
        return data.ToArray();
    }

    [Fact]
    public async Task NormalizeAsync_ValidJpeg_ShouldReturnJpegUnder512px()
    {
        var inputBytes = CreateBitmapBytes(1200, 800, SKEncodedImageFormat.Jpeg);
        using var stream = new MemoryStream(inputBytes);

        var result = await _processor.NormalizeAsync(stream, 512, 85);

        result.SizeBytes.ShouldBeGreaterThan(0);
        result.Bytes.Length.ShouldBe(result.SizeBytes);

        using var decoded = SKBitmap.Decode(result.Bytes);
        decoded.ShouldNotBeNull();
        decoded.Width.ShouldBe(512);
        decoded.Height.ShouldBe(341);
    }

    [Fact]
    public async Task NormalizeAsync_ValidPngWithTransparency_ShouldFlattenToWhite()
    {
        using var bitmap = new SKBitmap(100, 100, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent); // completely transparent
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);

        using var stream = new MemoryStream(data.ToArray());
        var result = await _processor.NormalizeAsync(stream, 512, 85);

        using var decoded = SKBitmap.Decode(result.Bytes);
        decoded.ShouldNotBeNull();
        // Since transparent was drawn on white background, top-left pixel should be white (255, 255, 255)
        var pixel = decoded.GetPixel(10, 10);
        pixel.Red.ShouldBe((byte)255);
        pixel.Green.ShouldBe((byte)255);
        pixel.Blue.ShouldBe((byte)255);
    }

    [Fact]
    public async Task NormalizeAsync_ValidWebP_ShouldReturnJpeg()
    {
        var inputBytes = CreateBitmapBytes(200, 200, SKEncodedImageFormat.Webp);
        using var stream = new MemoryStream(inputBytes);

        var result = await _processor.NormalizeAsync(stream, 512, 85);

        using var decoded = SKBitmap.Decode(result.Bytes);
        decoded.ShouldNotBeNull();
        decoded.Width.ShouldBe(200);
        decoded.Height.ShouldBe(200);
    }

    [Fact]
    public async Task NormalizeAsync_JpegWithExif_ShouldStripExifMetadata()
    {
        // Construct a JPEG with an APP1 Exif segment containing GPS info
        var baseJpeg = CreateBitmapBytes(100, 100, SKEncodedImageFormat.Jpeg);

        // Standard JPEG starts with SOI: 0xFF, 0xD8
        // Insert APP1 marker: 0xFF, 0xE1, length (2 bytes), "Exif\0\0", and dummy GPS tags
        var exifPayload = Encoding.ASCII.GetBytes("Exif\0\0GPS_FIXTURE_LAT_LONG_DATA_12345678");
        var app1Length = (ushort)(exifPayload.Length + 2);
        var app1Marker = new byte[]
        {
            0xFF, 0xE1,
            (byte)(app1Length >> 8), (byte)(app1Length & 0xFF)
        };

        var jpegWithExif = new List<byte>();
        jpegWithExif.AddRange(baseJpeg.Take(2)); // SOI
        jpegWithExif.AddRange(app1Marker);
        jpegWithExif.AddRange(exifPayload);
        jpegWithExif.AddRange(baseJpeg.Skip(2)); // Remaining segments

        var inputBytes = jpegWithExif.ToArray();
        // Verify fixture has the EXIF payload
        Encoding.ASCII.GetString(inputBytes).ShouldContain("GPS_FIXTURE_LAT_LONG_DATA_12345678");

        using var stream = new MemoryStream(inputBytes);
        var result = await _processor.NormalizeAsync(stream, 512, 85);

        // Assert processed result does NOT contain any Exif GPS tags or APP1 marker
        Encoding.ASCII.GetString(result.Bytes).ShouldNotContain("GPS_FIXTURE");
        Encoding.ASCII.GetString(result.Bytes).ShouldNotContain("Exif");
    }

    [Fact]
    public async Task NormalizeAsync_EmptyStream_ShouldThrowValidationException()
    {
        using var stream = new MemoryStream([]);
        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            _processor.NormalizeAsync(stream, 512, 85));

        ex.Code.ShouldBe("VALIDATION_FAILED");
    }

    [Fact]
    public async Task NormalizeAsync_TextFileWithJpgExtension_ShouldThrowPhotoInvalidType()
    {
        var textBytes = Encoding.UTF8.GetBytes("This is not an image file at all.");
        using var stream = new MemoryStream(textBytes);

        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            _processor.NormalizeAsync(stream, 512, 85));

        ex.Code.ShouldBe("PHOTO_INVALID_TYPE");
    }

    [Fact]
    public async Task NormalizeAsync_Gif_ShouldThrowPhotoInvalidType()
    {
        // GIF89a magic bytes
        var gifBytes = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61, 0x10, 0x00, 0x10, 0x00 };
        using var stream = new MemoryStream(gifBytes);

        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            _processor.NormalizeAsync(stream, 512, 85));

        ex.Code.ShouldBe("PHOTO_INVALID_TYPE");
    }

    [Fact]
    public async Task NormalizeAsync_Svg_ShouldThrowPhotoInvalidType()
    {
        var svgBytes = Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><circle r=\"50\"/></svg>");
        using var stream = new MemoryStream(svgBytes);

        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            _processor.NormalizeAsync(stream, 512, 85));

        ex.Code.ShouldBe("PHOTO_INVALID_TYPE");
    }

    [Fact]
    public async Task NormalizeAsync_Html_ShouldThrowPhotoInvalidType()
    {
        var htmlBytes = Encoding.UTF8.GetBytes("<!DOCTYPE html><html><body><h1>Hello</h1></body></html>");
        using var stream = new MemoryStream(htmlBytes);

        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            _processor.NormalizeAsync(stream, 512, 85));

        ex.Code.ShouldBe("PHOTO_INVALID_TYPE");
    }

    [Fact]
    public async Task NormalizeAsync_TruncatedJpeg_ShouldThrowPhotoInvalid()
    {
        // Valid JPEG SOI + JFIF header prefix, but truncated before image data/EOI
        var truncated = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01 };
        using var stream = new MemoryStream(truncated);

        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            _processor.NormalizeAsync(stream, 512, 85));

        ex.Code.ShouldBe("PHOTO_INVALID");
    }

    [Fact]
    public async Task NormalizeAsync_DecompressionBombDeclaredDimensions_ShouldThrowPhotoInvalid()
    {
        var bombBytes = CreateBitmapBytes(8001, 10, SKEncodedImageFormat.Jpeg);
        using var stream = new MemoryStream(bombBytes);

        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            _processor.NormalizeAsync(stream, 512, 85));

        ex.Code.ShouldBe("PHOTO_INVALID");
    }

    [Fact]
    public async Task NormalizeAsync_DecompressionBombTotalPixels_ShouldThrowPhotoInvalid()
    {
        var bombBytes = CreateBitmapBytes(5100, 5000, SKEncodedImageFormat.Jpeg);
        using var stream = new MemoryStream(bombBytes);

        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            _processor.NormalizeAsync(stream, 512, 85));

        ex.Code.ShouldBe("PHOTO_INVALID");
    }
}
