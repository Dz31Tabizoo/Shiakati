using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;

namespace Shiakati.Services.Implementations
{
    public class DashBordService : IDashBordService
    {
        private readonly HttpClient _http;

        public DashBordService(HttpClient http) => _http = http;

        public async Task<DashboardStatsResponse> GetDashBordDataAsync(DashboardFilterRequest filter)
        {
            var response = await _http.GetAsync($"api/dashboard/stats?StartDate={filter.StartDate:yyyy-MM-dd}&EndDate={filter.EndDate:yyyy-MM-dd}&UserId={filter.UserId}");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<DashboardStatsResponse>() ?? new DashboardStatsResponse();
            return new DashboardStatsResponse();
        }

        public async Task<bool> AcknowledgeAlertAsync(int variantId)
        {
            var response = await _http.PostAsync($"api/dashboard/acknowledge/{variantId}", null);
            return response.IsSuccessStatusCode;
        }
    }
}
