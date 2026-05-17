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
                var response = await _httpClient.GetAsync("api/ProductVariants");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<List<ProductVariantModel>>(option) ?? new();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex}");
                return new();
            }
        }

        
        public async Task<bool> AddProductVariantAsync(AddVariantRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/Stock/Add", request);
                return response.IsSuccessStatusCode;
            }
            catch(Exception ex)
            {
                MessageBox.Show($"{ex}");
                return false;
            }
        }
        
    }
}
