using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get; set; }       // اسم المنتج
        public string Barcode { get; set; }    // باركود
        public decimal PurchasePrice { get; set; } // سعر الشراء
        public decimal SalePrice { get; set; }     // سعر البيع
        public int Quantity { get; set; }          // الكمية
        public int MinStock { get; set; }   
        // الحد الأدنى للمخزون
        public string? ImagePath{ get; set; } 
        public ProductStatus Status { get; set; }  // الحالة (جديد/محجوز/مباع)

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        // Optional for motorcycles
        public string ?EngineNumber { get; set; }
        public string? ChassisNumber { get; set; }
    }
}
