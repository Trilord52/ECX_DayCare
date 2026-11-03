using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Announcements.CreateAnnouncement;
using Daycare.Application.Features.Announcements.DeleteAnnouncement;
using Daycare.Application.Features.Announcements.GetAnnouncements;
using Daycare.Application.Features.Announcements.UpdateAnnouncement;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Announcements;

public class AnnouncementsUseCaseTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private readonly Guid _classroomId1 = Guid.NewGuid();
    private readonly Guid _classroomId2 = Guid.NewGuid();
    private readonly Classroom _classroom1;
    private readonly Classroom _classroom2;
    private readonly DateTime _nowUtc = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);

    public AnnouncementsUseCaseTests()
    {
        _classroom1 = new Classroom("Infants", null, 3, 12, 8, 4, isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_classroom1, _classroomId1);

        _classroom2 = new Classroom("Toddlers", null, 12, 36, 12, 5, isActive: true);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(_classroom2, _classroomId2);

        _dateTimeProvider.UtcNow.Returns(_nowUtc);
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.FullName.Returns("Admin User");
        _currentUser.Role.Returns("Admin");
    }

    [Fact]
    public async Task GetAnnouncements_AdminSeesAllIncludingExpired()
    {
        // Arrange
        var a1 = new Announcement("All", "Body", AnnouncementAudience.Everyone, Guid.NewGuid(), "Admin", _nowUtc.AddDays(-2));
        var a2 = new Announcement("Staff Only", "Body", AnnouncementAudience.Staff, Guid.NewGuid(), "Admin", _nowUtc.AddDays(-1));
        var a3 = new Announcement("Expired", "Body", AnnouncementAudience.Everyone, Guid.NewGuid(), "Admin", _nowUtc.AddDays(-5), expiresAt: _nowUtc.AddDays(-1));

        var aSet = MockDbSetHelper.CreateMockDbSet([a1, a2, a3]);
        _dbContext.Announcements.Returns(aSet);

        _currentUser.Role.Returns("Admin");

        var validator = new GetAnnouncementsValidator();
        var useCase = new GetAnnouncementsUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        // Act
        var result = await useCase.ExecuteAsync(new GetAnnouncementsRequest(), CancellationToken.None);

        // Assert
        result.Items.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetAnnouncements_StaffHidesExpiredAndIrrelevantClassrooms()
    {
        // Arrange
        var staffId = Guid.NewGuid();
        _currentUser.StaffId.Returns(staffId);
        _currentUser.Role.Returns("Staff");

        var sc = new StaffClassroom(staffId, _classroomId1, _nowUtc);
        var scSet = MockDbSetHelper.CreateMockDbSet([sc]);
        _dbContext.StaffClassrooms.Returns(scSet);

        var aEveryone = new Announcement("Everyone", "Body", AnnouncementAudience.Everyone, Guid.NewGuid(), "Admin", _nowUtc);
        var aStaff = new Announcement("Staff", "Body", AnnouncementAudience.Staff, Guid.NewGuid(), "Admin", _nowUtc);
        var aParents = new Announcement("Parents", "Body", AnnouncementAudience.Parents, Guid.NewGuid(), "Admin", _nowUtc);
        var aClassroom1 = new Announcement("Infants", "Body", AnnouncementAudience.Classroom, Guid.NewGuid(), "Admin", _nowUtc, classroomId: _classroomId1);
        var aClassroom2 = new Announcement("Toddlers", "Body", AnnouncementAudience.Classroom, Guid.NewGuid(), "Admin", _nowUtc, classroomId: _classroomId2);
        var aExpired = new Announcement("Expired", "Body", AnnouncementAudience.Everyone, Guid.NewGuid(), "Admin", _nowUtc.AddDays(-5), expiresAt: _nowUtc.AddHours(-1));

        var aSet = MockDbSetHelper.CreateMockDbSet([aEveryone, aStaff, aParents, aClassroom1, aClassroom2, aExpired]);
        _dbContext.Announcements.Returns(aSet);

        var validator = new GetAnnouncementsValidator();
        var useCase = new GetAnnouncementsUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        // Act
        var result = await useCase.ExecuteAsync(new GetAnnouncementsRequest(), CancellationToken.None);

        // Assert
        // Should see: Everyone, Staff, and Classroom1. Should NOT see: Parents, Classroom2, or Expired
        result.Items.Count.ShouldBe(3);
        result.Items.Select(x => x.Title).ShouldContain("Everyone");
        result.Items.Select(x => x.Title).ShouldContain("Staff");
        result.Items.Select(x => x.Title).ShouldContain("Infants");
        result.Items.Select(x => x.Title).ShouldNotContain("Parents");
        result.Items.Select(x => x.Title).ShouldNotContain("Toddlers");
        result.Items.Select(x => x.Title).ShouldNotContain("Expired");
    }

    [Fact]
    public async Task GetAnnouncements_ParentHidesExpiredAndStaffAndOtherClassrooms()
    {
        // Arrange
        var guardianId = Guid.NewGuid();
        _currentUser.GuardianId.Returns(guardianId);
        _currentUser.Role.Returns("Parent");

        var child = new Child("Abel", "Abebe", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2026, 1, 1), _classroomId2);
        var link = new ChildGuardian(child.Id, guardianId, Relationship.Father, isPrimaryContact: true, canPickUp: true, isPayer: true, isEmergencyContact: true);
        typeof(ChildGuardian).GetProperty(nameof(ChildGuardian.Child))!.SetValue(link, child);

        var cgSet = MockDbSetHelper.CreateMockDbSet([link]);
        _dbContext.ChildGuardians.Returns(cgSet);

        var aEveryone = new Announcement("Everyone", "Body", AnnouncementAudience.Everyone, Guid.NewGuid(), "Admin", _nowUtc);
        var aStaff = new Announcement("Staff", "Body", AnnouncementAudience.Staff, Guid.NewGuid(), "Admin", _nowUtc);
        var aParents = new Announcement("Parents", "Body", AnnouncementAudience.Parents, Guid.NewGuid(), "Admin", _nowUtc);
        var aClassroom1 = new Announcement("Infants", "Body", AnnouncementAudience.Classroom, Guid.NewGuid(), "Admin", _nowUtc, classroomId: _classroomId1);
        var aClassroom2 = new Announcement("Toddlers", "Body", AnnouncementAudience.Classroom, Guid.NewGuid(), "Admin", _nowUtc, classroomId: _classroomId2);
        var aExpired = new Announcement("Expired", "Body", AnnouncementAudience.Everyone, Guid.NewGuid(), "Admin", _nowUtc.AddDays(-5), expiresAt: _nowUtc.AddHours(-1));

        var aSet = MockDbSetHelper.CreateMockDbSet([aEveryone, aStaff, aParents, aClassroom1, aClassroom2, aExpired]);
        _dbContext.Announcements.Returns(aSet);

        var validator = new GetAnnouncementsValidator();
        var useCase = new GetAnnouncementsUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        // Act
        var result = await useCase.ExecuteAsync(new GetAnnouncementsRequest(), CancellationToken.None);

        // Assert
        // Should see: Everyone, Parents, and Classroom2. Should NOT see: Staff, Classroom1, or Expired
        result.Items.Count.ShouldBe(3);
        result.Items.Select(x => x.Title).ShouldContain("Everyone");
        result.Items.Select(x => x.Title).ShouldContain("Parents");
        result.Items.Select(x => x.Title).ShouldContain("Toddlers");
        result.Items.Select(x => x.Title).ShouldNotContain("Staff");
        result.Items.Select(x => x.Title).ShouldNotContain("Infants");
        result.Items.Select(x => x.Title).ShouldNotContain("Expired");
    }

    [Fact]
    public async Task CreateAnnouncement_WhenClassroomAudienceWithoutClassroomId_ShouldFailValidation()
    {
        // Arrange
        var validator = new CreateAnnouncementValidator();
        var useCase = new CreateAnnouncementUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        var request = new CreateAnnouncementRequest(
            "Classroom News", "Body text", AnnouncementAudience.Classroom, ClassroomId: null);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationFailedException>(() =>
            useCase.ExecuteAsync(request, CancellationToken.None));

        ex.Errors.Values.SelectMany(v => v).ShouldContain("Classroom is required when audience is Classroom.");
    }

    [Fact]
    public async Task CreateAnnouncement_WhenEveryoneAudienceWithClassroomId_ShouldFailValidation()
    {
        // Arrange
        var validator = new CreateAnnouncementValidator();
        var useCase = new CreateAnnouncementUseCase(_dbContext, _currentUser, _dateTimeProvider, validator);

        var request = new CreateAnnouncementRequest(
            "General News", "Body text", AnnouncementAudience.Everyone, ClassroomId: _classroomId1);

        // Act & Assert
        var ex = await Should.ThrowAsync<ValidationFailedException>(() =>
            useCase.ExecuteAsync(request, CancellationToken.None));

        ex.Errors.Values.SelectMany(v => v).ShouldContain("Classroom must not be set unless audience is Classroom.");
    }
}

