namespace Domain.Entities;

public class InstallmentPayment
{
    public int Id { get; set; }
    
    // Foreign Key
    public int InstallmentPlanId { get; set; }
    
    // Payment Details
    public int PaymentNumber { get; set; }  // رقم القسط (1, 2, 3...)
    public DateTime DueDate { get; set; }  // تاريخ الاستحقاق
    public decimal AmountDue { get; set; }  // المبلغ المستحق
    public decimal AmountPaid { get; set; }  // المبلغ المدفوع
    public DateTime? PaymentDate { get; set; }  // تاريخ الدفع الفعلي
    
    // Status
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? Notes { get; set; }
    
    // Navigation Properties
    public InstallmentPlan InstallmentPlan { get; set; } = null!;
}

public enum PaymentStatus
{
    Pending = 1,        // معلق
    Paid = 2,           // مدفوع
    Overdue = 3,        // متأخر
    PartiallyPaid = 4   // مدفوع جزئياً
}