using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Customer : BaseEntity
    {
        public string Name { get; set; }      // الاسم
        public string Phone { get; set; }     // الهاتف
        public string Address { get; set; }   // العنوان
        public string NationalId { get; set; } // الرقم القومي

        public ICollection<Sale> Sales { get; set; }
    }
}
