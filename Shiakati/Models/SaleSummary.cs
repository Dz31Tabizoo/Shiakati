using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class SaleSummary
    {
        public int SaleId { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public DateTime? SaleDate { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? GlobalDiscount { get; set; }
    }
}
