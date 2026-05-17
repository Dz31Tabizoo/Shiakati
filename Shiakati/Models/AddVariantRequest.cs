using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class AddVariantRequest
    {
        public int CategoryId { get; set; }
        public int? BrandId { get; set; }            // null if creating a new brand
        public string? BrandName { get; set; }       // required only if BrandId is null
        public string ProductName { get; set; } = string.Empty;
        public string? Sku { get; set; }             // leave empty to auto‑generate
        public string? Color { get; set; }
        public int? Length { get; set; }
        public string? Width { get; set; }
        public decimal? PurchasePrice { get; set; }
        public decimal? DiscountFixed { get; set; }
        public decimal? SalePrice { get; set; }
        public int StockQuantity { get; set; }
    }
}
