using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class SaleItemResponse
    {
        public int SaleItemId { get; set; }
        public int VariantId { get; set; }
        public string? SKU { get; set; }
        public int Quantity { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal? DiscountedUnitPrice { get; set; }
        public decimal? DiscountAmount { get; set; }
        public bool? FixedDiscountApplied { get; set; }
        public decimal? LineTotal { get; set; }
    }
}
