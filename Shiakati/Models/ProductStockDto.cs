namespace Shiakati.Models
{
    public class ProductStockDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int TotalStockQuantity { get; set; }       // Sum of variant StockQuantity
        public decimal TotalPurchaseValue { get; set; }   // Sum of (variant.StockQuantity * variant.PurchasePrice)
        public decimal TotalSaleValue { get; set; }       // Sum of (variant.StockQuantity * variant.SalePrice)
        public decimal PotentialMargin => TotalSaleValue - TotalPurchaseValue;
    }
}
