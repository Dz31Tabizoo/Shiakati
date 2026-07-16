using Serilog;
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
            try
            {
                var response = await _http.PostAsJsonAsync("api/sales", request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<SaleCreationResult>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating sale");
                return null;
            }
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

        public async Task<List<SaleModel>> GetSalesAsync(string? search, DateTime? from, DateTime? to)
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");
            if (from.HasValue) queryParams.Add($"from={from.Value:yyyy-MM-dd}");
            if (to.HasValue) queryParams.Add($"to={to.Value:yyyy-MM-dd}");

            var url = "api/sales";
            if (queryParams.Any()) url += "?" + string.Join("&", queryParams);

            var response = await _http.GetAsync(url);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<List<SaleModel>>() ?? new();
            return new();
        }

        public async Task<bool> VoidSaleAsync(int saleId)
        {
            var response = await _http.PutAsync($"api/sales/{saleId}/void", null);
            return response.IsSuccessStatusCode;
        }

        
    }
}
