using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class UpdateSaleItemDto
    {
        public int? SaleItemId { get; set; }   // null for new items
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        public bool FixedDiscountApplied { get; set; }
        public decimal? ManualDiscountAmount { get; set; }
    }
}
