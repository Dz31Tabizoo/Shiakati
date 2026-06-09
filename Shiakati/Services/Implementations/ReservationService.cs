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
    }
}
