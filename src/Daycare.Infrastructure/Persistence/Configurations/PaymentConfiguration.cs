using Daycare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Daycare.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", t =>
        {
            t.HasCheckConstraint("ck_payments_amount", "amount > 0");
        });

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(p => p.Method)
            .HasMaxLength(15)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(p => p.Reference)
            .HasMaxLength(100);

        builder.Property(p => p.PaidAt)
            .IsRequired();

        builder.Property(p => p.Notes)
            .HasMaxLength(500);

        builder.Property(p => p.RecordedByName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.VoidReason)
            .HasMaxLength(300);

        builder.Property(p => p.VoidedByUserId);

        builder.Property(p => p.VoidedByName)
            .HasMaxLength(200);

        builder.HasIndex(p => p.InvoiceId);

        builder.HasIndex(p => new { p.Method, p.Reference })
            .IsUnique()
            .HasFilter("reference IS NOT NULL AND voided_at IS NULL");

        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
