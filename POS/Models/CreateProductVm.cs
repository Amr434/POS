using Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

public class CreateProductVm
{
    [Required(ErrorMessage = "اسم المنتج مطلوب")]
    [MaxLength(200)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "الفئة مطلوبة")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "سعر الشراء مطلوب")]
    [Range(0.01, double.MaxValue, ErrorMessage = "سعر غير صحيح")]
    public decimal PurchasePrice { get; set; }

    [Required(ErrorMessage = "سعر البيع مطلوب")]
    [Range(0.01, double.MaxValue, ErrorMessage = "سعر غير صحيح")]
    public decimal SalePrice { get; set; }

    [Required(ErrorMessage = "الكمية مطلوبة")]
    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0, int.MaxValue)]
    public int MinStock { get; set; } = 5;

    [MaxLength(100)]
    public string? Barcode { get; set; }

    [Required(ErrorMessage = "حالة المنتج مطلوبة")]
    public ProductStatus Status { get; set; } = ProductStatus.New;

    public IFormFile? Image { get; set; }

    // Motorcycle specific fields
    public bool IsMotorcycle { get; set; }

    [MaxLength(100)]
    public string? EngineNumber { get; set; }

    [MaxLength(100)]
    public string? ChassisNumber { get; set; }

    public List<SelectListItem>? Categories { get; set; }
}