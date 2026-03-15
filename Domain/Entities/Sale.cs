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

        public decimal TotalAmount { get; set; }    // الإجمالي
        public decimal PaidAmount { get; set; }     // المدفوع
        public decimal RemainingAmount { get; set; } // المتبقي

        public DateTime SaleDate { get; set; }      // تاريخ البيع
        public PaymentType PaymentType { get; set; } // نوع الدفع (نقد/تقسيط)

        public ICollection<SaleItem> Items { get; set; }
    }
}
