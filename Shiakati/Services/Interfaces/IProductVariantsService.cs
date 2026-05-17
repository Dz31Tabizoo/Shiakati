using Shiakati.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Services.Interfaces
{
    public interface IProductVariantsService
    {
        Task<List<ProductVariantModel>> GetProductVariantsAsync();
        Task<bool> AddProductVariantAsync(AddVariantRequest request);
    }
}
