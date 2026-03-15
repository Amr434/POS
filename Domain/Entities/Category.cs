using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } // اسم التصنيف

        public ICollection<Product> Products { get; set; }
    }
}
