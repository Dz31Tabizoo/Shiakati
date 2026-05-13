using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace Shiakati.Services.Implementations
{
    public class ProductsService : IProductsService
    {
        private readonly HttpClient _httpClient;

        public ProductsService(HttpClient http)
        {
            _httpClient = http;

        }
        public async Task<List<ProductModel>> GetProductsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/products");
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<List<ProductModel>>() ?? new();
                
                
            }
            catch (Exception ex) 
            {
                MessageBox.Show($"{ex}");
                return new();
            }
                
            
        }
    }
}
