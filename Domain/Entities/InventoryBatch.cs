using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class InventoryBatch : BaseEntity
    {
        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int PurchaseItemId { get; set; }
        public PurchaseItem PurchaseItem { get; set; }

        public int Quantity { get; set; } // الكمية المشتراة
        public int RemainingQuantity { get; set; } // الكمية المتبقية في المخزون
        public decimal UnitPrice { get; set; } // سعر الشراء

        public DateTime PurchaseDate { get; set; }
    }
}