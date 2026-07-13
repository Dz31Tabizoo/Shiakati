using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class ClientSummaryDto
    {
        public int ClientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Address { get; set; }
        public decimal TotalCredits { get; set; }

        public decimal TotalPurchases { get; set; }
        public decimal TotalVersements { get; set; }
        public decimal NetBalance { get; set; }

    }
}
