using Shiakati.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.Services.Interfaces.DataServices
{
    public interface IStockMovementDataService
    {
        ObservableCollection<StockMovementModel> Movements { get; }

        Task LoadMovementsAsync(DateTime? from = null, DateTime? to = null, bool forceRefresh = false);

        event Action? DataChanged;
    }
}
