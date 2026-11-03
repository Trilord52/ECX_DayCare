using Daycare.Domain.Entities;
using Daycare.Domain.Exceptions;
using Shouldly;
using Xunit;

namespace Daycare.Domain.Tests;

public class ClassroomTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldSucceed()
    {
        var classroom = new Classroom("Toddlers", "12-30 months", 12, 30, 12, 6);

        classroom.Name.ShouldBe("Toddlers");
        classroom.MinAgeMonths.ShouldBe(12);
        classroom.MaxAgeMonths.ShouldBe(30);
        classroom.Capacity.ShouldBe(12);
        classroom.MaxChildrenPerStaff.ShouldBe(6);
        classroom.IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", 0, 12, 10, 4, "Classroom name is required.")]
    [InlineData("Infants", -1, 12, 10, 4, "Minimum age in months cannot be negative.")]
    [InlineData("Infants", 24, 12, 10, 4, "Maximum age cannot be less than minimum age.")]
    [InlineData("Infants", 0, 12, 0, 4, "Capacity must be greater than zero.")]
    [InlineData("Infants", 0, 12, 10, 0, "Max children per staff must be greater than zero.")]
    public void Create_WithInvalidParameters_ShouldThrowDomainRuleException(
        string name, int minAge, int maxAge, int capacity, int maxPerStaff, string expectedMessage)
    {
        var ex = Should.Throw<DomainRuleException>(() =>
            new Classroom(name, null, minAge, maxAge, capacity, maxPerStaff));

        ex.Code.ShouldBe("VALIDATION_FAILED");
        ex.Message.ShouldBe(expectedMessage);
    }

    [Theory]
    [InlineData(7, 1, 6, true)]   // 7 present, 1 staff, max 6 -> over ratio
    [InlineData(6, 1, 6, false)]  // 6 present, 1 staff, max 6 -> at limit, not over
    [InlineData(5, 1, 6, false)]  // 5 present, 1 staff, max 6 -> under limit
    [InlineData(13, 2, 6, true)]  // 13 present, 2 staff, limit 12 -> over ratio
    [InlineData(12, 2, 6, false)] // 12 present, 2 staff, limit 12 -> at limit
    [InlineData(1, 0, 6, true)]   // 1 present, 0 staff -> over ratio
    [InlineData(0, 0, 6, false)]  // 0 present, 0 staff -> not over
    public void IsOverRatio_ShouldCalculateCorrectly(int presentCount, int staffCount, int maxPerStaff, bool expectedOverRatio)
    {
        var classroom = new Classroom("Test", null, 0, 60, 20, maxPerStaff);

        classroom.IsOverRatio(presentCount, staffCount).ShouldBe(expectedOverRatio);
    }
}
