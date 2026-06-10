using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    
        public class ReservationDto
        {
            public int ReservationId { get; set; }
            public int ClientId { get; set; }
            public string ClientFullName { get; set; } = string.Empty;
            public DateTime ReservationDate { get; set; }
            public DateTime ExpirationDate { get; set; }
            public decimal TotalAmount { get; set; }
            public decimal DepositAmount { get; set; }
            public decimal Remaining => TotalAmount - DepositAmount;
            public string? Notes { get; set; }
            public bool IsFulfilled { get; set; }
            public bool IsCancelled { get; set; }
            public bool IsActive => !IsCancelled && !IsFulfilled;
            public List<ReservationItemDto> Items { get; set; } = new();
        }
    
}
