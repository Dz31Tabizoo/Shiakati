using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using static System.Net.WebRequestMethods;

namespace Shiakati.Services.Implementations
{
    public class ProductVariantsService : IProductVariantsService
    {
        private readonly HttpClient _httpClient;

        public ProductVariantsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Remplacement du nom de la méthode pour éviter le conflit avec le nom de la classe
        public async Task<List<ProductVariantModel>> GetProductVariantsAsync()
        {
            try
            {
                var option = new JsonSerializerOptions {PropertyNameCaseInsensitive = true };
                var response = await _httpClient.GetAsync("api/productVariants");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<List<ProductVariantModel>>(option) ?? new();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex}");
                return new();
            }
        }

        public async Task<ProductVariantResponse?> AddProductVariantAsync(AddVariantRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/stock/add", request);
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<ProductVariantResponse>();
                return null;
            }
            catch(Exception ex)
            {
                MessageBox.Show($"{ex}");
                return null;
            }
        }

        public async Task<ProductVariantResponse?> UpdateProductVariantAsync(UpdateVariantRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync("api/stock/update", request);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<ProductVariantResponse>();
            return null;
        }

        public async Task<List<ProductVariantResponse>?> BulkAddVariantsAsync(BulkAddVariantsRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/stock/bulk-add", request);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<List<ProductVariantResponse>>();
            return null;
        }

        public async Task<StockValuationResponse> GetStockValuationAsync()
        {
            var response = await _httpClient.GetAsync("api/stock/valuation");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<StockValuationResponse>() ?? new StockValuationResponse();
            return new StockValuationResponse();
        }
    }
}
