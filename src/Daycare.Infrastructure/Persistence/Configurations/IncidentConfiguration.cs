using Daycare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Daycare.Infrastructure.Persistence.Configurations;

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.ToTable("incidents");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.OccurredAt)
            .IsRequired();

        builder.Property(i => i.Type)
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(i => i.Severity)
            .HasMaxLength(10)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(i => i.Description)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(i => i.ActionTaken)
            .HasMaxLength(2000);

        builder.Property(i => i.ReportedByName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.ParentNotifiedByName)
            .HasMaxLength(200);

        builder.HasIndex(i => new { i.ChildId, i.OccurredAt });

        builder.HasOne(i => i.Child)
            .WithMany(c => c.Incidents)
            .HasForeignKey(i => i.ChildId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Classroom)
            .WithMany()
            .HasForeignKey(i => i.ClassroomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.AcknowledgedByGuardian)
            .WithMany()
            .HasForeignKey(i => i.AcknowledgedByGuardianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
