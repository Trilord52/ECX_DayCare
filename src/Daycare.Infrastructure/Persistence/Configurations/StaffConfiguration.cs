using Daycare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Daycare.Infrastructure.Persistence.Configurations;

public class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> builder)
    {
        builder.ToTable("staff", t =>
        {
            t.HasCheckConstraint("ck_staff_photo_all_or_none", "num_nonnulls(photo_key, photo_content_type, photo_size_bytes, photo_updated_at) IN (0, 4)");
        });

        builder.HasKey(s => s.Id);

        builder.OwnsOne(s => s.Photo, photo =>
        {
            photo.Property(p => p.Key)
                .HasColumnName("photo_key")
                .HasMaxLength(120);

            photo.Property(p => p.ContentType)
                .HasColumnName("photo_content_type")
                .HasMaxLength(30);

            photo.Property(p => p.SizeBytes)
                .HasColumnName("photo_size_bytes");

            photo.Property(p => p.UpdatedAt)
                .HasColumnName("photo_updated_at");
        });

        builder.Property(s => s.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Phone)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(s => s.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(s => s.JobTitle)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.HireDate)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(s => s.UserId)
            .IsUnique()
            .HasFilter("user_id IS NOT NULL");
    }
}
