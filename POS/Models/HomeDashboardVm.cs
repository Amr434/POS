using System;
using System.Collections.Generic;

namespace POS.Models
{
    public class HomeDashboardVm
    {
        public DateTime Today { get; set; } = DateTime.Today;

        public decimal TodaySalesTotal { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockCount { get; set; }
        public int TodayInvoicesCount { get; set; }

        public List<RecentSaleVm> RecentSales { get; set; } = new();
        public List<LowStockVm> LowStockItems { get; set; } = new();

        public class RecentSaleVm
        {
            public int SaleId { get; set; }
            public DateTime SaleDate { get; set; }
            public string CustomerName { get; set; } = "";
            public decimal TotalAmount { get; set; }
            public decimal RemainingAmount { get; set; }
        }

        public class LowStockVm
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = "";
            public int RemainingQuantity { get; set; }
            public int MinStock { get; set; }
        }
    }
}

