using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class AddInvoiceItemRequest
    {
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        public decimal? UnitCost { get; set; }
        public string? Notes { get; set; }
    }
}
