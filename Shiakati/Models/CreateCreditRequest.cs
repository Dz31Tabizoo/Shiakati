using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class CreateCreditRequest
    {
        public int ClientId { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
