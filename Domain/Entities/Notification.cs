using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Notification : BaseEntity
    {
        public string Title { get; set; }   // العنوان
        public string Message { get; set; } // الرسالة
        public bool IsRead { get; set; }    // مقروء
        public DateTime Date { get; set; }  // تاريخ التنبيه
    }
}
