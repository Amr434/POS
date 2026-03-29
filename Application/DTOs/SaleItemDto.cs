using System.ComponentModel.DataAnnotations;

public class SaleItemDto
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
    public int Quantity { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "السعر غير صحيح")]
    public decimal UnitPrice { get; set; }

    public decimal Total => Quantity * UnitPrice;
}