using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class CreateVersementRequest
    {
        public int ClientId { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
        public int? SaleId { get; set; }
    }
}
