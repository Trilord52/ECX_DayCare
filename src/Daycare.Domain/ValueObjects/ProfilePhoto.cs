using Daycare.Domain.Exceptions;

namespace Daycare.Domain.ValueObjects;

public record ProfilePhoto
{
    public string Key { get; init; } = null!;
    public string ContentType { get; init; } = null!;
    public int SizeBytes { get; init; }
    public DateTime UpdatedAt { get; init; }

    private ProfilePhoto() { }

    public ProfilePhoto(string key, string contentType, int sizeBytes, DateTime updatedAt)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainRuleException("VALIDATION_FAILED", "Photo key is required.");
        if (string.IsNullOrWhiteSpace(contentType))
            throw new DomainRuleException("VALIDATION_FAILED", "Photo content type is required.");
        if (sizeBytes <= 0)
            throw new DomainRuleException("VALIDATION_FAILED", "Photo size must be greater than zero.");

        Key = key.Trim();
        ContentType = contentType.Trim();
        SizeBytes = sizeBytes;
        UpdatedAt = updatedAt;
    }
}

