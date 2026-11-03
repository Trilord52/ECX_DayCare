using Daycare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Daycare.Infrastructure.Persistence.Configurations;

public class ClassroomConfiguration : IEntityTypeConfiguration<Classroom>
{
    public void Configure(EntityTypeBuilder<Classroom> builder)
    {
        builder.ToTable("classrooms", t =>
        {
            t.HasCheckConstraint("ck_classrooms_min_age_months", "min_age_months >= 0");
            t.HasCheckConstraint("ck_classrooms_max_age_months", "max_age_months >= min_age_months");
            t.HasCheckConstraint("ck_classrooms_capacity", "capacity > 0");
            t.HasCheckConstraint("ck_classrooms_max_children_per_staff", "max_children_per_staff > 0");
        });

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();

        // Note: Case-insensitive uniqueness is enforced at DB level via:
        // CREATE UNIQUE INDEX ix_classrooms_lower_name ON classrooms (lower(name));
        // (Managed in EF Core migration)

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.MinAgeMonths)
            .IsRequired();

        builder.Property(c => c.MaxAgeMonths)
            .IsRequired();

        builder.Property(c => c.Capacity)
            .IsRequired();

        builder.Property(c => c.MaxChildrenPerStaff)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}
