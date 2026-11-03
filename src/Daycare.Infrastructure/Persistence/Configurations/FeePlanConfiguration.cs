using Daycare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Daycare.Infrastructure.Persistence.Configurations;

public class FeePlanConfiguration : IEntityTypeConfiguration<FeePlan>
{
    public void Configure(EntityTypeBuilder<FeePlan> builder)
    {
        builder.ToTable("fee_plans", t =>
        {
            t.HasCheckConstraint("ck_fee_plans_monthly_amount", "monthly_amount >= 0");
        });

        builder.HasKey(fp => fp.Id);

        builder.Property(fp => fp.Name)
            .HasMaxLength(100)
            .IsRequired();

        // Note: Case-insensitive uniqueness is enforced at DB level via:
        // CREATE UNIQUE INDEX ix_fee_plans_lower_name ON fee_plans (lower(name));
        // (Managed in EF Core migration)

        builder.Property(fp => fp.Description)
            .HasMaxLength(500);

        builder.Property(fp => fp.MonthlyAmount)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(fp => fp.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
    }
}
