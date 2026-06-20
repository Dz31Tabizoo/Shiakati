using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class StockAlertDto
    {
        public int VariantId { get; set; }
        public string Sku { get; set; }
        public string ProductName { get; set; }
        public string Color { get; set; }
        public string FullSize { get; set; }
        public bool IsAcknowledged { get; set; }
    }
}
