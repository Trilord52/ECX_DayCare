using Daycare.Domain.Entities;

namespace Daycare.Application.Abstractions;

public interface IAccessGuard
{
    Task EnsureCanViewChildAsync(Guid childId, CancellationToken ct = default);
    Task EnsureCanWriteChildDataAsync(Guid childId, CancellationToken ct = default);
    Task EnsureCanViewGuardianPhotoAsync(Guid guardianId, CancellationToken ct = default);
    Task EnsureCanViewStaffPhotoAsync(Guid staffId, CancellationToken ct = default);
    void EnsureAdmin();
    IQueryable<Child> VisibleChildren();
}
