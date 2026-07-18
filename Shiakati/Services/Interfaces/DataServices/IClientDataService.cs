using Shiakati.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Services.Interfaces.DataServices
{
    public interface IClientDataService
    {
        ObservableCollection<ClientSummaryDto> Clients { get; }

        Task LoadClientsAsync(bool forceRefresh = false);
        Task<ClientSummaryDto> AddClientAsync(CreateClientRequest client);
        Task UpdateClientAsync(ClientSummaryDto client);
        //Task DeleteClientAsync(int clientId);

        // These may affect the list, so we include them here:
        Task<bool> AddVersementAsync(CreateVersementRequest request);
        Task<bool> GrantCreditAsync(CreateCreditRequest request);

        // Detail & Sales – optional, can stay in domain service or be forwarded
        Task<ClientDetailDto?> GetClientDetailAsync(int clientId);
        Task<List<ClientSaleDto>> GetClientSalesAsync(int clientId);

        event Action? ClientsDataChanged;

    }

}
