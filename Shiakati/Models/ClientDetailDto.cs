using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class ClientDetailDto
    {
        public int ClientId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
        public decimal TotalCredits { get; set; }
        public decimal TotalPaid { get; set; }
        public List<CreditDto> Credits { get; set; } = new();
        public List<VersementDto> Versements { get; set; } = new();
    }
}
