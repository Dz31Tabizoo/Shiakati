using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class SaleRequest
    {
        public decimal? GlobalDiscount { get; set; }
        public List<SaleItemDto> Items { get; set; } = new();

        public int? ClientId { get; set; }
        public DateTime? CreditExpiresAt {  get; set; }
        public decimal? PaidAmount { get; set; }
    }
}
