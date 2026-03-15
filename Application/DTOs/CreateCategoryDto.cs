using System.ComponentModel.DataAnnotations;

namespace POS.Application.DTOs
{
    public class CreateCategoryDto
    {
        [Required(ErrorMessage = "اسم التصنيف مطلوب")]
        [StringLength(100, ErrorMessage = "اسم التصنيف يجب ألا يتجاوز 100 حرف")]
        public string Name { get; set; }
    }
}