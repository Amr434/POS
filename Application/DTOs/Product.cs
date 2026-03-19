namespace POS.Application.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Barcode { get; set; }
        public decimal SalePrice { get; set; }
        public int CurrentStock { get; set; }
        public int MinStock { get; set; }
        public string Status { get; set; }
        public string CategoryName { get; set; }
    }

    public class CreateProductDto
    {
        public string Name { get; set; }
        public string Barcode { get; set; }
        public decimal SalePrice { get; set; }
        public int MinStock { get; set; }
        public int CategoryId { get; set; }
        public int Status { get; set; } // Enum: New, Reserved, Sold
    }

    public class UpdateProductDto : CreateProductDto
    {
        public int Id { get; set; }
    }
}