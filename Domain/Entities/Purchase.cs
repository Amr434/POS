using System;
using System.Collections.Generic;
using System.Text;
using Domain.Enums;

namespace Domain.Entities
{
    public class Purchase : BaseEntity
    {
        public int? SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        public DateTime PurchaseDate { get; set; } // تاريخ الشراء
        public decimal TotalAmount { get; set; }   // الإجمالي
        public PurchaseStatus Status { get; set; } = PurchaseStatus.Draft;

        public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
    }
}
