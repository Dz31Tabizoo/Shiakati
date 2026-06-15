using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class BulkAddVariantsRequest
    {
        public int CategoryId { get; set; }
        public int? BrandId { get; set; }
        public string? BrandName { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public List<VariantDetail> Variants { get; set; } = new();
    }
}
