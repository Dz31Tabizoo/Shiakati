using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class UpdateSaleRequest
    {
        public int? ClientId { get; set; }
        public DateTime? CreditExpiresAt { get; set; }
        public decimal? PaidAmount { get; set; }


        public int SaleId { get; set; }
        public decimal? GlobalDiscount { get; set; }
        public int? UserId { get; set; }  // optional, server will use token if null
        public List<UpdateSaleItemDto> Items { get; set; } = new();
    }
}
