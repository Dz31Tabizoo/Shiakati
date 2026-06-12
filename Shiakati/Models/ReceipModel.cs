using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class ReceipModel
    {
        public string TicketNumber { get; set; } = string.Empty;
        public bool IsEdited { get; set; } = false; 
        public DateTime Date { get; set; }
        public List<ReceiptItem> Items { get; set; } = new();
        public decimal TotalDiscount { get; set; }
        public decimal TotalAmount { get; set; }

        public string? ClientName { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingDebt { get; set; }
        public string DocumentType { get; set; } = "SALE"; // SALE, RESERVATION, HONOR
        public decimal DepositAmount { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public decimal TotalBeforeDeposit { get; set; }
    }    
}
