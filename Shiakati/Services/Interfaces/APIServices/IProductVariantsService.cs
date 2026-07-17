using Shiakati.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Shiakati.Services.Interfaces.APIServices
{
    public interface IProductVariantsService
    {
        Task<List<ProductVariantModel>> GetProductVariantsAsync();
        Task<ProductVariantResponse> AddProductVariantAsync(AddVariantRequest request);
        Task<ProductVariantResponse> UpdateProductVariantAsync(UpdateVariantRequest request);
        Task<List<ProductVariantResponse>?> BulkAddVariantsAsync(BulkAddVariantsRequest request);
        Task<StockValuationResponse> GetStockValuationAsync();
        Task<List<ProductModel>> GetProductsAsync();
        
      
    }
}
