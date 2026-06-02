using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class DailyBonusModel
    {
        public DateTime Date { get; set; }
        public decimal TotalSales { get; set; }
        public decimal BonusPercentage { get; set; }
        public decimal BonusAmount { get; set; }
    }
}
