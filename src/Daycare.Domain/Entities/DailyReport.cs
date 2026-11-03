using Daycare.Domain.Common;
using Daycare.Domain.Enums;

namespace Daycare.Domain.Entities;

public class DailyReport : BaseEntity
{
    public Guid ChildId { get; private set; }
    public Guid ClassroomId { get; private set; }
    public DateOnly Date { get; private set; }
    public Mood? Mood { get; private set; }
    public string? Notes { get; private set; }

    // Navigations
    public Child Child { get; private set; } = null!;
    public Classroom Classroom { get; private set; } = null!;
    public ICollection<DailyReportEntry> Entries { get; private set; } = new List<DailyReportEntry>();

    private DailyReport() { }

    public DailyReport(
        Guid childId,
        Guid classroomId,
        DateOnly date,
        Mood? mood = null,
        string? notes = null)
    {
        ChildId = childId;
        ClassroomId = classroomId;
        Date = date;
        Mood = mood;
        Notes = notes?.Trim();
    }

    public void Update(Mood? mood, string? notes)
    {
        Mood = mood;
        Notes = notes?.Trim();
    }
}
