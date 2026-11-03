using Daycare.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Daycare.Infrastructure.Persistence.Configurations;

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable("invoice_lines", t =>
        {
            t.HasCheckConstraint("ck_invoice_lines_quantity", "quantity > 0");
            t.HasCheckConstraint("ck_invoice_lines_unit_price", "line_type = 'Adjustment' OR unit_price >= 0");
        });

        builder.HasKey(il => il.Id);

        builder.Property(il => il.LineType)
            .HasMaxLength(15)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(il => il.Description)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(il => il.Quantity)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(il => il.UnitPrice)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(il => il.LineTotal)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(il => il.SortOrder)
            .IsRequired();

        builder.HasOne(il => il.Invoice)
            .WithMany(i => i.Lines)
            .HasForeignKey(il => il.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
