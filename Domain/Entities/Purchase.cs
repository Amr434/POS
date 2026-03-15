using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Purchase : BaseEntity
    {
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; }

        public DateTime PurchaseDate { get; set; } // تاريخ الشراء
        public decimal TotalAmount { get; set; }   // الإجمالي

        public ICollection<PurchaseItem> Items { get; set; }
    }
}
