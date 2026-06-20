using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class TopProductDto
    {
        public string Sku { get; set; }
        public string ProductName { get; set; }
        public string BrandName { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalMargin { get; set; }
        public decimal MarginPercentage { get; set; } // Optional, you calculated it
        public int CurrentStock { get; set; }

        public string SizeColor { get; set; }
    }
}
