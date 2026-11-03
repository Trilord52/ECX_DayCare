using Daycare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Daycare.Infrastructure.Persistence.Configurations;

public class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.ToTable("announcements", t =>
        {
            t.HasCheckConstraint(
                "ck_announcements_audience_classroom",
                "(audience = 'Classroom' AND classroom_id IS NOT NULL) OR (audience <> 'Classroom' AND classroom_id IS NULL)");
        });

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Body)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(a => a.Audience)
            .HasMaxLength(15)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(a => a.CreatedByName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.PublishedAt)
            .IsRequired();

        builder.HasOne(a => a.Classroom)
            .WithMany()
            .HasForeignKey(a => a.ClassroomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
