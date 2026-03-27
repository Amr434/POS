using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POS.Infrastructure.Data.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SaleDate)
               .IsRequired();

        builder.Property(s => s.Subtotal)
               .HasColumnType("decimal(18,2)");
               //.IsRequired();

        builder.Property(s => s.DiscountAmount)
               .HasColumnType("decimal(18,2)")
               .HasDefaultValue(0);

        builder.Property(s => s.TotalAmount)
               .HasColumnType("decimal(18,2)")
               .IsRequired();

        builder.Property(s => s.PaymentMethod)
               .HasConversion<int>()
               .IsRequired();

        builder.Property(s => s.Notes)
               .HasMaxLength(1000);

        builder.Property(s => s.CreatedAt)
               .IsRequired();

        // Indexes
        builder.HasIndex(s => s.SaleDate);
        builder.HasIndex(s => s.CustomerId);
        builder.HasIndex(s => s.PaymentMethod);

        // Relationships
        builder.HasOne(s => s.Customer)
               .WithMany(c => c.Sales)
               .HasForeignKey(s => s.CustomerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(s => s.SaleItems)
               .WithOne(si => si.Sale)
               .HasForeignKey(si => si.SaleId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.InstallmentPlan)
               .WithOne(ip => ip.Sale)
               .HasForeignKey<InstallmentPlan>(ip => ip.SaleId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}