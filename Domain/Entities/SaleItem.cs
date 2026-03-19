using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class SaleItem : BaseEntity
    {
        public int SaleId { get; set; }
        public Sale Sale { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }

        public int Quantity { get; set; }
        public decimal UnitSalePrice { get; set; } // سعر البيع الفعلي وقت البيع
        public decimal Total { get; set; }
    }
}
