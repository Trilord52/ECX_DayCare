using Daycare.Application.Abstractions;
using Daycare.Application.Features.Children.ChildGuardians;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using FluentValidation;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Children;

public class ChildGuardianUseCaseTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();

    [Fact]
    public async Task UpdateChildGuardian_RemovingSolePickup_ShouldThrowNoPickupGuardian()
    {
        var childId = Guid.NewGuid();
        var guardianId = Guid.NewGuid();

        var child = new Child("Abel", "Abebe", new DateOnly(2023, 1, 1), Gender.Male, new DateOnly(2024, 1, 1));
        typeof(Daycare.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(child, childId);

        var guardian = new Guardian("Abebe", "Tadesse", "+251911234567");
        typeof(Daycare.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(guardian, guardianId);

        var link = new ChildGuardian(childId, guardianId, Relationship.Father, isPrimaryContact: true, canPickUp: true, isPayer: true, isEmergencyContact: true);
        typeof(ChildGuardian).GetProperty("Guardian")!.SetValue(link, guardian);
        child.Guardians.Add(link);
        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        _dbContext.Children.Returns(childSet);

        var validator = new UpdateChildGuardianValidator();
        var useCase = new UpdateChildGuardianUseCase(_dbContext, validator);

        var request = new UpdateChildGuardianRequest(
            childId, guardianId, Relationship.Father, IsPrimaryContact: true, CanPickUp: false, IsPayer: true, IsEmergencyContact: true);

        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("NO_PICKUP_GUARDIAN");
    }

    [Fact]
    public async Task UnlinkGuardian_RemovingSolePickup_ShouldThrowNoPickupGuardian()
    {
        var childId = Guid.NewGuid();
        var guardianId = Guid.NewGuid();

        var child = new Child("Abel", "Abebe", new DateOnly(2023, 1, 1), Gender.Male, new DateOnly(2024, 1, 1));
        typeof(Daycare.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(child, childId);

        var guardian = new Guardian("Abebe", "Tadesse", "+251911234567");
        typeof(Daycare.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(guardian, guardianId);

        var link = new ChildGuardian(childId, guardianId, Relationship.Father, isPrimaryContact: true, canPickUp: true, isPayer: true, isEmergencyContact: true);
        typeof(ChildGuardian).GetProperty("Guardian")!.SetValue(link, guardian);
        child.Guardians.Add(link);

        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        _dbContext.Children.Returns(childSet);

        var useCase = new UnlinkGuardianUseCase(_dbContext);
        var request = new UnlinkGuardianRequest(childId, guardianId);

        var ex = await Should.ThrowAsync<DomainRuleException>(() => useCase.ExecuteAsync(request));
        ex.Code.ShouldBe("NO_PICKUP_GUARDIAN");
    }

    [Fact]
    public async Task LinkGuardian_AsPrimary_ShouldClearExistingPrimaryContact()
    {
        var childId = Guid.NewGuid();
        var guardian1Id = Guid.NewGuid();
        var guardian2Id = Guid.NewGuid();

        var child = new Child("Abel", "Abebe", new DateOnly(2023, 1, 1), Gender.Male, new DateOnly(2024, 1, 1));
        typeof(Daycare.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(child, childId);

        var guardian1 = new Guardian("Abebe", "Tadesse", "+251911234567");
        typeof(Daycare.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(guardian1, guardian1Id);

        var guardian2 = new Guardian("Aster", "Desta", "+251922345678");
        typeof(Daycare.Domain.Common.BaseEntity).GetProperty("Id")!.SetValue(guardian2, guardian2Id);

        var link1 = new ChildGuardian(childId, guardian1Id, Relationship.Father, isPrimaryContact: true, canPickUp: true, isPayer: true, isEmergencyContact: true);
        child.Guardians.Add(link1);

        var childSet = MockDbSetHelper.CreateMockDbSet([child]);
        var guardianSet = MockDbSetHelper.CreateMockDbSet([guardian1, guardian2]);
        _dbContext.Children.Returns(childSet);
        _dbContext.Guardians.Returns(guardianSet);

        var validator = new LinkGuardianValidator();
        var useCase = new LinkGuardianUseCase(_dbContext, validator);

        var request = new LinkGuardianRequest(childId, guardian2Id, Relationship.Grandparent, IsPrimaryContact: true, CanPickUp: true);

        var response = await useCase.ExecuteAsync(request);

        response.IsPrimaryContact.ShouldBeTrue();
        link1.IsPrimaryContact.ShouldBeFalse();
    }
}
