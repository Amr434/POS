using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class PurchaseItem : BaseEntity
    {
        public int PurchaseId { get; set; }
        public Purchase Purchase { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; } // سعر الشراء في وقت الشراء
    }
}
