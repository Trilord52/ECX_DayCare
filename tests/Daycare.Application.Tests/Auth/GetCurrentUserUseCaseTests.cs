using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Auth.GetCurrentUser;
using Daycare.Application.Tests.Helpers;
using Daycare.Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Auth;

public class GetCurrentUserUseCaseTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();

    public GetCurrentUserUseCaseTests()
    {
        var staffDbSet = MockDbSetHelper.CreateMockDbSet<Staff>([]);
        _dbContext.Staff.Returns(staffDbSet);

        var guardiansDbSet = MockDbSetHelper.CreateMockDbSet<Guardian>([]);
        _dbContext.Guardians.Returns(guardiansDbSet);
    }

    private GetCurrentUserUseCase CreateUseCase() => new(_currentUser, _identityService, _dbContext);

    [Fact]
    public async Task ExecuteAsync_WhenAuthenticated_ShouldReturnCurrentUser()
    {
        var useCase = CreateUseCase();
        var userId = Guid.NewGuid();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);

        var userDto = new ApplicationUserDto(
            userId, "staff@daycare.local", "Staff User", "Staff", false, null, Guid.NewGuid(), true);

        _identityService.GetUserByIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(userDto);

        var response = await useCase.ExecuteAsync(new GetCurrentUserRequest());

        response.ShouldNotBeNull();
        response.Id.ShouldBe(userId);
        response.Email.ShouldBe("staff@daycare.local");
        response.Role.ShouldBe("Staff");
        response.StaffId.ShouldBe(userDto.StaffId);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        var useCase = CreateUseCase();
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);

        var ex = await Should.ThrowAsync<UnauthorizedException>(() =>
            useCase.ExecuteAsync(new GetCurrentUserRequest()));

        ex.Code.ShouldBe("UNAUTHORIZED");
    }
}
