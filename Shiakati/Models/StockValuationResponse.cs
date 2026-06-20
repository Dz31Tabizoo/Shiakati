namespace Shiakati.Models
{
    public class StockValuationResponse
    {
        public List<CategoryStockDto> Categories { get; set; } = new();

        // Grand totals
        public int TotalStockQuantity => Categories.Sum(c => c.TotalStockQuantity);
        public decimal TotalPurchaseValue => Categories.Sum(c => c.TotalPurchaseValue);
        public decimal TotalSaleValue => Categories.Sum(c => c.TotalSaleValue);
        public decimal TotalPotentialMargin => TotalSaleValue - TotalPurchaseValue;
    }
}
