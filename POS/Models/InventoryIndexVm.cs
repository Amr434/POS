namespace POS.Models
{
    public class InventoryIndexVm
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public string Barcode { get; set; }
        public string? ImagePath { get; set; }
        public int TotalStock { get; set; }      // مجموع الكميات المتبقية
        public int MinStock { get; set; }         // الحد الأدنى للمخزون
        public string StockStatus { get; set; }   // Normal / Low / Out

        public static string GetStockStatus(int totalStock, int minStock)
        {
            if (totalStock <= 0) return "out";
            if (totalStock <= minStock) return "low";
            return "normal";
        }

        public static string GetStockStatusText(string status)
        {
            return status switch
            {
                "out" => "نفذ",
                "low" => "منخفض",
                _ => "متوفر"
            };
        }
    }
}
