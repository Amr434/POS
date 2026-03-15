using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POS.Infrastructure.Data.Configurations
{
    public class InstallmentConfiguration : IEntityTypeConfiguration<Installment>
    {
        public void Configure(EntityTypeBuilder<Installment> builder)
        {
            builder.ToTable("Installments");
            builder.HasKey(i => i.Id);

            builder.Property(i => i.TotalAmount).IsRequired();
            builder.Property(i => i.DownPayment).IsRequired();
            builder.Property(i => i.RemainingAmount).IsRequired();
            builder.Property(i => i.Months).IsRequired();
            builder.Property(i => i.MonthlyPayment).IsRequired();
            builder.Property(i => i.StartDate).IsRequired();

            builder.HasOne(i => i.Sale)
                   .WithMany()
                   .HasForeignKey(i => i.SaleId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}