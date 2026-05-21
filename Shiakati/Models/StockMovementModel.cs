using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class StockMovementModel
    {
        public int MovementId { get; set; }
        public int VariantId { get; set; }
        public string MovementType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public DateTime? MovementDate { get; set; }
        public int? ReferenceId { get; set; }
        public string? Notes { get; set; }
        public int? UserId { get; set; }
        public string? Sku { get; set; }
        public string? ProductName { get; set; }
        public string? Color { get; set; }
        public string? FullSize { get; set; }
    }
}
