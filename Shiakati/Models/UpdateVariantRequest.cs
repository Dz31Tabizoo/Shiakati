using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class UpdateVariantRequest
    {
        public int VariantId { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public string? BrandName { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Color { get; set; }
        public int? Length { get; set; }
        public string? Width { get; set; }
        public decimal? PurchasePrice { get; set; }
        public decimal? DiscountFixed { get; set; }
        public decimal? SalePrice { get; set; }
        public int? StockQuantity { get; set; }
        public bool? IsActive { get; set; }
    }
}
