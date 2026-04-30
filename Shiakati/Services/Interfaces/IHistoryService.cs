using Shiakati.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Services.Interfaces
{
    public interface IHistoryService
    {
        //If all parameters are null, return today's sales.
        Task<IEnumerable<SaleModel>> GetSalesAsync(string? ticketNumber, DateTime? startDate, DateTime? endDate); 
        Task<bool> VoidSaleAsync(int saleId); //For deleting/refunding Annuler une vente

    }
}
