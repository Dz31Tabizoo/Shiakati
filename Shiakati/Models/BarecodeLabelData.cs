using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public  class BarecodeLabelData
    {
        public string BrandName { get; set; } = string.Empty;
        public string VariantName { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;//sku
        public string ProductSize { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }
}
