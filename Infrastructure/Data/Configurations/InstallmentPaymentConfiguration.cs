using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POS.Infrastructure.Data.Configurations;

public class InstallmentPaymentConfiguration : IEntityTypeConfiguration<InstallmentPayment>
{
    public void Configure(EntityTypeBuilder<InstallmentPayment> builder)
    {
        builder.ToTable("InstallmentPayments");
        builder.HasKey(ip => ip.Id);

        builder.Property(ip => ip.PaymentNumber)
               .IsRequired();

        builder.Property(ip => ip.DueDate)
               .IsRequired();

        builder.Property(ip => ip.AmountDue)
               .HasColumnType("decimal(18,2)")
               .IsRequired();

        builder.Property(ip => ip.AmountPaid)
               .HasColumnType("decimal(18,2)")
               .IsRequired()
               .HasDefaultValue(0);

        builder.Property(ip => ip.PaymentDate);

        builder.Property(ip => ip.Status)
               .HasConversion<int>()
               .IsRequired();

        builder.Property(ip => ip.Notes)
               .HasMaxLength(500);

        // Indexes
        builder.HasIndex(ip => ip.InstallmentPlanId);
        builder.HasIndex(ip => ip.DueDate);
        builder.HasIndex(ip => ip.Status);
        
        // Composite unique index to prevent duplicate payment numbers per plan
        builder.HasIndex(ip => new { ip.InstallmentPlanId, ip.PaymentNumber })
               .IsUnique();

        // Relationships
        builder.HasOne(ip => ip.InstallmentPlan)
               .WithMany(plan => plan.Payments)
               .HasForeignKey(ip => ip.InstallmentPlanId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}