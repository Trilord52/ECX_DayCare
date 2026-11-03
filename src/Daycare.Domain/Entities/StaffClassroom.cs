namespace Daycare.Domain.Entities;

public class StaffClassroom
{
    public Guid StaffId { get; private set; }
    public Guid ClassroomId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    // Navigations
    public Staff Staff { get; private set; } = null!;
    public Classroom Classroom { get; private set; } = null!;

    private StaffClassroom() { }

    public StaffClassroom(Guid staffId, Guid classroomId, DateTime assignedAt)
    {
        StaffId = staffId;
        ClassroomId = classroomId;
        AssignedAt = assignedAt;
    }
}
