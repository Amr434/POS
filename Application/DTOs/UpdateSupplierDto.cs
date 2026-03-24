using System.ComponentModel.DataAnnotations;

namespace POS.Application.DTOs
{
    public class UpdateSupplierDto
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم المورد مطلوب")]
        [StringLength(200, ErrorMessage = "اسم المورد يجب ألا يتجاوز 200 حرف")]
        public string Name { get; set; }

        [StringLength(20, ErrorMessage = "الهاتف يجب ألا يتجاوز 20 حرف")]
        public string? Phone { get; set; }

        [StringLength(300, ErrorMessage = "العنوان يجب ألا يتجاوز 300 حرف")]
        public string? Address { get; set; }
    }
}

