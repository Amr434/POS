using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class SaleItem : BaseEntity
    {
        // Primary Key

        // Foreign Keys
        public int SaleId { get; set; }
        public int ProductId { get; set; }

        // Item Details
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }  // سعر المنتج وقت البيع
        public decimal DiscountPercentage { get; set; }
        public decimal TotalPrice { get; set; }  // الإجمالي بعد الخصم

        // Navigation Properties
        public Sale Sale { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
