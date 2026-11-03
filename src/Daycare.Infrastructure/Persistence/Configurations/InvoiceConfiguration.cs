using Daycare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Daycare.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices", t =>
        {
            t.HasCheckConstraint("ck_invoices_total_amount", "total_amount >= 0");
            t.HasCheckConstraint("ck_invoices_amount_paid", "amount_paid >= 0");
        });

        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvoiceNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(i => i.InvoiceNumber)
            .IsUnique();

        builder.Property(i => i.PeriodStart)
            .IsRequired();

        builder.Property(i => i.PeriodEnd)
            .IsRequired();

        builder.Property(i => i.IssueDate)
            .IsRequired();

        builder.Property(i => i.DueDate)
            .IsRequired();

        builder.Property(i => i.Status)
            .HasMaxLength(15)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(i => i.Currency)
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired()
            .HasDefaultValue("ETB");

        builder.Property(i => i.TotalAmount)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(i => i.AmountPaid)
            .HasPrecision(12, 2)
            .IsRequired()
            .HasDefaultValue(0m);

        builder.Property(i => i.VoidReason)
            .HasMaxLength(300);

        builder.HasIndex(i => new { i.ChildId, i.PeriodStart })
            .IsUnique()
            .HasFilter("status <> 'Void'");

        builder.HasIndex(i => i.GuardianId);
        builder.HasIndex(i => new { i.Status, i.DueDate });

        builder.HasOne(i => i.Child)
            .WithMany(c => c.Invoices)
            .HasForeignKey(i => i.ChildId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Guardian)
            .WithMany()
            .HasForeignKey(i => i.GuardianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
