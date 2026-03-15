using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POS.Infrastructure.Data.Configurations
{
    public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
    {
        public void Configure(EntityTypeBuilder<SaleItem> builder)
        {
            builder.ToTable("SaleItems");
            builder.HasKey(si => si.Id);

            builder.Property(si => si.Quantity).IsRequired();
            builder.Property(si => si.Price).IsRequired();
            builder.Property(si => si.Total).IsRequired();

            builder.HasOne(si => si.Sale)
                   .WithMany(s => s.Items)
                   .HasForeignKey(si => si.SaleId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(si => si.Product)
                   .WithMany()
                   .HasForeignKey(si => si.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}