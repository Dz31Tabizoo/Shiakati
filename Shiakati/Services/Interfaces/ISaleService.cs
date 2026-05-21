using Shiakati.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Services.Interfaces
{
    public interface ISaleService
    {
        Task<SaleCreationResult?> CreateSaleAsync(SaleRequest request);
        Task<bool> UpdateSaleAsync(int saleId, UpdateSaleRequest request);
        Task<SaleResponse?> GetSaleAsync(int saleId);
        Task<List<SaleSummary>> GetSalesAsync();
    }
}
