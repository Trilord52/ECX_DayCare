using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using Daycare.Domain.Enums;
using Daycare.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Children.AssignChildClassroom;

public record AssignChildClassroomRequest(Guid ChildId, Guid? ClassroomId);

public record AssignChildClassroomResponse(bool Success = true);

public class AssignChildClassroomUseCase : UseCase<AssignChildClassroomRequest, AssignChildClassroomResponse>
{
    private readonly IApplicationDbContext _dbContext;

    public AssignChildClassroomUseCase(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task<AssignChildClassroomResponse> HandleAsync(
        AssignChildClassroomRequest request,
        CancellationToken ct)
    {
        var child = await _dbContext.Children
            .FirstOrDefaultAsync(c => c.Id == request.ChildId, ct);

        if (child == null)
        {
            throw new NotFoundException("Child", request.ChildId);
        }

        if (request.ClassroomId.HasValue)
        {
            var classroom = await _dbContext.Classrooms
                .FirstOrDefaultAsync(c => c.Id == request.ClassroomId.Value, ct);

            if (classroom == null)
            {
                throw new NotFoundException("Classroom", request.ClassroomId.Value);
            }

            if (!classroom.IsActive)
            {
                throw new DomainRuleException("CLASSROOM_INACTIVE", "Cannot assign child to an inactive classroom.");
            }

            // BR-05: Check capacity
            var activeCount = await _dbContext.Children
                .CountAsync(c => c.ClassroomId == classroom.Id && c.Status == ChildStatus.Active && c.Id != child.Id, ct);

            if (activeCount >= classroom.Capacity)
            {
                throw new DomainRuleException("CLASSROOM_FULL", "Classroom is already at maximum capacity.");
            }
        }

        child.AssignClassroom(request.ClassroomId);
        await _dbContext.SaveChangesAsync(ct);

        return new AssignChildClassroomResponse(true);
    }
}
