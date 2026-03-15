using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Expense : BaseEntity
    {
        public string Title { get; set; }       // اسم المصروف
        public decimal Amount { get; set; }     // المبلغ
        public DateTime ExpenseDate { get; set; } // تاريخ المصروف
        public string Notes { get; set; }       // ملاحظات
    }
}
