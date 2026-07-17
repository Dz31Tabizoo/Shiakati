using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Shiakati.Models;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Services.Interfaces.DataServices
{
    public interface ISaleDataService
    {
        ObservableCollection<SaleModel> Sales { get; }

        Task LoadSalesAsync(string? search = null, DateTime? from = null, DateTime? to = null,bool forceRefresh = false);
        Task<SaleCreationResult?> CreateSaleAsync(SaleRequest request);
        Task<bool> UpdateSaleAsync(int saleId, UpdateSaleRequest request);
        Task<bool> VoidSaleAsync(int saleId);
        Task<SaleResponse?> GetSaleAsync(int saleId);
        
        event Action? DataChanged;
    }
}
