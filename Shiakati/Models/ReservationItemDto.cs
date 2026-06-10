using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class ReservationItemDto
    {
        public int ReservationItemId { get; set; }   // new
        public int VariantId { get; set; }
        public string? SKU { get; set; }             // new
        public string? ProductName { get; set; }     // new
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
