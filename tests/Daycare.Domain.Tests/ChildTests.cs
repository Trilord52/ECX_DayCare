using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using Shouldly;
using Xunit;

namespace Daycare.Domain.Tests;

public class ChildTests
{
    [Fact]
    public void CalculateAgeInMonths_ShouldComputeAccurately()
    {
        var birthDate = new DateOnly(2024, 4, 15);
        var child = new Child(
            "Abel", "Tesfaye", birthDate, Gender.Male, new DateOnly(2025, 1, 10));

        // Exactly 2 years later: 24 months
        child.CalculateAgeInMonths(new DateOnly(2026, 4, 15)).ShouldBe(24);

        // One day before 2 years: 23 months
        child.CalculateAgeInMonths(new DateOnly(2026, 4, 14)).ShouldBe(23);

        // 2 years and 6 months: 30 months
        child.CalculateAgeInMonths(new DateOnly(2026, 10, 15)).ShouldBe(30);
    }

    [Fact]
    public void SoftDelete_ShouldSetIsDeletedAndDeletedAt()
    {
        var child = new Child(
            "Abel", "Tesfaye", new DateOnly(2024, 4, 15), Gender.Male, new DateOnly(2025, 1, 10));
        var now = DateTime.UtcNow;

        child.SoftDelete(now);

        child.IsDeleted.ShouldBeTrue();
        child.DeletedAt.ShouldBe(now);
    }

    [Fact]
    public void WithdrawAndReactivate_ShouldUpdateStatus()
    {
        var child = new Child(
            "Abel", "Tesfaye", new DateOnly(2024, 4, 15), Gender.Male, new DateOnly(2025, 1, 10));

        child.Withdraw();
        child.Status.ShouldBe(ChildStatus.Withdrawn);

        child.Reactivate();
        child.Status.ShouldBe(ChildStatus.Active);
    }

    [Fact]
    public void Create_WithFutureDateOfBirth_ShouldThrowValidationFailed()
    {
        var futureBirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var ex = Should.Throw<DomainRuleException>(() =>
            new Child("Future", "Baby", futureBirthDate, Gender.Female, DateOnly.FromDateTime(DateTime.UtcNow)));

        ex.Code.ShouldBe("VALIDATION_FAILED");
        ex.Message.ShouldContain("Date of birth cannot be in the future");
    }
}
