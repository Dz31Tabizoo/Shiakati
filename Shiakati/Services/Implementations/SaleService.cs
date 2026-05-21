using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;

namespace Shiakati.Services.Implementations
{
    public class SaleService : ISaleService
    {
        private readonly HttpClient _http;

        public SaleService(HttpClient http) => _http = http;

        public async Task<SaleCreationResult?> CreateSaleAsync(SaleRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/sales", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<SaleCreationResult>();
        }

        public async Task<bool> UpdateSaleAsync(int saleId, UpdateSaleRequest request)
        {
            var response = await _http.PutAsJsonAsync($"api/sales/{saleId}", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<SaleResponse?> GetSaleAsync(int saleId)
        {
            var response = await _http.GetAsync($"api/sales/{saleId}");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<SaleResponse>();
            return null;
        }

        public async Task<List<SaleSummary>> GetSalesAsync()
        {
            var response = await _http.GetAsync("api/sales");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<List<SaleSummary>>() ?? new();
            return new();
        }
    }
}
