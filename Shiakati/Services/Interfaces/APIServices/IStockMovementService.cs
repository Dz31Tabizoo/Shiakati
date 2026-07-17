using Shiakati.Models;

namespace Shiakati.Services.Interfaces.APIServices
{
    public interface IStockMovementService
    {
        Task<List<StockMovementModel>> GetMovementsAsync(DateTime? from, DateTime? to);
    }
}
