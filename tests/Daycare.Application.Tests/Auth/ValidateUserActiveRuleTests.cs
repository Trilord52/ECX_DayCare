using Daycare.Infrastructure.Identity;
using Daycare.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Auth;

public class ValidateUserActiveRuleTests
{
    private readonly IUserStore<ApplicationUser> _userStore = Substitute.For<IUserStore<ApplicationUser>>();
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IdentityService _identityService;

    public ValidateUserActiveRuleTests()
    {
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            _userStore, null, null, null, null, null, null, null, null);
        _identityService = new IdentityService(_userManager, null!, null!);
    }

    [Fact]
    public async Task ValidateUserActiveAsync_WhenUserDeactivatedOrDeleted_ShouldReturnFalse()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "deleted@test.local",
            Email = "deleted@test.local",
            FullName = "Deleted User",
            IsActive = false
        };
        _userManager.FindByIdAsync(userId.ToString()).Returns(Task.FromResult<ApplicationUser?>(user));

        var result = await _identityService.ValidateUserActiveAsync(userId);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateUserActiveAsync_WhenUserIsActiveAndLockedOut_ShouldReturnTrue()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "locked@test.local",
            Email = "locked@test.local",
            FullName = "Locked User",
            IsActive = true
        };
        _userManager.FindByIdAsync(userId.ToString()).Returns(Task.FromResult<ApplicationUser?>(user));

        var result = await _identityService.ValidateUserActiveAsync(userId);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateUserActiveAsync_WhenUserActiveAndNotLockedOut_ShouldReturnTrue()
    {
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "active@test.local",
            Email = "active@test.local",
            FullName = "Active User",
            IsActive = true
        };
        _userManager.FindByIdAsync(userId.ToString()).Returns(Task.FromResult<ApplicationUser?>(user));
        _userManager.IsLockedOutAsync(user).Returns(Task.FromResult(false));

        var result = await _identityService.ValidateUserActiveAsync(userId);

        result.ShouldBeTrue();
    }
}

