using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Models
{
    public class BrdCatgResponseDto
    {
        public List<BrandsModel> Brands { get; set; } = new();
        public List<CategoryModel> Categories { get; set; } = new();
    }
}
