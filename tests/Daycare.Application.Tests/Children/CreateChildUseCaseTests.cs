using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Children.CreateChild;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Common;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Children;

public class CreateChildUseCaseTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly CreateChildValidator _validator;

    public CreateChildUseCaseTests()
    {
        _dateTimeProvider.LocalToday.Returns(new DateOnly(2026, 4, 1));
        _dateTimeProvider.UtcNow.Returns(new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc));
        _validator = new CreateChildValidator(_dateTimeProvider);
    }

    [Fact]
    public async Task CreateChild_WhenClassroomIsInactive_ShouldThrowClassroomInactive()
    {
        var classroomId = Guid.NewGuid();
        var classroom = new Classroom("Toddlers", null, 12, 30, 10, 5, isActive: false);
        typeof(Daycare.Domain.Common.BaseEntity)
            .GetProperty(nameof(BaseEntity.Id))!
            .SetValue(classroom, classroomId);

        var classroomSet = MockDbSetHelper.CreateMockDbSet([classroom]);
        var childrenSet = MockDbSetHelper.CreateMockDbSet<Child>([]);
        _dbContext.Classrooms.Returns(classroomSet);
        _dbContext.Children.Returns(childrenSet);

        var useCase = new CreateChildUseCase(_dbContext, _dateTimeProvider, _validator);
        var request = new CreateChildRequest(
            "Sara", "Haile", new DateOnly(2024, 1, 1), Gender.Female, new DateOnly(2026, 1, 1),
            ClassroomId: classroomId);

        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("CLASSROOM_INACTIVE");
    }

    [Fact]
    public async Task CreateChild_WhenClassroomIsAtCapacity_ShouldThrowClassroomFull()
    {
        var classroomId = Guid.NewGuid();
        var classroom = new Classroom("Infants", null, 3, 12, 2, 4, isActive: true);
        typeof(Daycare.Domain.Common.BaseEntity)
            .GetProperty(nameof(BaseEntity.Id))!
            .SetValue(classroom, classroomId);

        var existingChild1 = new Child("Baby1", "One", new DateOnly(2025, 10, 1), Gender.Male, new DateOnly(2026, 1, 1), classroomId);
        var existingChild2 = new Child("Baby2", "Two", new DateOnly(2025, 11, 1), Gender.Female, new DateOnly(2026, 1, 1), classroomId);

        var classroomSet = MockDbSetHelper.CreateMockDbSet([classroom]);
        var childrenSet = MockDbSetHelper.CreateMockDbSet([existingChild1, existingChild2]);
        _dbContext.Classrooms.Returns(classroomSet);
        _dbContext.Children.Returns(childrenSet);

        var useCase = new CreateChildUseCase(_dbContext, _dateTimeProvider, _validator);
        var request = new CreateChildRequest(
            "Baby3", "Three", new DateOnly(2025, 12, 1), Gender.Male, new DateOnly(2026, 2, 1),
            ClassroomId: classroomId);

        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("CLASSROOM_FULL");
    }

    [Fact]
    public async Task CreateChild_WithFutureDateOfBirth_ShouldThrowValidationFailed()
    {
        var useCase = new CreateChildUseCase(_dbContext, _dateTimeProvider, _validator);
        var request = new CreateChildRequest(
            "Future", "Baby", new DateOnly(2026, 5, 1), Gender.Male, new DateOnly(2026, 4, 1));

        var ex = await Should.ThrowAsync<ValidationFailedException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("VALIDATION_FAILED");
        ex.Errors.ShouldContainKey("dateOfBirth");
    }
}
