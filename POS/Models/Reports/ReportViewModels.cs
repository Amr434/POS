using System;
using System.Collections.Generic;

namespace POS.Models.Reports
{
    public class ReportsDashboardVm
    {
        // Placeholder for future dashboard metrics (kept intentionally minimal).
    }

    public class ReportFiltersVm
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string GroupBy { get; set; } = "day"; // day|month
    }

    public class SalesReportVm : ReportFiltersVm
    {
        public int TotalSales { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }

        public List<SaleRowVm> Rows { get; set; } = new();
        public List<SalesGroupVm> Groups { get; set; } = new();
    }

    public class SaleRowVm
    {
        public int Id { get; set; }
        public DateTime SaleDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int ItemsCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
    }

    public class SalesGroupVm
    {
        public DateTime Period { get; set; }
        public int SalesCount { get; set; }
        public int ItemsCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
    }

    public class InventoryReportVm : ReportFiltersVm
    {
        public int TotalProducts { get; set; }
        public int TotalStockUnits { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }

        public List<InventoryRowVm> Rows { get; set; } = new();
        public List<InventoryPurchaseGroupVm> PurchaseGroups { get; set; } = new();
    }

    public class InventoryRowVm
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public int TotalStock { get; set; }
        public int MinStock { get; set; }
        public string StockStatus { get; set; } = "normal"; // normal|low|out
    }

    public class InventoryPurchaseGroupVm
    {
        public DateTime Period { get; set; }
        public int PurchasedUnits { get; set; }
        public decimal PurchasedValue { get; set; }
    }

    public class ExpensesReportVm : ReportFiltersVm
    {
        public decimal TotalAmount { get; set; }
        public List<ExpenseRowVm> Rows { get; set; } = new();
        public List<ExpensesGroupVm> Groups { get; set; } = new();
    }

    public class ExpenseRowVm
    {
        public int Id { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
    }

    public class ExpensesGroupVm
    {
        public DateTime Period { get; set; }
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ProfitReportVm : ReportFiltersVm
    {
        public decimal Revenue { get; set; }
        public decimal EstimatedCogs { get; set; }
        public decimal Expenses { get; set; }
        public decimal Profit { get; set; }

        public List<ProfitGroupVm> Groups { get; set; } = new();
    }

    public class ProfitGroupVm
    {
        public DateTime Period { get; set; }
        public decimal Revenue { get; set; }
        public decimal EstimatedCogs { get; set; }
        public decimal Expenses { get; set; }
        public decimal Profit { get; set; }
    }
}

