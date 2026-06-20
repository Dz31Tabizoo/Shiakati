
namespace Shiakati.Models
{
    public class BrandStockDto
    {
        public int BrandId { get; set; }
        public string BrandName { get; set; }
        public List<ProductStockDto> Products { get; set; } = new();

        // Aggregated totals
        public int TotalStockQuantity => Products.Sum(p => p.TotalStockQuantity);
        public decimal TotalPurchaseValue => Products.Sum(p => p.TotalPurchaseValue);
        public decimal TotalSaleValue => Products.Sum(p => p.TotalSaleValue);
        public decimal PotentialMargin => TotalSaleValue - TotalPurchaseValue;
    }
}
