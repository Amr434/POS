using Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace POS.Models
{
    public class PurchaseFormVm
    {
        public int Id { get; set; }

        [Display(Name = "المورد")]
        public int? SupplierId { get; set; }

        [Required(ErrorMessage = "تاريخ الشراء مطلوب")]
        [Display(Name = "تاريخ الشراء")]
        public DateTime PurchaseDate { get; set; } = DateTime.Today;

        public PurchaseStatus Status { get; set; } = PurchaseStatus.Draft;

        public List<PurchaseItemInputVm> Items { get; set; } = new() { new PurchaseItemInputVm() };

        public List<SelectListItem> Suppliers { get; set; } = new();
        public List<SelectListItem> Products { get; set; } = new();

        public decimal Total => Items.Sum(i => i.LineTotal);
    }

    public class PurchaseItemInputVm
    {
        [Range(1, int.MaxValue, ErrorMessage = "اختر منتج")]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "سعر الشراء غير صحيح")]
        public decimal UnitPrice { get; set; }

        public decimal LineTotal => Quantity * UnitPrice;
    }
}
