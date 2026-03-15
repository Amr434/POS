using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Payment : BaseEntity
    {
        public int InstallmentId { get; set; }
        public Installment Installment { get; set; }

        public decimal Amount { get; set; }      // المبلغ
        public DateTime PaymentDate { get; set; } // تاريخ الدفع
        public string Notes { get; set; }        // ملاحظات
    }
}
