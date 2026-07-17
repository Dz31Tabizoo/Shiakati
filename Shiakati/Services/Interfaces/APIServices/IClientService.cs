using Shiakati.Models;

namespace Shiakati.Services.Interfaces.APIServices
{
    public interface IClientService
    {
        Task<bool> AddVersementAsync(CreateVersementRequest request);
        Task<ClientSummaryDto?> CreateClientAsync(CreateClientRequest request);
        Task<ClientDetailDto?> GetClientDetailAsync(int clientId);
        Task<List<ClientSummaryDto>> GetClientSummariesAsync(string? search);
        Task<bool> GrantCreditAsync(CreateCreditRequest request);
        Task<bool> UpdateClientAsync(int clientId, CreateClientRequest request);

        Task<List<ClientSaleDto>> GetClientSalesAsync(int clientId);
    }
}