using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POS.Infrastructure.Data.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");
            builder.HasKey(p => p.Id);

            builder.Property(p => p.Amount).IsRequired();
            builder.Property(p => p.PaymentDate).IsRequired();
            builder.Property(p => p.Notes).HasMaxLength(300);

            builder.HasOne(p => p.Installment)
                   .WithMany(i => i.Payments)
                   .HasForeignKey(p => p.InstallmentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}