using Daycare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Daycare.Infrastructure.Persistence.Configurations;

public class DailyReportConfiguration : IEntityTypeConfiguration<DailyReport>
{
    public void Configure(EntityTypeBuilder<DailyReport> builder)
    {
        builder.ToTable("daily_reports");

        builder.HasKey(dr => dr.Id);

        builder.Property(dr => dr.Date)
            .IsRequired();

        builder.Property(dr => dr.Mood)
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(dr => dr.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(dr => new { dr.ChildId, dr.Date })
            .IsUnique();

        builder.HasOne(dr => dr.Child)
            .WithMany(c => c.DailyReports)
            .HasForeignKey(dr => dr.ChildId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(dr => dr.Classroom)
            .WithMany()
            .HasForeignKey(dr => dr.ClassroomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
