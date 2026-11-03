using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Children.DeleteChild;
using Daycare.Application.Features.Children.Photos;
using Daycare.Application.Features.Guardians.DeleteGuardian;
using Daycare.Application.Features.Guardians.Photos;
using Daycare.Application.Features.Staff.DeleteStaff;
using Daycare.Application.Features.Staff.Photos;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using Daycare.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Photos;

public class PhotoUseCaseTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IAccessGuard _accessGuard = Substitute.For<IAccessGuard>();
    private readonly IPhotoStorage _photoStorage = Substitute.For<IPhotoStorage>();
    private readonly IImageProcessor _imageProcessor = Substitute.For<IImageProcessor>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly ProfilePhotoManager _photoManager;
    private readonly PhotoOptions _options;
    private readonly DateTime _nowUtc = new(2026, 4, 15, 12, 0, 0, DateTimeKind.Utc);

    public PhotoUseCaseTests()
    {
        _options = new PhotoOptions
        {
            RootPath = "data/photos",
            MaxUploadBytes = 2 * 1024 * 1024,
            MaxDimension = 512,
            JpegQuality = 85
        };

        _dateTimeProvider.UtcNow.Returns(_nowUtc);

        _photoManager = new ProfilePhotoManager(
            _photoStorage,
            _imageProcessor,
            Options.Create(_options),
            _dateTimeProvider,
            NullLogger<ProfilePhotoManager>.Instance);
    }

    [Theory]
    [InlineData("children")]
    [InlineData("guardians")]
    [InlineData("staff")]
    public async Task Upload_ValidImages_ShouldSucceedWithCorrectPrefix(string category)
    {
        var ownerId = Guid.NewGuid();
        var processedBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xEE, 0x01, 0x02, 0x03, 0x04 };
        _imageProcessor.NormalizeAsync(Arg.Any<Stream>(), 512, 85, Arg.Any<CancellationToken>())
            .Returns(new ProcessedImage(processedBytes, processedBytes.Length));

        using var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        var (photo, key) = await _photoManager.UploadAndProcessAsync(category, ownerId, stream, stream.Length);

        key.ShouldStartWith($"{category}/{ownerId}/");
        key.ShouldEndWith(".jpg");
        photo.Key.ShouldBe(key);
        photo.ContentType.ShouldBe("image/jpeg");
        photo.SizeBytes.ShouldBe(processedBytes.Length);
        photo.UpdatedAt.ShouldBe(_nowUtc);

        await _photoStorage.Received(1).SaveAsync(key, Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Upload_Exceeding2MB_ShouldThrowPhotoTooLarge()
    {
        var ownerId = Guid.NewGuid();
        using var stream = new MemoryStream(new byte[100]);

        var ex = await Should.ThrowAsync<DomainRuleException>(() =>
            _photoManager.UploadAndProcessAsync("children", ownerId, stream, 2 * 1024 * 1024 + 1));

        ex.Code.ShouldBe("PHOTO_TOO_LARGE");
    }

    [Fact]
    public async Task Upload_EmptyStream_ShouldThrowValidationFailed()
    {
        var ownerId = Guid.NewGuid();
        using var stream = new MemoryStream([]);

        var ex = await Should.ThrowAsync<ValidationFailedException>(() =>
            _photoManager.UploadAndProcessAsync("children", ownerId, stream, 0));

        ex.Code.ShouldBe("VALIDATION_FAILED");
    }

    [Fact]
    public async Task SetChildPhoto_Replace_ShouldDeleteOldFileAfterNewOneSaved()
    {
        var childId = Guid.NewGuid();
        var child = new Child("Abel", "Tesfaye", new DateOnly(2023, 1, 1), Gender.Male, new DateOnly(2024, 1, 1));
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(child, childId);

        var oldPhoto = new ProfilePhoto($"children/{childId}/old.jpg", "image/jpeg", 1000, _nowUtc.AddDays(-1));
        child.SetPhoto(oldPhoto);

        var childrenList = new List<Child> { child };
        var childrenDbSet = MockDbSetHelper.CreateMockDbSet(childrenList);
        _dbContext.Children.Returns(childrenDbSet);

        var processedBytes = new byte[] { 0xFF, 0xD8, 0xFF };
        _imageProcessor.NormalizeAsync(Arg.Any<Stream>(), 512, 85, Arg.Any<CancellationToken>())
            .Returns(new ProcessedImage(processedBytes, processedBytes.Length));

        var useCase = new SetChildPhotoUseCase(_dbContext, _accessGuard, _photoManager);
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        var result = await useCase.ExecuteAsync(new SetChildPhotoRequest(childId, stream, stream.Length));

        result.HasPhoto.ShouldBeTrue();
        child.Photo.ShouldNotBeNull();
        child.Photo.Key.ShouldNotBe(oldPhoto.Key);

        // Old photo was deleted from storage
        await _photoStorage.Received(1).DeleteAsync(oldPhoto.Key, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetChildPhoto_WhenDatabaseSaveFails_ShouldDeleteNewFile()
    {
        var childId = Guid.NewGuid();
        var child = new Child("Abel", "Tesfaye", new DateOnly(2023, 1, 1), Gender.Male, new DateOnly(2024, 1, 1));
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(child, childId);

        var childrenList = new List<Child> { child };
        var childrenDbSet = MockDbSetHelper.CreateMockDbSet(childrenList);
        _dbContext.Children.Returns(childrenDbSet);
        _dbContext.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("DB connection dropped"));

        var processedBytes = new byte[] { 0xFF, 0xD8, 0xFF };
        _imageProcessor.NormalizeAsync(Arg.Any<Stream>(), 512, 85, Arg.Any<CancellationToken>())
            .Returns(new ProcessedImage(processedBytes, processedBytes.Length));

        var useCase = new SetChildPhotoUseCase(_dbContext, _accessGuard, _photoManager);
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        await Should.ThrowAsync<InvalidOperationException>(() =>
            useCase.ExecuteAsync(new SetChildPhotoRequest(childId, stream, stream.Length)));

        // New photo was saved then deleted due to DB failure
        await _photoStorage.Received(1).DeleteAsync(
            Arg.Is<string>(k => k.StartsWith($"children/{childId}/")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveChildPhoto_ShouldDeleteFileAndBeIdempotent()
    {
        var childId = Guid.NewGuid();
        var child = new Child("Abel", "Tesfaye", new DateOnly(2023, 1, 1), Gender.Male, new DateOnly(2024, 1, 1));
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(child, childId);

        var photo = new ProfilePhoto($"children/{childId}/photo.jpg", "image/jpeg", 1000, _nowUtc);
        child.SetPhoto(photo);

        var childrenList = new List<Child> { child };
        var childrenDbSet = MockDbSetHelper.CreateMockDbSet(childrenList);
        _dbContext.Children.Returns(childrenDbSet);

        var useCase = new RemoveChildPhotoUseCase(_dbContext, _accessGuard, _photoManager);

        // First remove
        var res1 = await useCase.ExecuteAsync(new RemoveChildPhotoRequest(childId));
        res1.HasPhoto.ShouldBeFalse();
        child.Photo.ShouldBeNull();
        await _photoStorage.Received(1).DeleteAsync(photo.Key, Arg.Any<CancellationToken>());

        // Second remove (idempotent 204)
        var res2 = await useCase.ExecuteAsync(new RemoveChildPhotoRequest(childId));
        res2.HasPhoto.ShouldBeFalse();
    }

    [Fact]
    public async Task SoftDeleteChild_ShouldRemovePhotoAndFile()
    {
        var childId = Guid.NewGuid();
        var child = new Child("Abel", "Tesfaye", new DateOnly(2023, 1, 1), Gender.Male, new DateOnly(2024, 1, 1));
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(child, childId);

        var photo = new ProfilePhoto($"children/{childId}/photo.jpg", "image/jpeg", 1000, _nowUtc);
        child.SetPhoto(photo);

        var childrenList = new List<Child> { child };
        var childrenDbSet = MockDbSetHelper.CreateMockDbSet(childrenList);
        _dbContext.Children.Returns(childrenDbSet);

        var useCase = new DeleteChildUseCase(_dbContext, _dateTimeProvider, _photoManager);
        await useCase.ExecuteAsync(new DeleteChildRequest(childId));

        child.IsDeleted.ShouldBeTrue();
        child.Photo.ShouldBeNull();
        await _photoStorage.Received(1).DeleteAsync(photo.Key, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SoftDeleteGuardian_ShouldRemovePhotoAndFile()
    {
        var guardianId = Guid.NewGuid();
        var guardian = new Guardian("Jane", "Doe", "555-1234", "jane@example.com", "Address");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(guardian, guardianId);

        var photo = new ProfilePhoto($"guardians/{guardianId}/photo.jpg", "image/jpeg", 1000, _nowUtc);
        guardian.SetPhoto(photo);

        var guardiansList = new List<Guardian> { guardian };
        var guardiansDbSet = MockDbSetHelper.CreateMockDbSet(guardiansList);
        _dbContext.Guardians.Returns(guardiansDbSet);
        var childGuardiansDbSet = MockDbSetHelper.CreateMockDbSet<ChildGuardian>([]);
        _dbContext.ChildGuardians.Returns(childGuardiansDbSet);

        var useCase = new DeleteGuardianUseCase(_dbContext, _dateTimeProvider, _identityService, _photoManager);
        await useCase.ExecuteAsync(new DeleteGuardianRequest(guardianId));

        guardian.IsDeleted.ShouldBeTrue();
        guardian.Photo.ShouldBeNull();
        await _photoStorage.Received(1).DeleteAsync(photo.Key, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SoftDeleteStaff_ShouldRemovePhotoAndFile()
    {
        var staffId = Guid.NewGuid();
        var staff = new Staff("Alice", "Smith", "Staff", "alice@example.com", "555-4321", new DateOnly(2023, 1, 1));
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(staff, staffId);

        var photo = new ProfilePhoto($"staff/{staffId}/photo.jpg", "image/jpeg", 1000, _nowUtc);
        staff.SetPhoto(photo);

        var staffList = new List<Staff> { staff };
        var staffDbSet = MockDbSetHelper.CreateMockDbSet(staffList);
        _dbContext.Staff.Returns(staffDbSet);

        var useCase = new DeleteStaffUseCase(_dbContext, _dateTimeProvider, _identityService, _photoManager);
        await useCase.ExecuteAsync(new DeleteStaffRequest(staffId));

        staff.IsDeleted.ShouldBeTrue();
        staff.Photo.ShouldBeNull();
        await _photoStorage.Received(1).DeleteAsync(photo.Key, Arg.Any<CancellationToken>());
    }
}
