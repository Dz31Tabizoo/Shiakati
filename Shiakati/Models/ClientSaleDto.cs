using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class ClientSaleDto
    {
        public int SaleId { get; set; }
        public string? TicketNumber { get; set; }
        public DateTime? SaleDate { get; set; }
        public decimal? TotalAmount { get; set; }
        public List<ClientSaleItemDto> Items { get; set; } = new();
    }

    
}
