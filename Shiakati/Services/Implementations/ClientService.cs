using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;


namespace Shiakati.Services.Implementations
{
    public class ClientService : IClientService
    {
        private readonly HttpClient _http;
        public ClientService(HttpClient http) => _http = http;

        public async Task<List<ClientSummaryDto>> GetClientSummariesAsync(string? search)
        {
            var url = "api/clients/summaries";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"?search={Uri.EscapeDataString(search)}";
            var response = await _http.GetAsync(url);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<List<ClientSummaryDto>>() ?? new();
            return new();
        }

        public async Task<ClientDetailDto?> GetClientDetailAsync(int clientId)
        {
            var response = await _http.GetAsync($"api/clients/{clientId}");
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<ClientDetailDto>();
            return null;
        }

        public async Task<ClientDto?> CreateClientAsync(CreateClientRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/clients", request);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<ClientDto>();
            return null;
        }

        public async Task<bool> UpdateClientAsync(int clientId, CreateClientRequest request)
        {
            var response = await _http.PutAsJsonAsync($"api/clients/{clientId}", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> GrantCreditAsync(CreateCreditRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/clientcredits", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> AddVersementAsync(CreateVersementRequest request)
        {
            var response = await _http.PostAsJsonAsync("api/versements", request);
            return response.IsSuccessStatusCode;
        }
    }
}
