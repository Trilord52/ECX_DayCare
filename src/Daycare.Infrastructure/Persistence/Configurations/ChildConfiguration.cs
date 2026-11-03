using Daycare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Daycare.Infrastructure.Persistence.Configurations;

public class ChildConfiguration : IEntityTypeConfiguration<Child>
{
    public void Configure(EntityTypeBuilder<Child> builder)
    {
        builder.ToTable("children", t =>
        {
            t.HasCheckConstraint("ck_children_photo_all_or_none", "num_nonnulls(photo_key, photo_content_type, photo_size_bytes, photo_updated_at) IN (0, 4)");
        });

        builder.HasKey(c => c.Id);

        builder.OwnsOne(c => c.Photo, photo =>
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

        builder.Property(c => c.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.DateOfBirth)
            .IsRequired();

        builder.Property(c => c.Gender)
            .HasMaxLength(10)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(c => c.Status)
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(c => c.EnrollmentDate)
            .IsRequired();

        builder.Property(c => c.Allergies)
            .HasMaxLength(1000);

        builder.Property(c => c.MedicalNotes)
            .HasMaxLength(2000);

        builder.Property(c => c.DietaryNotes)
            .HasMaxLength(1000);

        builder.Property(c => c.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(c => new { c.LastName, c.FirstName });
        builder.HasIndex(c => c.ClassroomId);
        builder.HasIndex(c => c.Status);

        builder.HasOne(c => c.Classroom)
            .WithMany(cl => cl.Children)
            .HasForeignKey(c => c.ClassroomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.FeePlan)
            .WithMany(fp => fp.Children)
            .HasForeignKey(c => c.FeePlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
