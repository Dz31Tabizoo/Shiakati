using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class DashboardStatsResponse
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageBasket { get; set; }
        public decimal TotalMargin { get; set; }
        public decimal AverageMarginPercentage { get; set; }
        public List<TopProductDto> TopSellingProducts { get; set; }
        public List<TopProductDto> TopProfitableProducts { get; set; }
        public List<StockAlertDto> StockAlerts { get; set; }
        public List<UserPerformanceDto> UserPerformances { get; set; }
        public List<DailySalesTrendDto> DailyTrend { get; set; }
    }
}
