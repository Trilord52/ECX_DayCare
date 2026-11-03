using Daycare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Daycare.Infrastructure.Persistence.Configurations;

public class ChildGuardianConfiguration : IEntityTypeConfiguration<ChildGuardian>
{
    public void Configure(EntityTypeBuilder<ChildGuardian> builder)
    {
        builder.ToTable("child_guardians");

        builder.HasKey(cg => cg.Id);

        builder.Property(cg => cg.Relationship)
            .HasMaxLength(30)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(cg => cg.IsPrimaryContact)
            .IsRequired();

        builder.Property(cg => cg.CanPickUp)
            .IsRequired();

        builder.Property(cg => cg.IsPayer)
            .IsRequired();

        builder.Property(cg => cg.IsEmergencyContact)
            .IsRequired();

        builder.HasIndex(cg => new { cg.ChildId, cg.GuardianId })
            .IsUnique();

        builder.HasIndex(cg => cg.ChildId)
            .IsUnique()
            .HasFilter("is_primary_contact = true");

        builder.HasOne(cg => cg.Child)
            .WithMany(c => c.Guardians)
            .HasForeignKey(cg => cg.ChildId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cg => cg.Guardian)
            .WithMany(g => g.Children)
            .HasForeignKey(cg => cg.GuardianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
