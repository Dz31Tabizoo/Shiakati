using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class ProductVariantResponse
    {
        public int VariantId { get; set; }
        public int? ProductId { get; set; }
        public string? Sku { get; set; }
        public string? Color { get; set; }
        public int? Length { get; set; }
        public string? Width { get; set; }
        public decimal? PurchasePrice { get; set; }
        public decimal? DiscountFixed { get; set; }
        public decimal? SalePrice { get; set; }
        public int? StockQuantity { get; set; }
        public string? FullSize { get; set; }
        public bool? IsActive { get; set; }
        public string? ProductName { get; set; }
        public string? BrandName { get; set; }
        public string? CategoryName { get; set; }
    }
}
