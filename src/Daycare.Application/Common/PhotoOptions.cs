namespace Daycare.Application.Common;

public class PhotoOptions
{
    public const string SectionName = "Photos";

    public string RootPath { get; set; } = "data/photos";
    public int MaxUploadBytes { get; set; } = 2_097_152;
    public int MaxDimension { get; set; } = 512;
    public int JpegQuality { get; set; } = 85;
}

