using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POS.Infrastructure.Data.Configurations
{
    public class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
    {
        public void Configure(EntityTypeBuilder<Purchase> builder)
        {
            builder.ToTable("Purchases");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.TotalAmount).IsRequired();
            builder.Property(p => p.PurchaseDate).IsRequired();
            builder.Property(p => p.Status)
                   .IsRequired()
                   .HasDefaultValue(PurchaseStatus.Draft);

            builder.HasOne(p => p.Supplier)
                   .WithMany()
                   .HasForeignKey(p => p.SupplierId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}