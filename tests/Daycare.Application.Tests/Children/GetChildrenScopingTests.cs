using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Children.GetChildren;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Entities;
using Daycare.Domain.Enums;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Children;

public class GetChildrenScopingTests
{
    private readonly IAccessGuard _accessGuard = Substitute.For<IAccessGuard>();
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public GetChildrenScopingTests()
    {
        _dateTimeProvider.LocalToday.Returns(new DateOnly(2026, 4, 1));
    }

    [Fact]
    public async Task GetChildren_ShouldQueryVisibleChildrenFromAccessGuard()
    {
        var child1 = new Child("Abel", "Abebe", new DateOnly(2023, 1, 1), Gender.Male, new DateOnly(2024, 1, 1));
        var child2 = new Child("Sara", "Abebe", new DateOnly(2024, 1, 1), Gender.Female, new DateOnly(2024, 6, 1));

        var visibleSet = MockDbSetHelper.CreateMockDbSet([child1, child2]);
        _accessGuard.VisibleChildren().Returns(visibleSet);

        var useCase = new GetChildrenUseCase(_accessGuard, _dateTimeProvider);
        var result = await useCase.ExecuteAsync(new GetChildrenRequest());

        result.Items.Count.ShouldBe(2);
        _accessGuard.Received(1).VisibleChildren();
    }

    [Fact]
    public async Task GetChildById_WhenAccessGuardRejects_ShouldPropagateForbiddenException()
    {
        var childId = Guid.NewGuid();
        _accessGuard.EnsureCanViewChildAsync(childId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new ForbiddenException("Parents may only view children linked to their guardian record.")));

        var useCase = new GetChildByIdUseCase(_accessGuard, _dbContext, _dateTimeProvider);

        var ex = await Should.ThrowAsync<ForbiddenException>(() =>
            useCase.ExecuteAsync(new GetChildByIdRequest(childId)));

        ex.Message.ShouldContain("linked to their guardian record");
    }
}
