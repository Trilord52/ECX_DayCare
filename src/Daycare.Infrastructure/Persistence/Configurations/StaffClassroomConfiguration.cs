using Daycare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Daycare.Infrastructure.Persistence.Configurations;

public class StaffClassroomConfiguration : IEntityTypeConfiguration<StaffClassroom>
{
    public void Configure(EntityTypeBuilder<StaffClassroom> builder)
    {
        builder.ToTable("staff_classrooms");

        builder.HasKey(sc => new { sc.StaffId, sc.ClassroomId });

        builder.Property(sc => sc.AssignedAt)
            .IsRequired();

        builder.HasOne(sc => sc.Staff)
            .WithMany(s => s.StaffClassrooms)
            .HasForeignKey(sc => sc.StaffId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sc => sc.Classroom)
            .WithMany(c => c.StaffClassrooms)
            .HasForeignKey(sc => sc.ClassroomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
