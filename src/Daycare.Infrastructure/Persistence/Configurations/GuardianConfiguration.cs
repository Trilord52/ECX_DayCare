using Daycare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Daycare.Infrastructure.Persistence.Configurations;

public class GuardianConfiguration : IEntityTypeConfiguration<Guardian>
{
    public void Configure(EntityTypeBuilder<Guardian> builder)
    {
        builder.ToTable("guardians", t =>
        {
            t.HasCheckConstraint("ck_guardians_photo_all_or_none", "num_nonnulls(photo_key, photo_content_type, photo_size_bytes, photo_updated_at) IN (0, 4)");
        });

        builder.HasKey(g => g.Id);

        builder.OwnsOne(g => g.Photo, photo =>
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

        builder.Property(g => g.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(g => g.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(g => g.Phone)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(g => g.Email)
            .HasMaxLength(256);

        builder.Property(g => g.Address)
            .HasMaxLength(500);

        builder.Property(g => g.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(g => g.Phone);

        builder.HasIndex(g => g.UserId)
            .IsUnique()
            .HasFilter("user_id IS NOT NULL");
    }
}
