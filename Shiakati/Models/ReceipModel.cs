using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class ReceipModel
    {
        public string TicketNumber { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public List<ReceiptItem> Items { get; set; } = new();
        public decimal TotalDiscount { get; set; }
        public decimal TotalAmount { get; set; }
    }    
}
