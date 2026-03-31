using Domain.Entities;
using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class CreateSaleDto
{
    public int CustomerId { get; set; }

    public DateTime SaleDate { get; set; } = DateTime.Now;

    public List<SaleItemDto> Items { get; set; } = new();

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public PaymentMethod paymentType { get; set; }
    
    // ✅ إضافة بيانات التقسيط
    public InstallmentDto? Installment { get; set; }
}

public class SaleItemDto
{
    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Total { get; set; }
}

// ✅ DTO جديد للتقسيط
public class InstallmentDto
{
    public int NumberOfMonths { get; set; }
    
    public decimal DownPayment { get; set; }
    
    public decimal InterestRate { get; set; }
}

