using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class InvoiceImageDto
    {
        public int Id { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public int? ProductsTotal { get; set; }      
        public decimal? TotalAmount { get; set; }
        public decimal? AmountPaid { get; set; }
        public decimal? AmountRest { get; set; }
    }
}
