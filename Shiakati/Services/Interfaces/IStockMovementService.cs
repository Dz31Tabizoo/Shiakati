using Shiakati.Models;

namespace Shiakati.Services.Interfaces
{
    public interface IStockMovementService
    {
        Task<List<StockMovementModel>> GetMovementsAsync(DateTime? from, DateTime? to);
    }
}
