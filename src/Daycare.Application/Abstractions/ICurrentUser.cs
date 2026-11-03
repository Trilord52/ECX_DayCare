namespace Daycare.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    string? FullName { get; }
    string? Role { get; }
    Guid? GuardianId { get; }
    Guid? StaffId { get; }
    bool IsAuthenticated { get; }
}
