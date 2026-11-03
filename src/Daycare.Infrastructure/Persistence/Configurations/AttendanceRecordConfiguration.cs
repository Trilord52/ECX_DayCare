using Daycare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Daycare.Infrastructure.Persistence.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("attendance_records", t =>
        {
            t.HasCheckConstraint(
                "ck_attendance_records_status_checkin",
                "(status = 'Present' AND check_in_at IS NOT NULL) OR (status = 'Absent' AND check_in_at IS NULL AND check_out_at IS NULL)");
            t.HasCheckConstraint(
                "ck_attendance_records_checkout_after_checkin",
                "check_out_at IS NULL OR check_out_at >= check_in_at");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Date)
            .IsRequired();

        builder.Property(a => a.Status)
            .HasMaxLength(10)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(a => a.CheckedInByName)
            .HasMaxLength(200);

        builder.Property(a => a.CheckedOutByName)
            .HasMaxLength(200);

        builder.Property(a => a.LateMinutes)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.AbsenceReason)
            .HasMaxLength(300);

        builder.Property(a => a.Notes)
            .HasMaxLength(500);

        builder.HasIndex(a => new { a.ChildId, a.Date })
            .IsUnique();

        builder.HasIndex(a => new { a.Date, a.ClassroomId });

        builder.HasOne(a => a.Child)
            .WithMany(c => c.AttendanceRecords)
            .HasForeignKey(a => a.ChildId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Classroom)
            .WithMany()
            .HasForeignKey(a => a.ClassroomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CheckedInByGuardian)
            .WithMany()
            .HasForeignKey(a => a.CheckedInByGuardianId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CheckedOutByGuardian)
            .WithMany()
            .HasForeignKey(a => a.CheckedOutByGuardianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
