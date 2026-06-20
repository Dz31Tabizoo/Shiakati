using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class UserPerformanceDto
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public int OrdersCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalMargin { get; set; }
    }
}
