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
        
        
        Task<List<SaleSummary>> GetSalesAsync();
        Task<List<SaleModel>> GetSalesAsync(string? search, DateTime? from, DateTime? to);


        Task<SaleResponse?> GetSaleAsync(int saleId);
        Task<SaleCreationResult?> CreateSaleAsync(SaleRequest request);
        Task<bool> UpdateSaleAsync(int saleId, UpdateSaleRequest request);
        Task<bool> VoidSaleAsync(int saleId);

        
    }
}
