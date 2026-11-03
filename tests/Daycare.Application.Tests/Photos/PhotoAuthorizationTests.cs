using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.ValueObjects;
using Daycare.Infrastructure.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Photos;

public class PhotoAuthorizationTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();

    private readonly Guid _adminUserId = Guid.NewGuid();
    private readonly Guid _staffUserId = Guid.NewGuid();
    private readonly Guid _parentUserId = Guid.NewGuid();

    private readonly Guid _staffId = Guid.NewGuid();
    private readonly Guid _guardian1Id = Guid.NewGuid();
    private readonly Guid _guardian2Id = Guid.NewGuid();
    private readonly Guid _unrelatedGuardianId = Guid.NewGuid();
    private readonly Guid _child1Id = Guid.NewGuid();
    private readonly Guid _child2Id = Guid.NewGuid();
    private readonly Guid _classroom1Id = Guid.NewGuid();
    private readonly Guid _classroom2Id = Guid.NewGuid();

    private AccessGuard CreateGuard() => new(_currentUser, _dbContext);

    public PhotoAuthorizationTests()
    {
        // Classroom 1
        var classroom1 = new Classroom("Infants", null, 0, 12, 10, 4);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(classroom1, _classroom1Id);

        // Classroom 2
        var classroom2 = new Classroom("Toddlers", null, 12, 36, 15, 6);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(classroom2, _classroom2Id);

        // Staff assigned to Classroom 1
        var staff = new Staff("Alice", "Staff", "Teacher", "alice@daycare.local", "555-1111", new DateOnly(2023, 1, 1), _staffUserId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(staff, _staffId);
        var staffClassroom = new StaffClassroom(_staffId, _classroom1Id, DateTime.UtcNow);

        // Child 1 in Classroom 1
        var child1 = new Child("Baby", "One", new DateOnly(2025, 1, 1), Gender.Female, new DateOnly(2025, 6, 1));
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(child1, _child1Id);
        child1.AssignClassroom(_classroom1Id);

        // Child 2 in Classroom 2
        var child2 = new Child("Toddler", "Two", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2025, 6, 1));
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(child2, _child2Id);
        child2.AssignClassroom(_classroom2Id);

        // Guardian 1 linked to Child 1 (Parent User)
        var guardian1 = new Guardian("Mom", "One", "555-2222", "mom@daycare.local", "Address 1", _parentUserId);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(guardian1, _guardian1Id);

        // Guardian 2 also linked to Child 1 (Dad One)
        var guardian2 = new Guardian("Dad", "One", "555-3333", "dad@daycare.local", "Address 1");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(guardian2, _guardian2Id);

        // Unrelated Guardian linked to Child 2
        var unrelatedGuardian = new Guardian("Unrelated", "Guardian", "555-4444", "unrelated@daycare.local", "Address 2");
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(unrelatedGuardian, _unrelatedGuardianId);

        // ChildGuardians links
        var cg1 = new ChildGuardian(_child1Id, _guardian1Id, Relationship.Mother, true, true, true, true);
        var cg2 = new ChildGuardian(_child1Id, _guardian2Id, Relationship.Father, true, true, true, true);
        var cg3 = new ChildGuardian(_child2Id, _unrelatedGuardianId, Relationship.Mother, true, true, true, true);

        // Setup mock DbSets
        var staffDbSet = MockDbSetHelper.CreateMockDbSet([staff]);
        var staffClassroomsDbSet = MockDbSetHelper.CreateMockDbSet([staffClassroom]);
        var childrenDbSet = MockDbSetHelper.CreateMockDbSet([child1, child2]);
        var guardiansDbSet = MockDbSetHelper.CreateMockDbSet([guardian1, guardian2, unrelatedGuardian]);
        var childGuardiansDbSet = MockDbSetHelper.CreateMockDbSet([cg1, cg2, cg3]);

        _dbContext.Staff.Returns(staffDbSet);
        _dbContext.StaffClassrooms.Returns(staffClassroomsDbSet);
        _dbContext.Children.Returns(childrenDbSet);
        _dbContext.Guardians.Returns(guardiansDbSet);
        _dbContext.ChildGuardians.Returns(childGuardiansDbSet);
    }

    [Fact]
    public void Admin_EnsureAdmin_ShouldSucceed()
    {
        _currentUser.Role.Returns("Admin");
        _currentUser.UserId.Returns(_adminUserId);

        var guard = CreateGuard();
        Should.NotThrow(() => guard.EnsureAdmin());
    }

    [Theory]
    [InlineData("Staff")]
    [InlineData("Parent")]
    public void NonAdmin_EnsureAdmin_ShouldThrowForbidden(string role)
    {
        _currentUser.Role.Returns(role);
        _currentUser.UserId.Returns(Guid.NewGuid());

        var guard = CreateGuard();
        Should.Throw<ForbiddenException>(() => guard.EnsureAdmin());
    }

    [Fact]
    public async Task Staff_CanViewChildInAssignedClassroom_ShouldSucceed()
    {
        _currentUser.Role.Returns("Staff");
        _currentUser.UserId.Returns(_staffUserId);

        var guard = CreateGuard();
        await Should.NotThrowAsync(() => guard.EnsureCanViewChildAsync(_child1Id));
    }

    [Fact]
    public async Task Staff_CannotViewChildInUnassignedClassroom_ShouldThrowForbidden()
    {
        _currentUser.Role.Returns("Staff");
        _currentUser.UserId.Returns(_staffUserId);

        var guard = CreateGuard();
        await Should.ThrowAsync<ForbiddenException>(() => guard.EnsureCanViewChildAsync(_child2Id));
    }

    [Fact]
    public async Task Staff_CanViewGuardianLinkedToAssignedClassroomChild_ShouldSucceed()
    {
        _currentUser.Role.Returns("Staff");
        _currentUser.UserId.Returns(_staffUserId);

        var guard = CreateGuard();
        await Should.NotThrowAsync(() => guard.EnsureCanViewGuardianPhotoAsync(_guardian1Id));
        await Should.NotThrowAsync(() => guard.EnsureCanViewGuardianPhotoAsync(_guardian2Id));
    }

    [Fact]
    public async Task Staff_CannotViewGuardianLinkedOnlyToUnassignedClassroomChild_ShouldThrowForbidden()
    {
        _currentUser.Role.Returns("Staff");
        _currentUser.UserId.Returns(_staffUserId);

        var guard = CreateGuard();
        await Should.ThrowAsync<ForbiddenException>(() => guard.EnsureCanViewGuardianPhotoAsync(_unrelatedGuardianId));
    }

    [Fact]
    public async Task Staff_CanViewStaffPhoto_ShouldSucceed()
    {
        _currentUser.Role.Returns("Staff");
        _currentUser.UserId.Returns(_staffUserId);

        var guard = CreateGuard();
        await Should.NotThrowAsync(() => guard.EnsureCanViewStaffPhotoAsync(_staffId));
    }

    [Fact]
    public async Task Parent_CanViewOwnChild_ShouldSucceed()
    {
        _currentUser.Role.Returns("Parent");
        _currentUser.UserId.Returns(_parentUserId);

        var guard = CreateGuard();
        await Should.NotThrowAsync(() => guard.EnsureCanViewChildAsync(_child1Id));
    }

    [Fact]
    public async Task Parent_CannotViewOtherChild_ShouldThrowForbidden()
    {
        _currentUser.Role.Returns("Parent");
        _currentUser.UserId.Returns(_parentUserId);

        var guard = CreateGuard();
        await Should.ThrowAsync<ForbiddenException>(() => guard.EnsureCanViewChildAsync(_child2Id));
    }

    [Fact]
    public async Task Parent_CanViewOwnGuardianPhoto_ShouldSucceed()
    {
        _currentUser.Role.Returns("Parent");
        _currentUser.UserId.Returns(_parentUserId);

        var guard = CreateGuard();
        await Should.NotThrowAsync(() => guard.EnsureCanViewGuardianPhotoAsync(_guardian1Id));
    }

    [Fact]
    public async Task Parent_CanViewCoGuardianPhotoLinkedToSameChild_ShouldSucceed()
    {
        _currentUser.Role.Returns("Parent");
        _currentUser.UserId.Returns(_parentUserId);

        var guard = CreateGuard();
        await Should.NotThrowAsync(() => guard.EnsureCanViewGuardianPhotoAsync(_guardian2Id));
    }

    [Fact]
    public async Task Parent_CannotViewUnrelatedGuardianPhoto_ShouldThrowForbidden()
    {
        _currentUser.Role.Returns("Parent");
        _currentUser.UserId.Returns(_parentUserId);

        var guard = CreateGuard();
        await Should.ThrowAsync<ForbiddenException>(() => guard.EnsureCanViewGuardianPhotoAsync(_unrelatedGuardianId));
    }

    [Fact]
    public async Task Parent_CannotViewStaffPhoto_ShouldThrowForbidden()
    {
        _currentUser.Role.Returns("Parent");
        _currentUser.UserId.Returns(_parentUserId);

        var guard = CreateGuard();
        await Should.ThrowAsync<ForbiddenException>(() => guard.EnsureCanViewStaffPhotoAsync(_staffId));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetChildPhoto_WhenParentCannotViewChild_ShouldThrowForbidden_RegardlessOfPhotoState(bool hasPhoto)
    {
        _currentUser.Role.Returns("Parent");
        _currentUser.UserId.Returns(_parentUserId);

        var childId = _child2Id; // Child in another classroom, not linked to parent
        var accessGuard = Substitute.For<IAccessGuard>();
        accessGuard.EnsureCanViewChildAsync(childId, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ForbiddenException("Parents may only view children linked to their guardian record."));

        var child = new Child("Other", "Child", new DateOnly(2024, 1, 1), Gender.Male, new DateOnly(2025, 1, 1));
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(child, childId);
        if (hasPhoto)
        {
            child.SetPhoto(new ProfilePhoto("children/c.jpg", "image/jpeg", 5000, DateTime.UtcNow));
        }

        var childrenDbSet = MockDbSetHelper.CreateMockDbSet([child]);
        _dbContext.Children.Returns(childrenDbSet);

        var photoStorage = Substitute.For<IPhotoStorage>();
        var photoManager = new ProfilePhotoManager(
            photoStorage,
            Substitute.For<IImageProcessor>(),
            Microsoft.Extensions.Options.Options.Create(new Daycare.Application.Common.PhotoOptions()),
            Substitute.For<IDateTimeProvider>(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ProfilePhotoManager>.Instance);

        var useCase = new Daycare.Application.Features.Children.Photos.GetChildPhotoUseCase(
            _dbContext, accessGuard, photoManager);

        var ex = await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new Daycare.Application.Features.Children.Photos.GetChildPhotoRequest(childId)));

        ex.Message.ShouldContain("Parents may only view children linked to their guardian record.");
        await photoStorage.DidNotReceiveWithAnyArgs().OpenReadAsync(default!);
    }
}
