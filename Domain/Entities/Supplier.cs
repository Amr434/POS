using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Supplier : BaseEntity
    {
        public string Name { get; set; }     // اسم المورد
        public string Phone { get; set; }    // الهاتف
        public string Address { get; set; }  // العنوان
    }
}
