namespace Shiakati.Models
{
    public class CategoryStockDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public List<BrandStockDto> Brands { get; set; } = new();

        // Aggregated totals
        public int TotalStockQuantity => Brands.Sum(b => b.TotalStockQuantity);
        public decimal TotalPurchaseValue => Brands.Sum(b => b.TotalPurchaseValue);
        public decimal TotalSaleValue => Brands.Sum(b => b.TotalSaleValue);
        public decimal PotentialMargin => TotalSaleValue - TotalPurchaseValue;
    }
}
