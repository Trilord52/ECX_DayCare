using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using Daycare.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace Daycare.Domain.Tests;

public class ProfilePhotoTests
{
    [Fact]
    public void ProfilePhoto_WithValidArguments_ShouldInstantiate()
    {
        var now = DateTime.UtcNow;
        var photo = new ProfilePhoto("children/c1.jpg", "image/jpeg", 12345, now);

        photo.Key.ShouldBe("children/c1.jpg");
        photo.ContentType.ShouldBe("image/jpeg");
        photo.SizeBytes.ShouldBe(12345);
        photo.UpdatedAt.ShouldBe(now);
    }

    [Theory]
    [InlineData("", "image/jpeg", 100)]
    [InlineData("   ", "image/jpeg", 100)]
    [InlineData("key.jpg", "", 100)]
    [InlineData("key.jpg", "   ", 100)]
    [InlineData("key.jpg", "image/jpeg", 0)]
    [InlineData("key.jpg", "image/jpeg", -10)]
    public void ProfilePhoto_WithInvalidArguments_ShouldThrowDomainRuleException(
        string key, string contentType, int sizeBytes)
    {
        Should.Throw<DomainRuleException>(() =>
            new ProfilePhoto(key, contentType, sizeBytes, DateTime.UtcNow));
    }

    [Fact]
    public void Child_SetPhoto_ShouldReturnPreviousKeyAndSetPhoto()
    {
        var child = new Child("John", "Doe", new DateOnly(2023, 1, 1), Gender.Male, new DateOnly(2024, 1, 1));
        var photo1 = new ProfilePhoto("children/c1.jpg", "image/jpeg", 5000, DateTime.UtcNow);
        var photo2 = new ProfilePhoto("children/c2.jpg", "image/jpeg", 6000, DateTime.UtcNow);

        var prev1 = child.SetPhoto(photo1);
        prev1.ShouldBeNull();
        child.Photo.ShouldBe(photo1);

        var prev2 = child.SetPhoto(photo2);
        prev2.ShouldBe("children/c1.jpg");
        child.Photo.ShouldBe(photo2);
    }

    [Fact]
    public void Child_RemovePhoto_ShouldReturnPreviousKeyAndClearPhoto()
    {
        var child = new Child("John", "Doe", new DateOnly(2023, 1, 1), Gender.Male, new DateOnly(2024, 1, 1));
        var photo = new ProfilePhoto("children/c1.jpg", "image/jpeg", 5000, DateTime.UtcNow);

        child.RemovePhoto().ShouldBeNull();

        child.SetPhoto(photo);
        var removedKey = child.RemovePhoto();
        removedKey.ShouldBe("children/c1.jpg");
        child.Photo.ShouldBeNull();

        child.RemovePhoto().ShouldBeNull();
    }

    [Fact]
    public void Guardian_SetPhotoAndRemovePhoto_ShouldWorkAsExpected()
    {
        var guardian = new Guardian("Jane", "Doe", "555-1234", "jane@example.com", "Address");
        var photo1 = new ProfilePhoto("guardians/g1.jpg", "image/jpeg", 5000, DateTime.UtcNow);
        var photo2 = new ProfilePhoto("guardians/g2.jpg", "image/jpeg", 6000, DateTime.UtcNow);

        guardian.SetPhoto(photo1).ShouldBeNull();
        guardian.Photo.ShouldBe(photo1);

        guardian.SetPhoto(photo2).ShouldBe("guardians/g1.jpg");
        guardian.Photo.ShouldBe(photo2);

        guardian.RemovePhoto().ShouldBe("guardians/g2.jpg");
        guardian.Photo.ShouldBeNull();
        guardian.RemovePhoto().ShouldBeNull();
    }

    [Fact]
    public void Staff_SetPhotoAndRemovePhoto_ShouldWorkAsExpected()
    {
        var staff = new Staff("Alice", "Smith", "Staff", "alice@example.com", "555-4321", new DateOnly(2023, 1, 1));
        var photo1 = new ProfilePhoto("staff/s1.jpg", "image/jpeg", 5000, DateTime.UtcNow);
        var photo2 = new ProfilePhoto("staff/s2.jpg", "image/jpeg", 6000, DateTime.UtcNow);

        staff.SetPhoto(photo1).ShouldBeNull();
        staff.Photo.ShouldBe(photo1);

        staff.SetPhoto(photo2).ShouldBe("staff/s1.jpg");
        staff.Photo.ShouldBe(photo2);

        staff.RemovePhoto().ShouldBe("staff/s2.jpg");
        staff.Photo.ShouldBeNull();
        staff.RemovePhoto().ShouldBeNull();
    }
}
