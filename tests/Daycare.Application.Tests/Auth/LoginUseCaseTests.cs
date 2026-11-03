using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Application.Features.Auth.Login;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Daycare.Application.Tests.Auth;

public class LoginUseCaseTests
{
    private readonly IIdentityService _identityService = Substitute.For<IIdentityService>();
    private readonly IJwtTokenGenerator _jwtTokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly LoginValidator _validator = new();

    private LoginUseCase CreateUseCase() => new(_identityService, _jwtTokenGenerator, _validator);

    [Fact]
    public async Task ExecuteAsync_WithValidCredentials_ShouldReturnTokenAndUser()
    {
        var useCase = CreateUseCase();
        var request = new LoginRequest("admin@daycare.local", "Admin@12345");
        var userDto = new ApplicationUserDto(
            Guid.NewGuid(), "admin@daycare.local", "Admin User", "Admin", false, null, null, true);

        _identityService.AuthenticateAsync("admin@daycare.local", "Admin@12345", Arg.Any<CancellationToken>())
            .Returns((true, null, null, userDto));

        var expiresAt = DateTime.UtcNow.AddHours(2);
        _jwtTokenGenerator.GenerateToken(userDto)
            .Returns(("jwt-token-123", expiresAt));

        var response = await useCase.ExecuteAsync(request);

        response.ShouldNotBeNull();
        response.AccessToken.ShouldBe("jwt-token-123");
        response.ExpiresAt.ShouldBe(expiresAt);
        response.User.Email.ShouldBe("admin@daycare.local");
        response.User.Role.ShouldBe("Admin");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidCredentials_ShouldThrowUnauthorizedException()
    {
        var useCase = CreateUseCase();
        var request = new LoginRequest("admin@daycare.local", "WrongPassword");

        _identityService.AuthenticateAsync("admin@daycare.local", "WrongPassword", Arg.Any<CancellationToken>())
            .Returns((false, "INVALID_CREDENTIALS", "Invalid email or password.", null));

        var ex = await Should.ThrowAsync<UnauthorizedException>(() => useCase.ExecuteAsync(request));

        ex.Code.ShouldBe("INVALID_CREDENTIALS");
        ex.Message.ShouldBe("Invalid email or password.");
    }

    [Fact]
    public async Task ExecuteAsync_WithLockedAccount_ShouldThrowAccountLocked()
    {
        var useCase = CreateUseCase();
        var request = new LoginRequest("locked@daycare.local", "Password@123");

        _identityService.AuthenticateAsync("locked@daycare.local", "Password@123", Arg.Any<CancellationToken>())
            .Returns((false, "ACCOUNT_LOCKED", "Account is locked.", null));

        var ex = await Should.ThrowAsync<UnauthorizedException>(() => useCase.ExecuteAsync(request));

        ex.Code.ShouldBe("ACCOUNT_LOCKED");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidInput_ShouldThrowValidationFailedException()
    {
        var useCase = CreateUseCase();
        var request = new LoginRequest("not-an-email", "");

        var ex = await Should.ThrowAsync<ValidationFailedException>(() => useCase.ExecuteAsync(request));

        ex.Code.ShouldBe("VALIDATION_FAILED");
        ex.Errors.ShouldContainKey("email");
        ex.Errors.ShouldContainKey("password");
    }
}
