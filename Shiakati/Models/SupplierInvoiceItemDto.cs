using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class SupplierInvoiceItemDto
    {
        public int SupplierInvoiceItemId { get; set; }
        public int SupplierInvoiceId { get; set; }
        public int VariantId { get; set; }
        public string? VariantName { get; set; }
        public string? Sku { get; set; }
        public int Quantity { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal? TotalCost { get; set; }
        public string? Notes { get; set; }
    }
}
