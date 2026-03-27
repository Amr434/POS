namespace Domain.Entities;

public class InstallmentPlan
{
    public int Id { get; set; }
    
    // Foreign Key
    public int SaleId { get; set; }
    
    // Installment Details
    public int NumberOfMonths { get; set; }
    public decimal InterestRate { get; set; }  // نسبة الفائدة الشهرية
    public decimal TotalWithInterest { get; set; }  // الإجمالي مع الفائدة
    public decimal MonthlyPaymentAmount { get; set; }  // القسط الشهري
    public decimal DownPayment { get; set; }  // الدفعة المقدمة
    public decimal RemainingAmount { get; set; }  // المتبقي مع الفائدة
    
    // Status
    public DateTime StartDate { get; set; } = DateTime.Now;
    public InstallmentStatus Status { get; set; } = InstallmentStatus.Active;
    
    // Navigation Properties
    public Sale Sale { get; set; } = null!;
    public ICollection<InstallmentPayment> Payments { get; set; } = new List<InstallmentPayment>();
}

public enum InstallmentStatus
{
    Active = 1,      // نشط
    Completed = 2,   // مكتمل
    Defaulted = 3,   // متعثر
    Cancelled = 4    // ملغي
}