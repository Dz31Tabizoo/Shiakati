using System.Net.Http.Json;
using Shiakati.Models;
using System.Net.Http;
using Shiakati.Services.Interfaces.APIServices;

namespace Shiakati.Services.Implementations
{
    public class StockMovementService : IStockMovementService
    {
        private readonly HttpClient _http;
        public StockMovementService(HttpClient http) => _http = http;

        public async Task<List<StockMovementModel>> GetMovementsAsync(DateTime? from, DateTime? to)
        {
            var queryParams = new List<string>();
            if (from.HasValue) queryParams.Add($"from={from.Value:yyyy-MM-dd}");
            if (to.HasValue) queryParams.Add($"to={to.Value:yyyy-MM-dd}");
            var url = "api/stockmovements";
            if (queryParams.Any()) url += "?" + string.Join("&", queryParams);

            var response = await _http.GetAsync(url);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<List<StockMovementModel>>() ?? new();
            return new();
        }
    }
}

