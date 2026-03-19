using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POS.Infrastructure.Data.Configurations
{
    public class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
    {
        public void Configure(EntityTypeBuilder<PurchaseItem> builder)
        {
            builder.ToTable("PurchaseItems");
            builder.HasKey(pi => pi.Id);

            builder.Property(pi => pi.Quantity).IsRequired();
            builder.Property(pi => pi.UnitPrice).IsRequired();

            builder.HasOne(pi => pi.Purchase)
                   .WithMany(p => p.Items)
                   .HasForeignKey(pi => pi.PurchaseId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pi => pi.Product)
                   .WithMany()
                   .HasForeignKey(pi => pi.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}