using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class CreditDto
    {
        public int CreditId { get; set; }
        public decimal Amount { get; set; }
        public DateTime? GrantedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? Notes { get; set; }
        public bool IsRedeemed { get; set; }
    }
}
