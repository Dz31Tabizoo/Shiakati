using Shiakati.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Services.Interfaces.DataServices
{
    public interface IStockDataService
    {
        ObservableCollection<ProductModel> Products { get; }
        ObservableCollection<ProductVariantModel> Variants { get; }

        Task LoadProductsAsync(bool forceRefresh = false);
        Task LoadVariantsAsync(bool forceRefresh = false);

        
        Task<ProductVariantResponse?> AddProductVariantAsync(AddVariantRequest request);
        Task<ProductVariantResponse?> UpdateProductVariantAsync(UpdateVariantRequest request);
        Task<List<ProductVariantResponse>?> BulkAddVariantsAsync(BulkAddVariantsRequest request);

        Task<StockValuationResponse> GetStockValuationAsync();

        event Action? DataChanged;
    }
}
