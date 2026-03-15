using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Installment : BaseEntity
    {
        public int SaleId { get; set; }
        public Sale Sale { get; set; }

        public decimal TotalAmount { get; set; }      // إجمالي القسط
        public decimal DownPayment { get; set; }      // المقدم
        public decimal RemainingAmount { get; set; }  // المتبقي
        public int Months { get; set; }               // عدد الشهور
        public decimal MonthlyPayment { get; set; }   // القسط الشهري
        public DateTime StartDate { get; set; }       // تاريخ البداية

        public ICollection<Payment> Payments { get; set; }
    }
}
