using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace POS.Infrastructure.Data.Configurations;

public class InstallmentPlanConfiguration : IEntityTypeConfiguration<InstallmentPlan>
{
    public void Configure(EntityTypeBuilder<InstallmentPlan> builder)
    {
        builder.ToTable("InstallmentPlans");
        builder.HasKey(ip => ip.Id);

        builder.Property(ip => ip.NumberOfMonths)
               .IsRequired();

        builder.Property(ip => ip.InterestRate)
               .HasColumnType("decimal(5,2)")
               .IsRequired();

        builder.Property(ip => ip.TotalWithInterest)
               .HasColumnType("decimal(18,2)")
               .IsRequired();

        builder.Property(ip => ip.MonthlyPaymentAmount)
               .HasColumnType("decimal(18,2)")
               .IsRequired();

        builder.Property(ip => ip.DownPayment)
               .HasColumnType("decimal(18,2)")
               .IsRequired();

        builder.Property(ip => ip.RemainingAmount)
               .HasColumnType("decimal(18,2)")
               .IsRequired();

        builder.Property(ip => ip.StartDate)
               .IsRequired();

        builder.Property(ip => ip.Status)
               .HasConversion<int>()
               .IsRequired();

        // Indexes
        builder.HasIndex(ip => ip.SaleId)
               .IsUnique();
        builder.HasIndex(ip => ip.Status);
        builder.HasIndex(ip => ip.StartDate);

        // Relationships
        builder.HasOne(ip => ip.Sale)
               .WithOne(s => s.InstallmentPlan)
               .HasForeignKey<InstallmentPlan>(ip => ip.SaleId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(ip => ip.Payments)
               .WithOne(p => p.InstallmentPlan)
               .HasForeignKey(p => p.InstallmentPlanId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}