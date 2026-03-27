using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Net.ServerSentEvents;
using System.Text;

namespace Domain.Entities
{
    public class Sale : BaseEntity
    {
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }

        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }    // الإجمالي
        public decimal PaidAmount { get; set; }     // المدفوع
        public decimal RemainingAmount { get; set; } // المتبقي

        public DateTime SaleDate { get; set; }      // تاريخ البيع
        public PaymentMethod PaymentMethod { get; set; } // نوع الدفع (نقد/تقسيط)

        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
        public InstallmentPlan? InstallmentPlan { get; set; }
    }

    public enum PaymentMethod
    {
        Cash = 1,
        Installment = 2
    }
}
