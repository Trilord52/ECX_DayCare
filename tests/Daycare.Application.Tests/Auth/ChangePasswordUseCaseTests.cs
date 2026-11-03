using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Auth.ChangePassword;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Auth;

public class ChangePasswordUseCaseTests
{
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly ChangePasswordValidator _validator = new();

    private ChangePasswordUseCase CreateUseCase() => new(_currentUser, _identityService, _validator);

    [Fact]
    public async Task ExecuteAsync_WithValidPasswords_ShouldSucceed()
    {
        var useCase = CreateUseCase();
        var userId = Guid.NewGuid();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);

        _identityService.ChangePasswordAsync(userId, "OldPassword@123", "NewPassword@123", Arg.Any<CancellationToken>())
            .Returns((true, null, null));

        var response = await useCase.ExecuteAsync(new ChangePasswordRequest("OldPassword@123", "NewPassword@123"));

        response.ShouldNotBeNull();
        response.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithIncorrectCurrentPassword_ShouldThrowValidationFailedException()
    {
        var useCase = CreateUseCase();
        var userId = Guid.NewGuid();

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(userId);

        _identityService.ChangePasswordAsync(userId, "WrongPassword@123", "NewPassword@123", Arg.Any<CancellationToken>())
            .Returns((false, "INVALID_PASSWORD", "Current password is incorrect."));

        var ex = await Should.ThrowAsync<ValidationFailedException>(() =>
            useCase.ExecuteAsync(new ChangePasswordRequest("WrongPassword@123", "NewPassword@123")));

        ex.Code.ShouldBe("VALIDATION_FAILED");
        ex.Errors.ShouldContainKey("currentPassword");
    }

    [Fact]
    public async Task ExecuteAsync_WithWeakNewPassword_ShouldFailValidation()
    {
        var useCase = CreateUseCase();
        var request = new ChangePasswordRequest("Current@123", "weak");

        var ex = await Should.ThrowAsync<ValidationFailedException>(() => useCase.ExecuteAsync(request));

        ex.Code.ShouldBe("VALIDATION_FAILED");
        ex.Errors.ShouldContainKey("newPassword");
    }
}
