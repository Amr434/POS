using Domain.Enums;
using Microsoft.AspNetCore.Http;

public class QuickEditProductDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int CategoryId { get; set; }
    public decimal PurchasePrice { get; set; }
    public IFormFile ?Image { get; set; }
    public decimal SalePrice { get; set; }
    public int Quantity { get; set; }
    public int MinStock { get; set; }
    public string Barcode { get; set; }
    public ProductStatus Status { get; set; }
    public bool IsMotorcycle { get; set; }
    public string? EngineNumber { get; set; }
    public string ?ChassisNumber { get; set; }
}
