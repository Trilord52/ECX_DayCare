using Daycare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Daycare.Infrastructure.Persistence.Configurations;

public class DailyReportEntryConfiguration : IEntityTypeConfiguration<DailyReportEntry>
{
    public void Configure(EntityTypeBuilder<DailyReportEntry> builder)
    {
        builder.ToTable("daily_report_entries");

        builder.HasKey(dre => dre.Id);

        builder.Property(dre => dre.EntryType)
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(dre => dre.OccurredAt)
            .IsRequired();

        builder.Property(dre => dre.MealType)
            .HasMaxLength(20)
            .HasConversion<string>();

        builder.Property(dre => dre.AmountEaten)
            .HasMaxLength(10)
            .HasConversion<string>();

        builder.Property(dre => dre.DiaperType)
            .HasMaxLength(10)
            .HasConversion<string>();

        builder.Property(dre => dre.Description)
            .HasMaxLength(1000);

        builder.Property(dre => dre.CreatedByName)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(dre => new { dre.DailyReportId, dre.OccurredAt });

        builder.HasOne(dre => dre.DailyReport)
            .WithMany(dr => dr.Entries)
            .HasForeignKey(dre => dre.DailyReportId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
