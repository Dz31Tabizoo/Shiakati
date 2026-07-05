using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class UpdateInvoiceRequest
    {
        public int InvoiceId { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public int? ProductsTotal { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? AmountPaid { get; set; }
    }
}
