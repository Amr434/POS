using System;
using System.Collections.Generic;
using System.Text;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POS.Infrastructure.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id)
                .ValueGeneratedOnAdd();

            builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
            builder.Property(p => p.Barcode).HasMaxLength(50);
            builder.Property(p => p.PurchasePrice).IsRequired();
            builder.Property(p => p.SalePrice).IsRequired();
            builder.Property(p => p.Quantity).IsRequired();
            builder.Property(p => p.MinStock)
                        .IsRequired()
                        .HasDefaultValue(5);

            builder.Property(p => p.ImagePath)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(p => p.Status).HasConversion<int>();

            builder.Property(p => p.EngineNumber).HasMaxLength(50)
                .IsRequired(false);
            builder.Property(p => p.ChassisNumber).HasMaxLength(50).IsRequired(false);

            builder.HasOne(p => p.Category)
                   .WithMany(c => c.Products)
                   .HasForeignKey(p => p.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}