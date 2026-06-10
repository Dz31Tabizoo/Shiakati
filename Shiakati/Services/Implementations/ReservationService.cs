using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;


namespace Shiakati.Services.Implementations
{
    public class ReservationService : IReservationService
    {
        private readonly HttpClient _http;
        public ReservationService(HttpClient http) => _http = http;
        public async Task<bool> CreateReservationAsync(CreateReservationRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/reservations", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<ReservationDto>> GetReservationsAsync(string? status = null)
        {
            var url = "api/reservations";
            if (!string.IsNullOrWhiteSpace(status))
                url += $"?status={status}";
            var response = await _http.GetAsync(url);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<List<ReservationDto>>() ?? new();
            return new();
        }

        public async Task<bool> CancelReservationAsync(int id)
        {
            var response = await _http.PutAsync($"api/reservations/{id}/cancel", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> FulfillReservationAsync(int id)
        {
            var response = await _http.PutAsync($"api/reservations/{id}/fulfill", null);
            return response.IsSuccessStatusCode;
        }

    }
}
