using Daycare.Application.Abstractions;
using Daycare.Application.Common.Exceptions;
using Daycare.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Infrastructure.Services;

public class AccessGuard : IAccessGuard
{
    private readonly ICurrentUser _currentUser;
    private readonly IApplicationDbContext _dbContext;

    public AccessGuard(ICurrentUser currentUser, IApplicationDbContext dbContext)
    {
        _currentUser = currentUser;
        _dbContext = dbContext;
    }

    public async Task EnsureCanViewChildAsync(Guid childId, CancellationToken ct = default)
    {
        var child = await _dbContext.Children
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == childId, ct);

        if (child == null)
        {
            throw new NotFoundException("Child", childId);
        }

        if (_currentUser.Role == "Admin")
        {
            return;
        }

        if (_currentUser.Role == "Staff")
        {
            var staffId = await GetCurrentStaffIdAsync(ct);
            if (!staffId.HasValue || !child.ClassroomId.HasValue)
            {
                throw new ForbiddenException("Staff may only view children in classrooms assigned to them.");
            }

            var isAssigned = await _dbContext.StaffClassrooms
                .AsNoTracking()
                .AnyAsync(sc => sc.StaffId == staffId.Value && sc.ClassroomId == child.ClassroomId.Value, ct);

            if (!isAssigned)
            {
                throw new ForbiddenException("Staff may only view children in classrooms assigned to them.");
            }

            return;
        }

        if (_currentUser.Role == "Parent")
        {
            var guardianId = await GetCurrentGuardianIdAsync(ct);
            if (!guardianId.HasValue)
            {
                throw new ForbiddenException("Parents may only view children linked to their guardian record.");
            }

            var isLinked = await _dbContext.ChildGuardians
                .AsNoTracking()
                .AnyAsync(cg => cg.ChildId == childId && cg.GuardianId == guardianId.Value, ct);

            if (!isLinked)
            {
                throw new ForbiddenException("Parents may only view children linked to their guardian record.");
            }

            return;
        }

        throw new ForbiddenException("You do not have permission to view this child.");
    }

    public async Task EnsureCanWriteChildDataAsync(Guid childId, CancellationToken ct = default)
    {
        var child = await _dbContext.Children
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == childId, ct);

        if (child == null)
        {
            throw new NotFoundException("Child", childId);
        }

        if (_currentUser.Role == "Admin")
        {
            return;
        }

        if (_currentUser.Role == "Staff")
        {
            var staffId = await GetCurrentStaffIdAsync(ct);
            if (!staffId.HasValue || !child.ClassroomId.HasValue)
            {
                throw new ForbiddenException("Staff may only modify records for children in assigned classrooms.");
            }

            var isAssigned = await _dbContext.StaffClassrooms
                .AsNoTracking()
                .AnyAsync(sc => sc.StaffId == staffId.Value && sc.ClassroomId == child.ClassroomId.Value, ct);

            if (!isAssigned)
            {
                throw new ForbiddenException("Staff may only modify records for children in assigned classrooms.");
            }

            return;
        }

        // Parents can never write child records per Section 5.3
        throw new ForbiddenException("Parents cannot modify child records.");
    }

    public async Task EnsureCanViewGuardianPhotoAsync(Guid guardianId, CancellationToken ct = default)
    {
        var guardian = await _dbContext.Guardians
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == guardianId, ct);

        if (guardian == null)
        {
            throw new NotFoundException("Guardian", guardianId);
        }

        if (_currentUser.Role == "Admin")
        {
            return;
        }

        if (_currentUser.Role == "Staff")
        {
            var staffId = await GetCurrentStaffIdAsync(ct);
            if (!staffId.HasValue)
            {
                throw new ForbiddenException("Staff may only view guardian photos for children in assigned classrooms.");
            }

            var assignedClassroomIds = await _dbContext.StaffClassrooms
                .AsNoTracking()
                .Where(sc => sc.StaffId == staffId.Value)
                .Select(sc => sc.ClassroomId)
                .ToListAsync(ct);

            if (assignedClassroomIds.Count == 0)
            {
                throw new ForbiddenException("Staff may only view guardian photos for children in assigned classrooms.");
            }

            var isLinkedToClassroomChild = await _dbContext.ChildGuardians
                .AsNoTracking()
                .Where(cg => cg.GuardianId == guardianId)
                .Join(_dbContext.Children.AsNoTracking(),
                    cg => cg.ChildId,
                    c => c.Id,
                    (cg, c) => c)
                .AnyAsync(c => c.ClassroomId.HasValue && assignedClassroomIds.Contains(c.ClassroomId.Value), ct);

            if (!isLinkedToClassroomChild)
            {
                throw new ForbiddenException("Staff may only view guardian photos for children in assigned classrooms.");
            }

            return;
        }

        if (_currentUser.Role == "Parent")
        {
            var currentGuardianId = await GetCurrentGuardianIdAsync(ct);
            if (!currentGuardianId.HasValue)
            {
                throw new ForbiddenException("Parents may only view their own photo or photos of guardians linked to their children.");
            }

            if (currentGuardianId.Value == guardianId)
            {
                return;
            }

            var ownChildIds = await _dbContext.ChildGuardians
                .AsNoTracking()
                .Where(cg => cg.GuardianId == currentGuardianId.Value)
                .Select(cg => cg.ChildId)
                .ToListAsync(ct);

            var sharedChild = await _dbContext.ChildGuardians
                .AsNoTracking()
                .AnyAsync(cg => cg.GuardianId == guardianId && ownChildIds.Contains(cg.ChildId), ct);

            if (!sharedChild)
            {
                throw new ForbiddenException("Parents may only view their own photo or photos of guardians linked to their children.");
            }

            return;
        }

        throw new ForbiddenException("You do not have permission to view this guardian's photo.");
    }

    public async Task EnsureCanViewStaffPhotoAsync(Guid staffId, CancellationToken ct = default)
    {
        var staff = await _dbContext.Staff
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == staffId, ct);

        if (staff == null)
        {
            throw new NotFoundException("Staff", staffId);
        }

        if (_currentUser.Role == "Admin" || _currentUser.Role == "Staff")
        {
            return;
        }

        throw new ForbiddenException("Parents are not permitted to view staff photos.");
    }

    public void EnsureAdmin()
    {
        if (_currentUser.Role != "Admin")
        {
            throw new ForbiddenException("Only administrators may perform this action.");
        }
    }

    public IQueryable<Child> VisibleChildren()
    {
        var query = _dbContext.Children.AsNoTracking();

        if (_currentUser.Role == "Admin")
        {
            return query;
        }

        if (_currentUser.Role == "Staff")
        {
            var staffId = _currentUser.StaffId;
            if (!staffId.HasValue && _currentUser.UserId.HasValue)
            {
                staffId = _dbContext.Staff
                    .Where(s => s.UserId == _currentUser.UserId.Value)
                    .Select(s => (Guid?)s.Id)
                    .FirstOrDefault();
            }

            if (!staffId.HasValue)
            {
                return query.Where(_ => false);
            }

            var assignedClassroomIds = _dbContext.StaffClassrooms
                .Where(sc => sc.StaffId == staffId.Value)
                .Select(sc => sc.ClassroomId);

            return query.Where(c => c.ClassroomId.HasValue && assignedClassroomIds.Contains(c.ClassroomId.Value));
        }

        if (_currentUser.Role == "Parent")
        {
            var guardianId = _currentUser.GuardianId;
            if (!guardianId.HasValue && _currentUser.UserId.HasValue)
            {
                guardianId = _dbContext.Guardians
                    .Where(g => g.UserId == _currentUser.UserId.Value)
                    .Select(g => (Guid?)g.Id)
                    .FirstOrDefault();
            }

            if (!guardianId.HasValue)
            {
                return query.Where(_ => false);
            }

            var linkedChildIds = _dbContext.ChildGuardians
                .Where(cg => cg.GuardianId == guardianId.Value)
                .Select(cg => cg.ChildId);

            return query.Where(c => linkedChildIds.Contains(c.Id));
        }

        return query.Where(_ => false);
    }

    private async Task<Guid?> GetCurrentStaffIdAsync(CancellationToken ct)
    {
        if (_currentUser.StaffId.HasValue)
            return _currentUser.StaffId.Value;

        if (!_currentUser.UserId.HasValue)
            return null;

        return await _dbContext.Staff
            .AsNoTracking()
            .Where(s => s.UserId == _currentUser.UserId.Value)
            .Select(s => (Guid?)s.Id)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<Guid?> GetCurrentGuardianIdAsync(CancellationToken ct)
    {
        if (_currentUser.GuardianId.HasValue)
            return _currentUser.GuardianId.Value;

        if (!_currentUser.UserId.HasValue)
            return null;

        return await _dbContext.Guardians
            .AsNoTracking()
            .Where(g => g.UserId == _currentUser.UserId.Value)
            .Select(g => (Guid?)g.Id)
            .FirstOrDefaultAsync(ct);
    }
}
