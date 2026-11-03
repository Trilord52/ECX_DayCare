using Daycare.Application.Abstractions;
using Daycare.Application.Common;
using Daycare.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Daycare.Application.Features.Announcements.DeleteAnnouncement;

public record DeleteAnnouncementRequest(Guid AnnouncementId);

public class DeleteAnnouncementValidator : AbstractValidator<DeleteAnnouncementRequest>
{
    public DeleteAnnouncementValidator()
    {
        RuleFor(x => x.AnnouncementId).NotEmpty().WithMessage("Announcement ID is required.");
    }
}

public class DeleteAnnouncementUseCase : UseCase<DeleteAnnouncementRequest, bool>
{
    private readonly IApplicationDbContext _dbContext;

    public DeleteAnnouncementUseCase(
        IApplicationDbContext dbContext,
        IValidator<DeleteAnnouncementRequest> validator)
        : base(validator)
    {
        _dbContext = dbContext;
    }

    protected override async Task<bool> HandleAsync(
        DeleteAnnouncementRequest request,
        CancellationToken ct)
    {
        var announcement = await _dbContext.Announcements
            .FirstOrDefaultAsync(a => a.Id == request.AnnouncementId, ct);

        if (announcement == null)
        {
            throw new NotFoundException("Announcement", request.AnnouncementId);
        }

        _dbContext.Announcements.Remove(announcement);
        await _dbContext.SaveChangesAsync(ct);

        return true;
    }
}

